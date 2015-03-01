#define FORCE_SINGLE_THREAD

using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Automata;

namespace Parlex {
    // A zero-based index into the source text
    using Position = Int32;
    // A character count
    using Length = Int32;

    public class SimpleParser {

        private Grammar _grammar;
        public SimpleParser(Grammar grammar) {
            _grammar = grammar;
        }
        public Job Parse(String text, int startPosition = 0, int length = -1, Grammar.Production rootProduction = null) {
            return new Job(text, startPosition, length, rootProduction ?? _grammar.MainProduction);
        }

        public class AbstractSyntaxGraph {
            public Dictionary<MatchClass, List<Match>> NodeTable;
            public MatchClass Root;

            public bool IsAmbiguous {
                get { return NodeTable.Keys.Any(matchClass => NodeTable[matchClass].Count > 1); }
            }

            public void StripWhiteSpaceEaters() {
                foreach (var matches in NodeTable.Values) {
                    foreach (var match in matches) {
                        match.StripWhiteSpaceEaters();
                    }
                }
            }

            public bool IsEmpty {
                get { return NodeTable.Keys.Count == 0; }
            }
        }

        public class Job {
            public readonly String Text;
            internal readonly Int32[] UnicodeCodePoints;
            private readonly ManualResetEventSlim _blocker = new ManualResetEventSlim(false);
            private readonly MatchClass _root;
            private readonly JaggedAutoDictionary<MatchCategory, SubJob> _subJobs;
            private readonly JaggedAutoDictionary<MatchCategory, List<Match>> _terminalMatches;
            private int _unterminatedSubJobAndConstructorCount = 1;

            public Job(string text, int startPosition, int length, Grammar.Production rootProduction) {
                Text = text;
                UnicodeCodePoints = text.GetUtf32CodePoints();
                if (length < 0) {
                    length = UnicodeCodePoints.Length - startPosition;
                }
                _root = new MatchClass(startPosition, rootProduction, length, this);
                _subJobs = new JaggedAutoDictionary<MatchCategory, SubJob>(matchCategory => {
                    Debug.WriteLine("BeginMatching(" + matchCategory + ")");
                    return new SubJob(matchCategory);
                });

                _terminalMatches = new JaggedAutoDictionary<MatchCategory, List<Match>>(_ => new List<Match>());

                CreateFirstSubJob(startPosition, rootProduction);
                ConstructorTerminated();
            }

            public bool IsDone {
                get { return _blocker.IsSet; }
            }

            public AbstractSyntaxGraph AbstractSyntaxGraph { get; private set; }

            public MatchCategory[] PossibleErrors {
                get { return _possibleErrors; }
            }
            
            private void CreateFirstSubJob(int startPosition, Grammar.Production rootProduction) {
                BeginMatching(new MatchCategory(startPosition, rootProduction, this));
            }

            private void ConstructorTerminated() {
                SubJobTerminated();
            }

            private IEnumerable<MatchClass> GetMatchClasses(MatchCategory matchCategory) {
                if (matchCategory.Symbol is Grammar.Production) {
                    return _subJobs[matchCategory].GetMatchClasses();
                }
                if (_terminalMatches.Keys.Contains(matchCategory)) {
                    return _terminalMatches[matchCategory];
                }
                return new MatchClass[] { };
            }

