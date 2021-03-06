﻿//#define PARSE_TRACE
using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Parlex {
    public class ParseEngine : IDisposable {
        public string Document { get; private set; }

        public IReadOnlyList<Int32> CodePoints {
            get { return _codePoints; }
        }

        public AbstractSyntaxGraph AbstractSyntaxGraph { get; private set; }

        public void Join() {
            _blocker.Wait();
        }

        public void Dispose() {
            _blocker.Dispose();
        }

        private readonly ReadOnlyCollection<Int32> _codePoints;
        private readonly Recognizer _main;
        private int _activeDispatcherCount;
        private readonly Dictionary<MatchCategory, Dispatcher> _dispatchers = new Dictionary<MatchCategory, Dispatcher>();
        private readonly ManualResetEventSlim _blocker = new ManualResetEventSlim();
        private readonly int _start;
        private readonly int _length;
        internal readonly CustomThreadPool ThreadPool = new CustomThreadPool();
        private readonly Action _idleHandler;

        public ParseEngine(string document, Recognizer main, int start = 0, int length = -1) {
            Document = document;
            _start = start;
            _codePoints = new ReadOnlyCollection<int>(document.GetUtf32CodePoints());
            _length = length < 0 ? _codePoints.Count : length;
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
                internal Recognizer Node { get; set; }
                internal ParseContext Context { get; set; }
                internal bool Ended { get; set; }
            }

            private readonly ParseEngine _engine;
            private bool IsGreedy { get; set; }
            internal readonly List<Match> Matches = new List<Match>();
            private readonly List<MatchClass> _matchClasses = new List<MatchClass>();
            internal readonly List<DependencyEntry> Dependents = new List<DependencyEntry>();
            internal bool Completed;
            internal event Action<Dispatcher> OnComplete;
            private readonly CustomThreadPool _threadPool;
            internal Dispatcher(ParseEngine engine, CustomThreadPool threadPool, int position, Recognizer recognizer)
                : base(position, recognizer) {
                _engine = engine;
                _threadPool = threadPool;
                IsGreedy = recognizer.IsGreedy;
#if PARSE_TRACE
                System.Diagnostics.Debug.WriteLine("Creating Dispatcher " + this);
#endif
            }

            internal void AddResult(Match match) {
#if PARSE_TRACE
                System.Diagnostics.Debug.WriteLine("Adding Match " + match);
#endif
                lock (Matches) {
                    Matches.Add(match);
                }
                if (!IsGreedy) {
                    lock (_matchClasses) {
                        if (!_matchClasses.Contains(match.MatchClass)) {
#if PARSE_TRACE
                            System.Diagnostics.Debug.WriteLine("New MatchClass " + match.MatchClass);
#endif
                            _matchClasses.Add(match.MatchClass);
                            ScheduleFlush();
                        }
                    }
                }
            }

            internal void AddDependency(Dispatcher dependent, Recognizer node, Action handler) {
#if PARSE_TRACE
                System.Diagnostics.Debug.WriteLine("Creating dependency by Dispatcher " + dependent + " on Dispatcher " + this);
#endif
                lock (Dependents) {
                    Dependents.Add(new DependencyEntry { Node = node, Handler = handler, Context = node.ParseContext, Dependent = dependent }); //context is TLS
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
#if PARSE_TRACE
                                System.Diagnostics.Debug.WriteLine("Sending " + matchClass + " to " + dependencyEntry.Dependent);
#endif
                                dependencyEntry.FifoIndex++;
                                var newPos = dependencyEntry.Context.Position + matchClass.Length;
                                var newChain = new List<MatchClass>(dependencyEntry.Context.ParseChain) { matchClass };
                                var oldContext = dependencyEntry.Context;
                                dependencyEntry.Node.ParseContext = dependencyEntry.Context;
                                dependencyEntry.Node.StartDependency();
                                var entry = dependencyEntry;
                                _threadPool.QueueUserWorkItem(_ => {
                                        entry.Node.ParseContext = new ParseContext { Position = newPos, ParseChain = newChain, Engine = _engine, Dispatcher = entry.Context.Dispatcher, DependencyCounter = entry.Context.DependencyCounter};
                                        entry.Handler();
                                        entry.Node.EndDependency();
                                        entry.Node.ParseContext = null;
                                    });
                                dependencyEntry.Node.ParseContext = oldContext;
                            }
                            if (Completed && !dependencyEntry.Ended) {
                                dependencyEntry.Ended = true;
#if PARSE_TRACE
                                System.Diagnostics.Debug.WriteLine("Informing " + dependencyEntry.Dependent + " that " + this + " has completed");
#endif
                                dependencyEntry.Node.ParseContext = dependencyEntry.Context;
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
#if PARSE_TRACE
                                        System.Diagnostics.Debug.WriteLine("New MatchClass (greedy) " + matchClass);
#endif
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
                return "{" + Position + ":" + Recognizer.Name + "}";
            }
        }

        private Dispatcher GetDispatcher(MatchCategory matchCategory) {
            Dispatcher dispatcher;
            lock (_dispatchers) {
                if (!_dispatchers.TryGetValue(matchCategory, out dispatcher)) {
                    dispatcher = new Dispatcher(this, ThreadPool, matchCategory.Position, matchCategory.Recognizer);
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
                dispatcher.Recognizer.ParseContext = new ParseContext {
                    Position = dispatcher.Position, 
                    ParseChain = new List<MatchClass>(), 
                    Engine = this, 
                    Dispatcher = dispatcher, 
                    DependencyCounter = new DependencyCounter()
                };
                dispatcher.Recognizer.StartDependency();
                dispatcher.Recognizer.Start();
                dispatcher.Recognizer.EndDependency();
            });
        }

        private void OnDispatcherTerminated(Dispatcher dispatcher) {
#if PARSE_TRACE
            System.Diagnostics.Debug.WriteLine("Terminating Dispatcher " + dispatcher);
#endif
            if (Interlocked.Decrement(ref _activeDispatcherCount) == 0) {
                Finish();
            }
        }

        private void StartParse() {
            GetDispatcher(new MatchCategory(0, _main));
        }

        internal void AddDependency(Recognizer symbol, Dispatcher dependent, Recognizer node, Action handler) {
            GetDispatcher(new MatchCategory(node.ParseContext.Position, symbol)).AddDependency(dependent, node, handler);
        }

        private void Finish() {
            ThreadPool.OnIdle -= _idleHandler;
            ConstructAbstractSyntaxGraph();
            _blocker.Set();
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
                Root = new MatchClass(this, _start, _main, _length),
                NodeTable = new Dictionary<MatchClass, List<Match>>()
            };
            AddSymbolMatchesToAbstractSyntaxForest();
            PruneAbstractSyntaxForest();
        }

        private void DeadLockBreaker() {
            //This should only be called when a complete work stoppage is detected
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