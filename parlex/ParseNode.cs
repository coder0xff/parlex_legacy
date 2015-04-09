using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Parlex;

namespace Parlex {
    public abstract class ParseNode {
        public readonly ThreadLocal<ParseContext> Context = new ThreadLocal<ParseContext>();

        public int Position {
            get {
                return Context.Value.Position;
            }
            set {
                var temp = Context.Value;
                temp.Position = value;
                Context.Value = temp;
            }
        }

        public abstract void Start();
        public virtual void OnCompletion(NodeParseResult result) {}

        protected void Transition(IParseNodeFactory symbol, Action nextState) {
            StartDependency();
            Context.Value.Engine.AddDependency(symbol, Context.Value.Dispatcher, this, nextState);
        }

        protected void Transition(RecognizerDefinition recognizerDefinition, Action nextState) {
            Transition(new SymbolNodeFactory(recognizerDefinition), nextState);
        }

        protected void Transition<T>(Action nextState) where T : ParseNode, new() {
            Transition(new GenericParseNodeFactory<T>(), nextState);
        }

        protected void Transition(String text, Action nextState) {
            Transition(new StringTerminalDefinition(text), nextState);
        }

        protected void Accept() {
            var dispatcher = Context.Value.Dispatcher;
            dispatcher.AddResult(new Match {
                Children = Context.Value.ParseChain.ToArray(),
                Length = Context.Value.Position - dispatcher.Position,
                Position = dispatcher.Position,
                Symbol = dispatcher.Symbol,
                Engine = Context.Value.Engine
            });
        }

        internal void StartDependency() {
            Context.Value.DependencyCounter.Increment();
        }

        internal void EndDependency() {
            var savedContext = Context.Value;
            if (Context.Value.DependencyCounter.Decrement()) {
                Context.Value.Engine.ThreadPool.QueueUserWorkItem(_ => savedContext.Dispatcher.NodeCompleted());
            }
        }
    }
}