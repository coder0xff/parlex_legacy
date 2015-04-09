using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Parlex;

namespace Parlex {
    public abstract class ParseNode {
        public readonly ThreadLocal<ParseContext> _context = new ThreadLocal<ParseContext>();

        internal int _activeDependencyCount;

        public int Position {
            get {
                return _context.Value.Position;
            }
            set {
                var temp = _context.Value;
                temp.Position = value;
                _context.Value = temp;
            }
        }

        public abstract void Start();
        public virtual void OnCompletion(NodeParseResult result) {}

        protected void Transition(IParseNodeFactory symbol, Action nextState) {
            StartDependency();
            _context.Value.Engine.AddDependency(symbol, _context.Value.Dispatcher, this, nextState);
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
            var dispatcher = _context.Value.Dispatcher;
            dispatcher.AddResult(new Match {
                Children = _context.Value.ParseChain.ToArray(),
                Length = _context.Value.Position - dispatcher.Position,
                Position = dispatcher.Position,
                Symbol = dispatcher.Symbol,
                Engine = _context.Value.Engine
            });
        }

        internal void StartDependency() {
            var temp = _context.Value;
            _context.Value = temp;
            Interlocked.Increment(ref _activeDependencyCount);
        }

        internal void EndDependency() {
            var savedContext = _context.Value;
            if (Interlocked.Decrement(ref _activeDependencyCount) == 0) {
                _context.Value.Engine.ThreadPool.QueueUserWorkItem(_ => savedContext.Dispatcher.NodeCompleted());
            }
        }
    }
}