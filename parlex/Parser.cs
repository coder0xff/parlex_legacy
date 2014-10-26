#define FORCE_SINGLE_THREAD

using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Automata;

namespace Parlex {
    // A zero-based index into the source text
    using Position = Int32;
    // A character count
    using Length = Int32;
    using Recognizer = Grammar.Recognizer;

    public static class Parser {
        /// <summary>
        /// All matches with the same Position, and recognizer are in the same MatchCategory
        /// </summary>
        public class MatchCategory {
            public readonly Position Position;
            public readonly Grammar.ISymbol Symbol;
            internal Job Job { get; private set; }

            protected MatchCategory(MatchCategory other) {
                Position = other.Position;
                Symbol = other.Symbol;
                Job = other.Job;
            }

            internal MatchCategory(Position position, Grammar.ISymbol symbol, Job job) {
                Position = position;
                Symbol = symbol;
                Job = job;
            }

            public override bool Equals(object obj) {
                var castObj = obj as MatchCategory;
                if (ReferenceEquals(null, castObj)) return false;
                return castObj.Position.Equals(Position) && castObj.Symbol.Equals(Symbol) && ReferenceEquals(Job, castObj.Job);
            }

            public override Length GetHashCode() {
                unchecked {
                    var hash = 17;
                    hash = hash * 31 + Position.GetHashCode();
                    hash = hash * 31 + Symbol.GetHashCode();
                    hash = hash * 31 + (Job ?? (Object)0).GetHashCode();
                    return hash;
                }
            }

            public override string ToString() {
                var temp = Position + " " + Symbol;
                if (ReferenceEquals(Job, null)) return temp;
                var docText = Job.Text;
                temp += " " + docText.Substring(Position, Math.Min(docText.Length - Position, 8)).Truncate(7);
                return temp;
            }
        }

        /// <summary>
        /// All matches with the same Position, Length, and recognizer are in the same MatchClass.
        /// </summary>
        public class MatchClass : MatchCategory {
            public readonly Length Length;

            protected MatchClass(MatchClass other)
                : base(other) {
                Length = other.Length;
            }

            public MatchClass(MatchCategory matchCategory, Length length)
                : base(matchCategory) {
                Length = length;
            }

            internal MatchClass(Position position, Grammar.ISymbol symbol, Length length, Job job)
                : base(position, symbol, job) {
                Length = length;
            }

            public override bool Equals(object obj) {
                var castObj = obj as MatchClass;
                if (ReferenceEquals(null, castObj)) return false;
                return base.Equals(castObj) && Length.Equals(castObj.Length);
            }

            public override Length GetHashCode() {
                unchecked {
                    var hash = 17;
                    hash = hash * 31 + base.GetHashCode();
                    hash = hash * 32 + Length.GetHashCode();
                    return hash;
                }
            }

            public override string ToString() {
                var temp = Position + " " + Symbol + " " + Length;
                if (ReferenceEquals(Job, null)) return temp;
                var docText = Job.Text;
                temp += " " + docText.Substring(Position, Math.Min(docText.Length - Position, 8)).Truncate(7);
                return temp;
            }
        }

        public class Match : MatchClass {
            public readonly MatchClass[] Children;

            public Match(MatchClass matchClass, MatchClass[] children)
                : base(matchClass) {
                Children = children;
            }

            public override string ToString()
            {
                return Symbol.Name + ": \"" + Job.Text.Substring(Position, Length) + "\"";
            }
        }

        public class AbstractSyntaxForest {
            public MatchClass Root;
            public Dictionary<MatchClass, List<Match>> NodeTable;

            public bool IsAmbiguous {
                get { return NodeTable.Keys.Any(matchClass => NodeTable[matchClass].Count > 1); }
            }
        }

        public class Job {
            internal class SubJob : MatchCategory {
                internal class RecognizerState {
                    private readonly SubJob _subJob;
                    private readonly Position _position;
                    private readonly Nfa<Grammar.ISymbol>.State[] _states;
                    private bool IsAcceptState { get { return _states.Any(x => ((Recognizer)_subJob.Symbol).AcceptStates.Contains(x)); } }
                    int _unterminatedSubsequentRecognizerStateAndEvaluateCount = 1;
                    bool _subsequentMadeMatch;
                    readonly RecognizerState _antecedent;
                    private readonly MatchClass _entranceMatchClass;

