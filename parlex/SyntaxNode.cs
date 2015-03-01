using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Parlex;

namespace Parlex {
    public abstract class SyntaxNode {
        internal ParserEngine Engine { get; set; }
        internal ParserEngine.Dispatcher Dispatcher { get; set; }
        internal readonly ThreadLocal<ParseContext> _context = new ThreadLocal<ParseContext>();

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
        public abstract void OnCompletion(NodeParseResult result);

        protected void Transition(ISyntaxNodeFactory symbol, Action nextState) {
            StartDependency();
            Engine.AddDependency(symbol, Dispatcher, this, nextState);
        }

        protected void Accept() {
            Dispatcher.AddResult(new Match {
                Children = _context.Value.ParseChain.ToArray(),
                Length = _context.Value.Position - Dispatcher.Position,
                Position = Dispatcher.Position,
                Symbol = Dispatcher.Symbol,
                Engine = Engine
            });
        }

        internal void StartDependency() {
            var temp = _context.Value;
            _context.Value = temp;
            Interlocked.Increment(ref _activeDependencyCount);
        }

        internal void EndDependency() {
            if (Interlocked.Decrement(ref _activeDependencyCount) == 0) {
                Engine.ThreadPool.QueueUserWorkItem(_ => Dispatcher.NodeCompleted());
            }
        }
    }
}