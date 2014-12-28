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

        private static bool DeadlockBreaker(HashSet<Node> blockedSet, Dictionary<Node, Node[]> dependenciesTable, bool doHalt) {
            var anyChanged = true;
            while (anyChanged) {
                anyChanged = false;
                foreach (var blocked in blockedSet) {
                    var dependencies = dependenciesTable[blocked];
                    if (dependencies.Length == 0 || dependencies.Any(node => !blockedSet.Contains(node))) {
                        blockedSet.Remove(blocked);
                        blocked.pressure = 0;
                        anyChanged = true;
                        break;
                    }
                }
            }
            if (blockedSet.Count > 0) {
                if (doHalt) {
                    var node = blockedSet.First();
                    var input = node.GetInputs().First(i => i.IsBlocked);
                    input.SignalHalt();
                }
                return true;
            }
            if (doHalt) {
                Console.WriteLine("A collective detected that a deadlock might be occurring, but it was a false positive.");
            }
            return false;
        }

        private void DeadlockDetector() {
            var needFullTest = false;
            while (!IsDone()) {
                Node[] nodesCopy;
                if (needFullTest) {
                    lock (_nodes) {
                        nodesCopy = _nodes.ToArray();
                    }
                    Thread.CurrentThread.Priority = ThreadPriority.Normal;
                    foreach (var node in nodesCopy) {
                        node.Lock();
                    }
                    //This branch does require locking
                    //It does not generate false positives
                    //and will take action (halt a node) when
                    //it finds a dead lock
                    var blockedSet = new HashSet<Node>(nodesCopy.Where(node => node.GetInputs().Any(i => i.IsBlocked)));
                    var dependenciesTable = blockedSet.ToDictionary(k => k, k => k.GetInputs().First(i => i.IsBlocked).GetConnectedOutputs().Select(o => o.Owner).Where(d => !d.IsHalted).Distinct().ToArray());
                    DeadlockBreaker(blockedSet, dependenciesTable, true);
                    needFullTest = false;
                    foreach (var node in nodesCopy) {
                        node.Unlock();
                    }
                } else {
                    //this branch doesn't require any locking
                    //it could produce false positives though
                    lock (_nodes) {
                        nodesCopy = _nodes.ToArray();
                    }
                    Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                    var blockedNodeToInput = nodesCopy.ToDictionary(node => node, node => node.GetInputs().FirstOrDefault(input => input.IsBlocked));
                    var blockedSet = new HashSet<Node>(blockedNodeToInput.Where(kvp => kvp.Value != null).Select(kvp => kvp.Key));
                    var dependenciesTable = blockedSet.ToDictionary(k => k, k => blockedNodeToInput[k].GetConnectedOutputs().Select(o => o.Owner).Distinct().Where(d => !d.IsHalted).ToArray());
                    needFullTest = DeadlockBreaker(blockedSet, dependenciesTable, false);
                }
            }
        }
    }
}