                    public RecognizerState(SubJob subJob, Position position, Nfa<Grammar.ISymbol>.State[] states, RecognizerState antecedent = null, MatchClass entranceMatchClass = null) {
                        _subJob = subJob;
                        _position = position;
                        _states = states;
                        _antecedent = antecedent;
                        _entranceMatchClass = entranceMatchClass;
                        subJob.RecognizerStateCreated();
                        if (antecedent != null) {
                            antecedent.SubsequentStateCreated();
                        }
#if FORCE_SINGLE_THREAD
                        Evaluate();
#else
                        new Thread(_ => Evaluate()).Start();
#endif
                    }

                    private void SubsequentStateCreated() {
                        Interlocked.Increment(ref _unterminatedSubsequentRecognizerStateAndEvaluateCount);
                    }

                    private void EvaluateTerminated() {
                        SubsequentRecognizerStateTerminated();
                    }

                    private void SubsequentRecognizerStateTerminated() {
                        if (_terminateCount > 0) {
                            throw new ApplicationException();
                        }
                        if (Interlocked.Decrement(ref _unterminatedSubsequentRecognizerStateAndEvaluateCount) == 0) {
                            Terminate();
                        }
                    }

                    private void SetSubsequentMadeMatch() {
                        _subsequentMadeMatch = true;
                        if (_antecedent != null) _antecedent.SetSubsequentMadeMatch();
                    }

                    IEnumerable<Grammar.ISymbol> GetCandidateSymbols() {
                        var results = new List<Grammar.ISymbol>();
                        foreach (var state in _states) {
                            results.AddRange(((Recognizer)_subJob.Symbol).TransitionFunction[state].Keys);
                        }
                        results = results.Distinct().ToList();
                        return results;
                    }

                    void Apply(MatchClass match) {
                        if (_terminateCount > 0 || _subJob._terminateCount > 0) {
                            throw new ApplicationException();
                        }
                        var nextStates = new List<Nfa<Grammar.ISymbol>.State>();
                        foreach (var currentState in _states) {
                            if (((Recognizer)_subJob.Symbol).TransitionFunction[currentState].Keys.Contains(match.Symbol)) {
                                nextStates.AddRange(((Recognizer)_subJob.Symbol).TransitionFunction[currentState][match.Symbol]);
                            }
                        }
                        nextStates = nextStates.Distinct().ToList();
                        var nextPosition = _position + match.Length;
                        if (match.Symbol is Recognizer && (match.Symbol as Recognizer).EatWhiteSpace) {
                            while (Grammar.WhiteSpaceTerminal.Matches(_subJob.Job.UnicodeCodePoints, nextPosition))
                                ++nextPosition;
                        }
                        // ReSharper disable once ObjectCreationAsStatement
                        new RecognizerState(_subJob, nextPosition, nextStates.ToArray(), this, match);
                    }

                    void Evaluate() {
                        var searches = GetCandidateSymbols().Select(symbol => new MatchCategory(_position, symbol, _subJob.Job)).ToList();
                        foreach (var search in searches) {
                            _subJob.Job.BeginMatching(search);
                        }
                        foreach (var search in searches) {
                            foreach (var matchClass in _subJob.Job.GetMatchClasses(search)) {
                                Apply(matchClass);
                            }
                        }
                        EvaluateTerminated();
                    }

                    int _terminateCount;

                    private void Terminate() {
                        var temp = Interlocked.Increment(ref _terminateCount);
                        if (temp > 1) {
                            throw new ApplicationException();
                        }
                        if (IsAcceptState) {
                            if (!((Recognizer)_subJob.Symbol).Greedy || !_subsequentMadeMatch) {
                                _subJob.AddMatch(new Match(new MatchClass(_subJob.Position, _subJob.Symbol, _position - _subJob.Position, _subJob.Job), GetChildren().ToArray()));
                                if (_antecedent != null) {
                                    _antecedent.SetSubsequentMadeMatch();
                                }
                            }
                        }
                        if (_antecedent != null) {
                            _antecedent.SubsequentRecognizerStateTerminated();
                        }
                        _subJob.RecognizerStateTerminated();
                    }

