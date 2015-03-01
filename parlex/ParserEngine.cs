using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Parlex {
    internal class ParserEngine {
        internal readonly Int32[] CodePoints;
        private readonly ISyntaxNodeFactory _main;
        private int _activeDispatcherCount;
        private readonly ConcurrentDictionary<MatchCategory, Dispatcher> _dispatchers = new ConcurrentDictionary<MatchCategory, Dispatcher>();
        private readonly ManualResetEventSlim _blocker = new ManualResetEventSlim();
        internal AbstractSyntaxGraph AbstractSyntaxGraph { get; private set; }
        private readonly int _start;
        private readonly int _length;

        internal ParserEngine(String document, int start, int length, ISyntaxNodeFactory main) {
            _start = start;
            CodePoints = document.GetUtf32CodePoints();
            _length = length < 0 ? CodePoints.Length : length;
            _main = main;
            StartParse();
        }

        internal class Dispatcher : MatchCategory {
            private class DependencyEntry {
                internal Dispatcher Dependent { private get; set; }
                internal Action Handler { get; set; }
                internal int FifoIndex { get; set; }
                internal SyntaxNode Node { get; set; }
                internal ParseContext Context { get; set; }
                internal bool Ended { get; set; }
            }

            private bool IsGreedy { get; set; }
            internal readonly List<Match> Matches = new List<Match>();
            private readonly List<MatchClass> _matchClasses = new List<MatchClass>();
            private readonly List<DependencyEntry> _dependencies = new List<DependencyEntry>();
            private bool _completed;
            internal event Action<Dispatcher> OnComplete;

            internal Dispatcher(int position, ISyntaxNodeFactory symbol)
                : base(position, symbol) {
                IsGreedy = symbol.IsGreedy;
                //Debug.WriteLine("Creating Dispatcher " + this);
            }

            internal void AddResult(Match match) {
                //Debug.WriteLine("Adding Match " + match);
                lock (Matches) {
                    Matches.Add(match);
                }
                if (!IsGreedy) {
                    lock (_matchClasses) {
                        if (!_matchClasses.Contains(match.MatchClass)) {
                            //Debug.WriteLine("New MatchClass " + match.MatchClass);
                            _matchClasses.Add(match.MatchClass);
                            ScheduleFlush();
                        }
                    }
                }
            }

            internal void AddDependency(Dispatcher dependent, SyntaxNode node, Action handler) {
                //Debug.WriteLine("Creating dependency by Dispatcher " + dependent + " on Dispatcher " + this);
                lock (_dependencies) {
                    _dependencies.Add(new DependencyEntry { Node = node, Handler = handler, Context = node._context.Value, Dependent = dependent }); //context is TLS
                }
                ScheduleFlush();
            }

            private void ScheduleFlush() {
                DebugThreadPool.QueueUserWorkItem(_ => Flush());
            }

            private void Flush() {
                lock (_dependencies) {
                    lock (_matchClasses) {
                        foreach (var dependencyEntry in _dependencies) {
                            while (dependencyEntry.FifoIndex < _matchClasses.Count) {
                                var matchClass = _matchClasses[dependencyEntry.FifoIndex];
                                //Debug.WriteLine("Sending " + matchClass + " to " + dependencyEntry.Dependent);
                                dependencyEntry.FifoIndex++;
                                var newPos = dependencyEntry.Context.Position + matchClass.Length;
                                var newChain = new List<MatchClass>(dependencyEntry.Context.ParseChain) {matchClass};
                                var oldContext = dependencyEntry.Context;
                                dependencyEntry.Node._context.Value = new ParseContext { Position = newPos, ParseChain = newChain };
                                dependencyEntry.Node.StartDependency();
                                dependencyEntry.Handler();
                                dependencyEntry.Node.EndDependency();
                                dependencyEntry.Node._context.Value = oldContext;
                            }
                            if (_completed && !dependencyEntry.Ended) {
                                dependencyEntry.Ended = true;
                                //Debug.WriteLine("Informing " + dependencyEntry.Dependent + " that " + this + " has completed");
                                dependencyEntry.Node.EndDependency();
                            }
                        }
                    }
                }
            }

            private void SignalTermination() {
                lock (_dependencies) {
                    _completed = true;
                    ScheduleFlush();
                }
                OnComplete(this);
            }

            public void NodeCompleted() {
                if (IsGreedy && Matches.Count > 0) {
                    var length = Matches.Select(match => match.Length).Max();
                    lock (_matchClasses) {
                        foreach (var matchClass in Matches.Where(match => match.Length == length).Select(match => match.MatchClass)) {
                            if (!_matchClasses.Contains(matchClass)) {
                                //Debug.WriteLine("New MatchClass (greedy) " + matchClass);
                                _matchClasses.Add(matchClass);
                            }
                        }
                    }
                    DebugThreadPool.QueueUserWorkItem(_ => SignalTermination());
                } else {
                    SignalTermination();
                }
            }

            public override string ToString() {
                return "{" + Position + ":" + Symbol.Name + "}";
            }
        }

        private Dispatcher GetDispatcher(MatchCategory matchCategory) {
            Dispatcher dispatcher;
            if (!_dispatchers.TryGetValue(matchCategory, out dispatcher)) {
                dispatcher = new Dispatcher(matchCategory.Position, matchCategory.Symbol);
                if (_dispatchers.TryAdd(matchCategory, dispatcher)) {
                    StartDispatcher(dispatcher);
                }
            }
            return dispatcher;
        }

        private void StartDispatcher(Dispatcher dispatcher) {
            Interlocked.Increment(ref _activeDispatcherCount);
            dispatcher.OnComplete += OnDispatcherTerminated;
            DebugThreadPool.QueueUserWorkItem(_ => {
                var node = dispatcher.Symbol.Create();
                node.Engine = this;
                node._context.Value = new ParseContext { Position = dispatcher.Position, ParseChain = new List<MatchClass>() };
                node.Dispatcher = dispatcher;
                node.StartDependency();
                node.Start();
                node.EndDependency();
            });
        }

        private void OnDispatcherTerminated(Dispatcher dispatcher) {
            //Debug.WriteLine("Terminating Dispatcher " + dispatcher);
            if (Interlocked.Decrement(ref _activeDispatcherCount) == 0) {
                Finish();
            }
        }

        private void StartParse() {
            GetDispatcher(new MatchCategory(0, _main));
        }

        internal void AddDependency(ISyntaxNodeFactory symbol, Dispatcher dependent, SyntaxNode node, Action handler) {
            GetDispatcher(new MatchCategory(node._context.Value.Position, symbol)).AddDependency(dependent, node, handler);
        }

        private void Finish() {
            ConstructAbstractSyntaxGraph();
            _blocker.Set();
        }

        public void Join() {
            _blocker.Wait();
        }

        private void PruneAbstractSyntaxForest() {
            var usedMatchClasses = new HashSet<MatchClass> { AbstractSyntaxGraph.Root };
            var priorAdditions = new HashSet<MatchClass> { AbstractSyntaxGraph.Root };
            bool addedAnyClasses = true;
            while (addedAnyClasses) {
                addedAnyClasses = false;
                var toAdds = new HashSet<MatchClass>();
                foreach (MatchClass matchClass in priorAdditions) {
                    if (AbstractSyntaxGraph.NodeTable.ContainsKey(matchClass)) {
                        foreach (Match match in AbstractSyntaxGraph.NodeTable[matchClass]) {
                            foreach (MatchClass child in match.Children) {
                                toAdds.Add(child);
                            }
                        }
                    }
                }
                priorAdditions.Clear();
                foreach (MatchClass toAdd in toAdds) {
                    if (!usedMatchClasses.Contains(toAdd)) {
                        addedAnyClasses = true;
                        usedMatchClasses.Add(toAdd);
                        priorAdditions.Add(toAdd);
                    }
                }
            }
            var toRemoves = AbstractSyntaxGraph.NodeTable.Keys.Where(matchClass => !usedMatchClasses.Contains(matchClass)).ToList();
            foreach (var matchClassToRemove in toRemoves) {
                AbstractSyntaxGraph.NodeTable.Remove(matchClassToRemove);
            }
        }

        private void AddSymbolMatchesToAbstractSyntaxForest() {
            foreach (var subJob in _dispatchers.Values) {
                foreach (var match in subJob.Matches) {
                    List<Match> subTable;
                    if (!AbstractSyntaxGraph.NodeTable.TryGetValue(match.MatchClass, out subTable)) {
                        subTable = new List<Match>();
                        AbstractSyntaxGraph.NodeTable[match.MatchClass] = subTable;
                    }
                    subTable.Add(match);
                }
            }
        }

        private void ConstructAbstractSyntaxGraph() {
            AbstractSyntaxGraph = new AbstractSyntaxGraph {
                Root = new MatchClass(_start, _main, _length),
                NodeTable = new Dictionary<MatchClass, List<Match>>()
            };
            AddSymbolMatchesToAbstractSyntaxForest();
            PruneAbstractSyntaxForest();
        }
    }
}