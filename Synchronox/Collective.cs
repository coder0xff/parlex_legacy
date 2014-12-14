using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Synchronox {
    public abstract class Collective {
        protected void ConstructionCompleted() {
            StartBlocker.Set();
        }

        /// <summary>
        /// Perform any cleanup after all nodes have halted
        /// </summary>
        protected virtual void Terminator() {}

        internal readonly ManualResetEventSlim StartBlocker = new ManualResetEventSlim();

        private int _haltedNodeCount;

        internal void Add(Node node) {
            _nodes.Add(node);
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
            foreach (var node in _nodes) {
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

        public void NodeHalted() {
            if (Interlocked.Increment(ref _haltedNodeCount) == _nodes.Count) {
                Terminator();
                _blocker.Set();
            }
        }
    }
}