                    private IEnumerable<MatchClass> GetChildren() {
                        var results = new List<MatchClass>();
                        if (_antecedent != null) {
                            results.AddRange(_antecedent.GetChildren());
                        }
                        if (_entranceMatchClass != null) {
                            results.Add(_entranceMatchClass);
                        }
                        return results;
                    }
                }

                int _unterminatedRecognizerStateAndCreateFirstRecognizerStateCount = 1;
                int _terminateCount;

                readonly AsyncSet<MatchClass> _matchClasses = new AsyncSet<MatchClass>();
                readonly JaggedAutoDictionary<MatchClass, ConcurrentSet<Match>> _matches = new JaggedAutoDictionary<MatchClass, ConcurrentSet<Match>>(_ => new ConcurrentSet<Match>());

                public SubJob(MatchCategory matchCategory)
                    : base(matchCategory) {
                    Job.SubJobCreated();
                    CreateFirstRecognizerState();
                }

                void CreateFirstRecognizerState() {
                    // ReSharper disable once ObjectCreationAsStatement
                    new RecognizerState(this, Position, ((Recognizer)Symbol).StartStates.ToArray());
                    CreateFirstRecognizeStateTerminated();
                }

                private void AddMatch(Match match) {
                    _matchClasses.Add(match);
                    _matches[match].TryAdd(match);
                }

                private void RecognizerStateCreated() {
                    if (_terminateCount > 0) {
                        throw new ApplicationException();
                    }
                    Interlocked.Increment(ref _unterminatedRecognizerStateAndCreateFirstRecognizerStateCount);
                }

                private void CreateFirstRecognizeStateTerminated() {
                    RecognizerStateTerminated();
                }

                private void RecognizerStateTerminated() {
                    var temp = Interlocked.Decrement(ref _unterminatedRecognizerStateAndCreateFirstRecognizerStateCount);
                    if (temp == 0) {
                        Terminate();
                    } else if (temp < 0) {
                        throw new ApplicationException();
                    }
                }

                private void Terminate() {
                    if (Interlocked.Increment(ref _terminateCount) > 1) {
                        throw new ApplicationException();
                    }
                    _matchClasses.Close();
                    Job.SubJobTerminated();
                }

                public IEnumerable<MatchClass> GetMatchClasses() {
                    return _matchClasses;
                }

                public IEnumerable<Match> GetMatches(MatchClass matchClass) {
                    if (_matches.Keys.Contains(matchClass)) {
                        return _matches[matchClass];
                    }
                    return new Match[] { };
                }
            }

            public readonly String Text;
            internal readonly Int32[] UnicodeCodePoints;
            private readonly JaggedAutoDictionary<MatchCategory, SubJob> _subJobs;
            private readonly JaggedAutoDictionary<MatchCategory, List<Match>> _terminalMatches;
            int _unterminatedSubJobAndConstructorCount = 1;
            readonly ManualResetEventSlim _blocker = new ManualResetEventSlim(false);
            public bool IsDone { get { return _blocker.IsSet; } }
            private readonly MatchClass _root;
            public AbstractSyntaxForest AbstractSyntaxForest { get; private set; }

            public Job(string text, int startPosition, Recognizer rootProduction) {
                Text = text;
                UnicodeCodePoints = text.GetUtf32CodePoints();
                _root = new MatchClass(startPosition, rootProduction, UnicodeCodePoints.Length - startPosition, this);
                _subJobs = new JaggedAutoDictionary<MatchCategory, SubJob>(matchCategory => {
                    System.Diagnostics.Debug.WriteLine("BeginMatching(" + matchCategory + ")");
                    return new SubJob(matchCategory);
                });

                _terminalMatches = new JaggedAutoDictionary<MatchCategory, List<Match>>(_ => new List<Match>());

                CreateFirstSubJob(startPosition, rootProduction);
                ConstructorTerminated();
            }

            void CreateFirstSubJob(int startPosition, Recognizer rootProduction) {
                BeginMatching(new MatchCategory(startPosition, rootProduction, this));
            }

            void ConstructorTerminated() {
                SubJobTerminated();
            }

            private IEnumerable<MatchClass> GetMatchClasses(MatchCategory matchCategory) {
                if (matchCategory.Symbol is Recognizer) {
                    return _subJobs[matchCategory].GetMatchClasses();
                }
                if (_terminalMatches.Keys.Contains(matchCategory)) {
                    return _terminalMatches[matchCategory];
                }
                return new MatchClass[] { };
            }

