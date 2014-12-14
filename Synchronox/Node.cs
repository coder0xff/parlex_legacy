using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Synchronox {
    public abstract class Node {
        protected Node(Collective collective) {
            _collective = collective;
            _collective.Add(this);
            foreach (var property in GetType().GetFields().Where(p => p.FieldType.IsGenericType)) {
                Type genericBase = property.FieldType.GetGenericTypeDefinition();
                if (genericBase == typeof(Input<>)) {
                    if (!property.IsInitOnly) throw new InvalidOperationException(GetType().Name + "." + property.Name + " is not readonly. Input<> fields in a Machine must be readonly.");
                    var input = (IInput)Activator.CreateInstance(property.FieldType, this, new CallProtector());
                    _inputs.Add(input);
                    property.SetValue(this, input);
                } else if (genericBase == typeof(Output<>)) {
                    if (!property.IsInitOnly) throw new InvalidOperationException(GetType().Name + "." + property.Name + " is not readonly. Output<> fields in a Machine must be readonly.");
                    var output = (IOutput)Activator.CreateInstance(property.FieldType, this, new CallProtector());
                    _outputs.Add(output);
                    property.SetValue(this, output);
                }
            }
            _computerThread = new Thread(ComputerRunner);
            _computerThread.Start();
        }

        internal bool IsHalted = false;

        private readonly ManualResetEventSlim _constructionBlocker = new ManualResetEventSlim();

        private readonly List<IInput> _inputs = new List<IInput>();
        private readonly List<IOutput> _outputs = new List<IOutput>(); 

        internal IEnumerable<IInput> GetInputs() {
            return _inputs;
        }

        internal IEnumerable<IOutput> GetOutputs() {
            return _outputs;
        }

        /// <summary>
        /// Set up any initial input connections that are needed to keep the node from halting immediately
        /// </summary>
        protected virtual void Initializer() {}

        /// <summary>
        /// The algorithm that reads from inputs and writes to outputs
        /// </summary>
        protected abstract void Computer();

        /// <summary>
        /// Perform any final actions that must occur when the node halts
        /// </summary>
        protected virtual void Terminator() {}

        private readonly Collective _collective;

        /// <summary>
        /// Permit the node to start execution
        /// </summary>
        protected void ConstructionCompleted() {
            _constructionBlocker.Set();
        }

        internal Collective Collective {
            get { return _collective; }
        }

        private void ComputerRunner() {
            Collective.StartBlocker.Wait();
            try {
                _constructionBlocker.Wait();
                Initializer();
                Computer();
                IsHalted = true;
                Terminator();
                Collective.PropagateHalt(this);
                Collective.NodeHalted();
            } catch (ThreadAbortException) {
                ;
            } catch (HaltException) {
                IsHalted = true;
                Terminator();
                Collective.PropagateHalt(this);
                Collective.NodeHalted();
            }
        }

        internal void VerifyConstructionCompleted() {
            if (!_constructionBlocker.IsSet) {
                throw new NodeConstructionNotCompletedException();
            }
        }

        internal void Join() {
            _computerThread.Join();
        }

        private readonly Thread _computerThread;
    }
}
