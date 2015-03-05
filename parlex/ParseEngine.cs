using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Parlex {
    internal class ParseEngine {
        internal readonly Int32[] CodePoints;
        private readonly ISyntaxNodeFactory _main;
        private int _activeDispatcherCount;
        private readonly Dictionary<MatchCategory, Dispatcher> _dispatchers = new Dictionary<MatchCategory, Dispatcher>();
        private readonly ManualResetEventSlim _blocker = new ManualResetEventSlim();
        internal AbstractSyntaxGraph AbstractSyntaxGraph { get; private set; }
        private readonly int _start;
        private readonly int _length;
        public readonly CustomThreadPool ThreadPool = new CustomThreadPool();
        private readonly Action _idleHandler;

        internal ParseEngine(String document, int start, int length, ISyntaxNodeFactory main) {
            _start = start;
            CodePoints = document.GetUtf32CodePoints();
            _length = length < 0 ? CodePoints.Length : length;
            _main = main;
            _idleHandler = DeadLockBreaker;
            ThreadPool.OnIdle += _idleHandler;
            StartParse();
        }

        internal class Dispatcher : MatchCategory {
            internal class DependencyEntry {
                internal Dispatcher Dependent { get; set; }
                internal Action Handler { get; set; }
                internal int FifoIndex { get; set; }
                internal SyntaxNode Node { get; set; }
                internal ParseContext Context { get; set; }
                internal bool Ended { get; set; }
            }

            private bool IsGreedy { get; set; }
            internal readonly List<Match> Matches = new List<Match>();
            private readonly List<MatchClass> _matchClasses = new List<MatchClass>();
            internal readonly List<DependencyEntry> Dependents = new List<DependencyEntry>();
            internal bool Completed;
            internal event Action<Dispatcher> OnComplete;
            private readonly CustomThreadPool _threadPool;
            internal Dispatcher(CustomThreadPool threadPool, int position, ISyntaxNodeFactory symbol)
                : base(position, symbol) {
                _threadPool = threadPool;
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
                lock (Dependents) {
                    Dependents.Add(new DependencyEntry { Node = node, Handler = handler, Context = node._context.Value, Dependent = dependent }); //context is TLS
                }
                ScheduleFlush();
            }

            private void ScheduleFlush() {
                _threadPool.QueueUserWorkItem(_ => Flush());
            }

            private void Flush() {
                lock (Dependents) {
                    lock (_matchClasses) {
                        foreach (var dependencyEntry in Dependents) {
                            while (dependencyEntry.FifoIndex < _matchClasses.Count) {
                                var matchClass = _matchClasses[dependencyEntry.FifoIndex];
                                //Debug.WriteLine("Sending " + matchClass + " to " + dependencyEntry.Dependent);
                                dependencyEntry.FifoIndex++;
                                var newPos = dependencyEntry.Context.Position + matchClass.Length;
                                var newChain = new List<MatchClass>(dependencyEntry.Context.ParseChain) { matchClass };
                                var oldContext = dependencyEntry.Context;
                                dependencyEntry.Node._context.Value = new ParseContext { Position = newPos, ParseChain = newChain };
                                dependencyEntry.Node.StartDependency();
                                dependencyEntry.Handler();
                                dependencyEntry.Node.EndDependency();
                                dependencyEntry.Node._context.Value = oldContext;
                            }
                            if (Completed && !dependencyEntry.Ended) {
                                dependencyEntry.Ended = true;
                                //Debug.WriteLine("Informing " + dependencyEntry.Dependent + " that " + this + " has completed");
                                dependencyEntry.Node.EndDependency();
                            }
                        }
                    }
                }
            }

            public void NodeCompleted() {
                lock (Dependents) {
                    if (!Completed) {
                        Completed = true;
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
                        }
                        _threadPool.QueueUserWorkItem(_ => {
                            Flush();
                            OnComplete(this);
                        });
                    }
                }
            }

            public override string ToString() {
                return "{" + Position + ":" + Symbol.Name + "}";
            }
        }

        private Dispatcher GetDispatcher(MatchCategory matchCategory) {
            Dispatcher dispatcher;
            lock (_dispatchers) {
                if (!_dispatchers.TryGetValue(matchCategory, out dispatcher)) {
                    dispatcher = new Dispatcher(ThreadPool, matchCategory.Position, matchCategory.Symbol);
                    _dispatchers[matchCategory] = dispatcher;
                    StartDispatcher(dispatcher);
                }
            }
            return dispatcher;
        }

        private void StartDispatcher(Dispatcher dispatcher) {
            Interlocked.Increment(ref _activeDispatcherCount);
            dispatcher.OnComplete += OnDispatcherTerminated;
            ThreadPool.QueueUserWorkItem(_ => {
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
            ThreadPool.OnIdle -= _idleHandler;
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

        private void DeadLockBreaker() {
            //The should only be called when a complete work stopage is detected
            //As such, locking is not needed
            var dispatchers = _dispatchers.Values.Where(dispatcher => !dispatcher.Completed).ToArray();
            //construct the flow graph
            var graph = new AutoDictionary<Dispatcher, HashSet<Dispatcher>>(x => new HashSet<Dispatcher>());
            var reached = new HashSet<Dispatcher>();
            var unreached = new HashSet<Dispatcher>();
            foreach (var dispatcher in dispatchers) {
                reached.Add(dispatcher);
            }
            foreach (var dispatcher in dispatchers) {
                foreach (var dependent in dispatcher.Dependents.Select(x => x.Dependent)) {
                    graph[dispatcher].Add(dependent);
                    reached.Remove(dependent);
                    unreached.Add(dependent);
                }
            }
            //now perform a reachability search
            while (reached.Count > 0) {
                var previousReached = reached;
                reached = new HashSet<Dispatcher>();
                foreach (var dispatcher in previousReached) {
                    foreach (var dispatcher1 in graph[dispatcher]) {
                        if (unreached.Remove(dispatcher1)) {
                            reached.Add(dispatcher1);
                        }
                    }
                }
            }
            foreach (var dispatcher in unreached) {
                dispatcher.NodeCompleted();
            }
        }
    }
}