            private void BeginMatching(MatchCategory search) {
                if (search.Symbol is Recognizer) {
                    _subJobs.EnsureCreated(search);
                } else {
                    var asTerminal = (Grammar.ITerminal)search.Symbol;
                    if (_terminalMatches.EnsureCreated(search)) {
                        if (asTerminal.Matches(UnicodeCodePoints, search.Position)) {
                            _terminalMatches[search].Add(new Match(new MatchClass(search, asTerminal.Length), new MatchClass[] { }));
                        }
                    }
                }
            }

            private void SubJobCreated() {
                Interlocked.Increment(ref _unterminatedSubJobAndConstructorCount);
            }

            private void SubJobTerminated() {
                var temp = Interlocked.Decrement(ref _unterminatedSubJobAndConstructorCount);
                if (temp == 0) {
                    Terminate();
                } else if (temp < 0) {
                    throw new ApplicationException();
                }
            }

            private void PruneAbstractSyntaxForest() {
                var usedMatchClasses = new HashSet<MatchClass> { AbstractSyntaxForest.Root };
                var priorAdditions = new HashSet<MatchClass> { AbstractSyntaxForest.Root };
                var addedAnyClasses = true;
                while (addedAnyClasses) {
                    addedAnyClasses = false;
                    var toAdds = new HashSet<MatchClass>();
                    foreach (var matchClass in priorAdditions) {
                        if (AbstractSyntaxForest.NodeTable.ContainsKey(matchClass)) {
                            foreach (var match in AbstractSyntaxForest.NodeTable[matchClass]) {
                                foreach (var child in match.Children) {
                                    toAdds.Add(child);
                                }
                            }
                        }
                    }
                    priorAdditions.Clear();
                    foreach (var toAdd in toAdds) {
                        if (!usedMatchClasses.Contains(toAdd)) {
                            addedAnyClasses = true;
                            usedMatchClasses.Add(toAdd);
                            priorAdditions.Add(toAdd);
                        }
                    }
                }
                var toRemoves = AbstractSyntaxForest.NodeTable.Keys.Where(matchClass => !usedMatchClasses.Contains(matchClass)).ToList();
                foreach (var matchClassToRemove in toRemoves) {
                    AbstractSyntaxForest.NodeTable.Remove(matchClassToRemove);
                }
            }

            private void AddProductionMatchesToAbstractSyntaxForest() {
                foreach (var subJob in _subJobs.Values) {
                    foreach (var matchClass in subJob.GetMatchClasses()) {
                        var classMatches = (ConcurrentSet<Match>)subJob.GetMatches(matchClass);
                        if (classMatches.Count == 0) continue;
                        if (!AbstractSyntaxForest.NodeTable.Keys.Contains(matchClass)) {
                            AbstractSyntaxForest.NodeTable[matchClass] = new List<Match>();
                        }
                        var subTable = AbstractSyntaxForest.NodeTable[matchClass];
                        subTable.AddRange(classMatches);
                    }
                }
            }

            private void AddTerminalMatchesToAbstractSyntaxGraph() {
                foreach (var matches in _terminalMatches.Values) {
                    foreach (var match in matches) {
                        if (!AbstractSyntaxForest.NodeTable.Keys.Contains(match)) {
                            AbstractSyntaxForest.NodeTable[match] = new List<Match>();
                        }
                        AbstractSyntaxForest.NodeTable[match].Add(match);
                    }
                }
            }

            private void ConstructAbstractSyntaxForest() {
                AbstractSyntaxForest = new AbstractSyntaxForest {
                    Root = _root,
                    NodeTable = new Dictionary<MatchClass, List<Match>>()
                };
                AddProductionMatchesToAbstractSyntaxForest();
                AddTerminalMatchesToAbstractSyntaxGraph();
                PruneAbstractSyntaxForest();
            }

            private void Terminate() {
                ConstructAbstractSyntaxForest();
                _blocker.Set();
            }

            public void Wait() {
                _blocker.Wait();
            }
        }

        public static Job Parse(String text, int startPosition, Recognizer rootProduction) {
            return new Job(text, startPosition, rootProduction);
        }
    }
}
