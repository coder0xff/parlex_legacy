using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;

namespace Synchronox {
    public abstract class Collective {
        protected Collective() {
            ThreadProvider.Default.Start(DeadlockDetector);
        }

        protected void ConstructionCompleted() {
            StartBlocker.Set();
        }

        /// <summary>
        /// Perform any cleanup after all nodes have halted
        /// </summary>
        protected virtual void Terminator() { }

        internal readonly ManualResetEventSlim StartBlocker = new ManualResetEventSlim();

        private int _haltedNodeCount;

        internal void Add(Node node) {
            if (node == null) {
                throw new ArgumentNullException();
            }
            lock (_nodes) {
                _nodes.Add(node);
                _toJoin.Enqueue(node);
            }
        }

        public void Connect<T>(Input<T> input, Output<T> output) {
            input.Owner.VerifyConstructionCompleted();
            output.Owner.VerifyConstructionCompleted();
            if (input.Owner.Collective != this || output.Owner.Collective != this) {
                throw new ApplicationException("The input and output must belong to the called instance of Collective.");
            }
            output.Connect(input);
        }

        public bool IsDone() {
            return _blocker.IsSet;
        }

        public void Join() {
            _blocker.Wait();
            Node node;
            while (_toJoin.TryDequeue(out node)) {
                node.Join();
            }
        }

        /// <summary>
        /// Walks up the Node dependency graph and halts any Nodes waiting on this Node
        /// Then, continue recursively
        /// </summary>
        /// <param name="node"></param>
        internal static void PropagateHalt(Node node) {
            var dependentInputs = node.GetOutputs().SelectMany(output => output.GetConnectedInputs()).Distinct();
            var dependentNodes = dependentInputs.Select(input => input.Owner).Distinct();
            foreach (var dependentNode in dependentNodes) {
                if (!dependentNode.IsHalted) {
                    foreach (var input in dependentNode.GetInputs()) {
                        input.CheckWillHalt();
                    }
                }
            }
        }

        private readonly ManualResetEventSlim _blocker = new ManualResetEventSlim(false);
        private readonly List<Node> _nodes = new List<Node>();
        private readonly ConcurrentQueue<Node> _toJoin = new ConcurrentQueue<Node>();

        public void NodeHalted() {
            if (Interlocked.Increment(ref _haltedNodeCount) == _nodes.Count) {
                Terminator();
                _blocker.Set();
            }
        }

        private void DeadlockDetector() {
            StartBlocker.Wait();
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
            while (!IsDone()) {
                Node[] nodesCopy;
                lock (_nodes) {
                    nodesCopy = _nodes.ToArray();
                }
                var nodeToInput = new Dictionary<Node, IInput>();
                foreach (var node in nodesCopy.Where(n => !n.IsHalted)) {
                    var clear = true;
                    foreach (var input in node.GetInputs()) {
                        if (input.IsBlocked()) {
                            nodeToInput[node] = input;
                            node.pressure += 1.0f / nodesCopy.Length;
                            if (node.pressure > 2) {
                                DeadlockBreaker();
                            }
                            clear = false;
                            break;
                        }
                    }
                    if (clear) {
                        node.pressure = 0;
                    }
                }
                foreach (var node in nodeToInput.Keys) {
                    var input = nodeToInput[node];
                    var dependencies = input.GetConnectedOutputs().Select(output => output.Owner).Distinct().Where(dependency => !dependency.IsHalted).ToArray();
                    var flowTo = dependencies.Where(d => d.pressure > node.pressure).ToArray();
                    foreach (var node1 in flowTo) {
                        node1.pressure += node.pressure / flowTo.Length;
                    }
                    if (flowTo.Length > 0) {
                        node.pressure = 0;
                    }
                }
            }
        }

        public void DeadlockBreaker() {
            lock (_nodes) {
                foreach (var node in _nodes) {
                    node.Lock();
                }
                var blockedSet = new HashSet<Node>(_nodes.Where(node => node.GetInputs().Any(i => i.IsBlocked())));
                var dependenciesTable = blockedSet.ToDictionary(k => k, k => k.GetInputs().First(i => i.IsBlocked()).GetConnectedOutputs().Select(o => o.Owner).Distinct().ToArray());
                var anyChanged = true;
                while (anyChanged) {
                    anyChanged = false;
                    foreach (var blocked in blockedSet) {
                        if (dependenciesTable[blocked].Any(node => !blockedSet.Contains(node))) {
                            blockedSet.Remove(blocked);
                            blocked.pressure = 0;
                            anyChanged = true;
                            break;
                        }
                    }
                }
                if (blockedSet.Count > 0) {
                    var node = blockedSet.First();
                    var input = node.GetInputs().First(i => i.IsBlocked());
                    input.SignalHalt();
                }
                foreach (var node in _nodes) {
                    node.Unlock();
                }
            }
        }
    }
}