            private void BeginMatching(MatchCategory search) {
                if (search.Symbol is Grammar.Production) {
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
                int temp = Interlocked.Decrement(ref _unterminatedSubJobAndConstructorCount);
                if (temp == 0) {
                    Terminate();
                } else if (temp < 0) {
                    throw new ApplicationException();
                }
            }

            private void ComputeErrorInformation() {
                var unmatched = _subJobs.Keys.Union(_terminalMatches.Keys).Except(AbstractSyntaxGraph.NodeTable.Keys).ToList();
                var unmatchedButBypassed = unmatched.Where(x => matchCategoryDependencies[x].Any(y => !unmatched.Contains(y)));
                var unmatchedAndNotBypassed = unmatched.Except(unmatchedButBypassed);
                _possibleErrors = unmatchedAndNotBypassed.OrderByDescending(x => x.Position).Concat(unmatchedButBypassed.OrderByDescending(x => x.Position)).ToArray();
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
                List<MatchClass> toRemoves = AbstractSyntaxGraph.NodeTable.Keys.Where(matchClass => !usedMatchClasses.Contains(matchClass)).ToList();
                foreach (MatchClass matchClassToRemove in toRemoves) {
                    AbstractSyntaxGraph.NodeTable.Remove(matchClassToRemove);
                }
            }

            private void AddProductionMatchesToAbstractSyntaxForest() {
                foreach (SubJob subJob in _subJobs.Values) {
                    foreach (MatchClass matchClass in subJob.GetMatchClasses()) {
                        var classMatches = (ConcurrentSet<Match>)subJob.GetMatches(matchClass);
                        if (classMatches.Count == 0) {
                            continue;
                        }
                        if (!AbstractSyntaxGraph.NodeTable.Keys.Contains(matchClass)) {
                            AbstractSyntaxGraph.NodeTable[matchClass] = new List<Match>();
                        }
                        List<Match> subTable = AbstractSyntaxGraph.NodeTable[matchClass];
                        subTable.AddRange(classMatches);
                    }
                }
            }

            private void AddTerminalMatchesToAbstractSyntaxGraph() {
                foreach (var matches in _terminalMatches.Values) {
                    foreach (Match match in matches) {
                        if (!AbstractSyntaxGraph.NodeTable.Keys.Contains(match)) {
                            AbstractSyntaxGraph.NodeTable[match] = new List<Match>();
                        }
                        AbstractSyntaxGraph.NodeTable[match].Add(match);
                    }
                }
            }

            private void ConstructAbstractSyntaxForest() {
                AbstractSyntaxGraph = new AbstractSyntaxGraph {
                    Root = _root,
                    NodeTable = new Dictionary<MatchClass, List<Match>>()
                };
                AddProductionMatchesToAbstractSyntaxForest();
                AddTerminalMatchesToAbstractSyntaxGraph();
                ComputeErrorInformation();
                PruneAbstractSyntaxForest();
            }

            private void Terminate() {
                ConstructAbstractSyntaxForest();
                _blocker.Set();
            }

            public void Join() {
                _blocker.Wait();
            }

            JaggedAutoDictionary<MatchCategory, List<MatchCategory>> matchCategoryDependencies = new JaggedAutoDictionary<MatchCategory, List<MatchCategory>>(_ => new List<MatchCategory>());
            private MatchCategory[] _possibleErrors;

            internal class SubJob : MatchCategory {
                private readonly AsyncSet<MatchClass> _matchClasses = new AsyncSet<MatchClass>();
                private readonly JaggedAutoDictionary<MatchClass, ConcurrentSet<Match>> _matches = new JaggedAutoDictionary<MatchClass, ConcurrentSet<Match>>(_ => new ConcurrentSet<Match>());
                private int _terminateCount;
                private int _unterminatedRecognizerStateAndCreateFirstRecognizerStateCount = 1;

                public SubJob(MatchCategory matchCategory)
                    : base(matchCategory) {
                    Job.SubJobCreated();
                    CreateFirstRecognizerState();
                }

                private void CreateFirstRecognizerState() {
                    // ReSharper disable once ObjectCreationAsStatement
                    new RecognizerState(this, Position, ((Grammar.Production)Symbol).StartStates.ToArray());
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
                    int temp = Interlocked.Decrement(ref _unterminatedRecognizerStateAndCreateFirstRecognizerStateCount);
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

                internal class RecognizerState {
                    private readonly RecognizerState _antecedent;
                    private readonly MatchClass _entranceMatchClass;
                    private readonly Position _position;
                    private readonly Nfa<Grammar.ISymbol>.State[] _states;
                    private readonly SubJob _subJob;

                    private bool _subsequentMadeMatch;
                    private int _terminateCount;
                    private int _unterminatedSubsequentRecognizerStateAndEvaluateCount = 1;

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

                    private bool IsAcceptState {
                        get { return _states.Any(x => ((Grammar.Production)_subJob.Symbol).AcceptStates.Contains(x)); }
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
                        if (_antecedent != null) {
                            _antecedent.SetSubsequentMadeMatch();
                        }
                    }

                    private IEnumerable<Grammar.ISymbol> GetCandidateSymbols() {
                        var results = new List<Grammar.ISymbol>();
                        foreach (Nfa<Grammar.ISymbol>.State state in _states) {
                            results.AddRange(((Grammar.Production)_subJob.Symbol).TransitionFunction[state].Keys);
                        }
                        results = results.Distinct().ToList();
                        return results;
                    }

                    private void Apply(MatchClass match) {
                        if (_terminateCount > 0 || _subJob._terminateCount > 0) {
                            throw new ApplicationException();
                        }
                        var nextStates = new List<Nfa<Grammar.ISymbol>.State>();
                        foreach (Nfa<Grammar.ISymbol>.State currentState in _states) {
                            if (((Grammar.Production)_subJob.Symbol).TransitionFunction[currentState].Keys.Contains(match.Symbol)) {
                                nextStates.AddRange(((Grammar.Production)_subJob.Symbol).TransitionFunction[currentState][match.Symbol]);
                            }
                        }
                        nextStates = nextStates.Distinct().ToList();
                        int nextPosition = _position + match.Length;
                        // ReSharper disable once ObjectCreationAsStatement
                        new RecognizerState(_subJob, nextPosition, nextStates.ToArray(), this, match);
                    }

                    private void Evaluate() {
                        List<MatchCategory> searches = GetCandidateSymbols().Select(symbol => new MatchCategory(_position, symbol, _subJob.Job)).ToList();
                        foreach (MatchCategory search in searches) {
                            _subJob.Job.matchCategoryDependencies[_subJob].Add(search);
                            _subJob.Job.BeginMatching(search);
                        }
                        foreach (MatchCategory search in searches) {
                            foreach (MatchClass matchClass in _subJob.Job.GetMatchClasses(search)) {
                                Apply(matchClass);
                            }
                        }
                        EvaluateTerminated();
                    }

                    private void Terminate() {
                        int temp = Interlocked.Increment(ref _terminateCount);
                        if (temp > 1) {
                            throw new ApplicationException();
                        }
                        if (IsAcceptState) {
                            if (!((Grammar.Production)_subJob.Symbol).Greedy || !_subsequentMadeMatch) {
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
            }
        }

        public class Match : MatchClass {
            public MatchClass[] Children;

            public Match(MatchClass matchClass, MatchClass[] children)
                : base(matchClass) {
                Children = children;
            }

            public override string ToString() {
                var sb = new StringBuilder();
                for (int i = 0; i < Length; ++i) {
                    sb.Append(Char.ConvertFromUtf32(Job.UnicodeCodePoints[Position + i]));
                }
                return Symbol.Name + ": \"" + sb + "\"";
            }

            internal void StripWhiteSpaceEaters() {
                Children = Children.Where(x => x.Symbol != Grammar.WhiteSpacesEater).ToArray();
            }
        }

        /// <summary>
        ///     All matches with the same Position, and recognizer are in the same MatchCategory
        /// </summary>
        public class MatchCategory {
            public readonly Position Position;
            public readonly Grammar.ISymbol Symbol;

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

            internal Job Job { get; private set; }

            public override bool Equals(object obj) {
                var castObj = obj as MatchCategory;
                if (ReferenceEquals(null, castObj)) {
                    return false;
                }
                return castObj.Position.Equals(Position) && castObj.Symbol.Equals(Symbol) && ReferenceEquals(Job, castObj.Job);
            }

            public override Length GetHashCode() {
                unchecked {
                    int hash = 17;
                    hash = hash * 31 + Position.GetHashCode();
                    hash = hash * 31 + Symbol.GetHashCode();
                    hash = hash * 31 + (Job ?? (Object)0).GetHashCode();
                    return hash;
                }
            }

            public override string ToString() {
                string temp = Position + " " + Symbol;
                if (ReferenceEquals(Job, null)) {
                    return temp;
                }
                var sb = new StringBuilder();
                int characterCount = Math.Min(Job.UnicodeCodePoints.Length - Position, 17);
                for (int i = 0; i < characterCount; ++i) {
                    sb.Append(Char.ConvertFromUtf32(Job.UnicodeCodePoints[Position + i]));
                }
                temp += " " + sb.ToString().Truncate(16);
                return temp;
            }
        }

        /// <summary>
        ///     All matches with the same Position, Length, and recognizer are in the same MatchClass.
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
                if (ReferenceEquals(null, castObj)) {
                    return false;
                }
                return base.Equals(castObj) && Length.Equals(castObj.Length);
            }

            public override Length GetHashCode() {
                unchecked {
                    int hash = 17;
                    hash = hash * 31 + base.GetHashCode();
                    hash = hash * 32 + Length.GetHashCode();
                    return hash;
                }
            }

            public override string ToString() {
                string temp = Position + " " + Symbol + " " + Length;
                if (ReferenceEquals(Job, null)) {
                    return temp;
                }
                var sb = new StringBuilder();
                int characterCount = Math.Min(Job.UnicodeCodePoints.Length - Position, 17);
                for (int i = 0; i < characterCount; ++i) {
                    sb.Append(Char.ConvertFromUtf32(Job.UnicodeCodePoints[Position + i]));
                }
                temp += " " + sb.ToString().Truncate(16);
                return temp;
            }
        }
    }
}