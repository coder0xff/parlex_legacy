using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Parlex;

namespace Parlex {
    public abstract class ParseNode {
        internal ParseEngine.Dispatcher Dispatcher { get; set; }
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
            _context.Value.Engine.AddDependency(symbol, Dispatcher, this, nextState);
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
            Dispatcher.AddResult(new Match {
                Children = _context.Value.ParseChain.ToArray(),
                Length = _context.Value.Position - Dispatcher.Position,
                Position = Dispatcher.Position,
                Symbol = Dispatcher.Symbol,
                Engine = _context.Value.Engine
            });
        }

        internal void StartDependency() {
            var temp = _context.Value;
            _context.Value = temp;
            Interlocked.Increment(ref _activeDependencyCount);
        }

        internal void EndDependency() {
            if (Interlocked.Decrement(ref _activeDependencyCount) == 0) {
                _context.Value.Engine.ThreadPool.QueueUserWorkItem(_ => Dispatcher.NodeCompleted());
            }
        }
    }
}