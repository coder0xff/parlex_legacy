using System;
using System.Threading;

namespace Parlex {
    public abstract class Recognizer : IDisposable {
        public ParseContext ParseContext {
            get { return _parseContext.Value; }
            set { _parseContext.Value = value; }
        }

        public abstract String Name { get; }
        public abstract Boolean IsGreedy { get; }
        public abstract void Start();

        public void Dispose() {
            _parseContext.Dispose();
        }

        protected void Transition(Recognizer symbol, Action nextState) {
            StartDependency();
            ParseContext.Engine.AddDependency(symbol, ParseContext.Dispatcher, this, nextState);
        }

        protected void Transition(String text, Action nextState) {
            Transition(new StringTerminal(text), nextState);
        }

        protected void Accept() {
            var dispatcher = ParseContext.Dispatcher;
            dispatcher.AddResult(new Match {
                ChildrenArray = ParseContext.ParseChain.ToArray(),
                Length = ParseContext.Position - dispatcher.Position,
                Position = dispatcher.Position,
                Recognizer = dispatcher.Recognizer,
                Engine = ParseContext.Engine
            });
        }

        private readonly ThreadLocal<ParseContext> _parseContext = new ThreadLocal<ParseContext>();

        internal void StartDependency() {
            ParseContext.DependencyCounter.Increment();
        }

        internal void EndDependency() {
            var savedContext = ParseContext;
            if (ParseContext.DependencyCounter.Decrement()) {
                ParseContext.Engine.ThreadPool.QueueUserWorkItem(_ => savedContext.Dispatcher.NodeCompleted());
            }
        }

    }
}