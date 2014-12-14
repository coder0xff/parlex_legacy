using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automata;
using Synchronox;

using Position = System.Int32;
using Length = System.Int32;

namespace Parlex {
    public class Parser {
        /// <summary>
        ///     All matches with the same Position and recognizer are in the same MatchCategory
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
        ///     All matches with the same Position, Length and recognizer are in the same MatchClass.
        /// </summary>
        public class MatchClass {
            public readonly MatchCategory Category;
            public readonly Length Length;

            public Grammar.ISymbol Symbol { get { return Category.Symbol; } }
            public Position Position { get { return Category.Position; } }
            public Job Job { get { return Category.Job; } }

            protected MatchClass(MatchClass other) {
                Category = other.Category;
                Length = other.Length;
            }

            public MatchClass(MatchCategory matchCategory, Length length) {
                Category = matchCategory;
                Length = length;
            }

            internal MatchClass(Position position, Grammar.ISymbol symbol, Length length, Job job) {
                Category = new MatchCategory(position, symbol, job);
                Length = length;
            }

            public override bool Equals(object obj) {
                var castObj = obj as MatchClass;
                if (ReferenceEquals(null, castObj)) {
                    return false;
                }
                return Category.Equals(castObj.Category) && Length.Equals(castObj.Length);
            }

            public override Length GetHashCode() {
                unchecked {
                    int hash = 17;
                    hash = hash * 31 + Category.GetHashCode();
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

        public class Match {
            public MatchClass[] Children;
            public readonly MatchClass MatchClass;
            public Position Position { get { return MatchClass.Position; } }
            public Grammar.ISymbol Symbol { get { return MatchClass.Symbol; } }
            public Length Length { get { return MatchClass.Length; } }
            public Job Job { get { return MatchClass.Job; } }

            public Match(MatchClass matchClass, MatchClass[] children) {
                MatchClass = matchClass;
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

        public class AbstractSyntaxForest {
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
        }

        public class Job : Collective {
            private readonly Grammar _grammar;
            public String Text { get; private set; }
            internal readonly Int32[] UnicodeCodePoints;
            private readonly Parser.MatchClass _root;
            private readonly JaggedAutoDictionary<MatchCategory, SubJob> _subJobs;
            private readonly JaggedAutoDictionary<MatchCategory, TerminalMatcher> _terminalMatches;
            public AbstractSyntaxForest AbstractSyntaxForest { get; private set; }
            internal Job(Grammar grammar, String document, Grammar.ISymbol mainSymbol, Position startPosition, Length length) {
                _grammar = grammar;
                Text = document;
                UnicodeCodePoints = document.GetUtf32CodePoints();
                if (length < 0) {
                    length = UnicodeCodePoints.Length - startPosition;
                }
                _root = new MatchClass(startPosition, mainSymbol, length, this);
                _subJobs = new JaggedAutoDictionary<MatchCategory, SubJob>(matchCategory => {
                    ////Debug.WriteLine("BeginMatching(" + matchCategory + ")");
                    return new SubJob(matchCategory);
                });

                _terminalMatches = new JaggedAutoDictionary<MatchCategory, TerminalMatcher>(category => new TerminalMatcher(category));

                MakeFirstSubJob(mainSymbol, startPosition);
                ConstructionCompleted();
            }

            internal class SubJob : Node {
                private readonly MatchCategory _matchCategory;
                private readonly ConcurrentSet<MatchClass> _matchClasses = new ConcurrentSet<MatchClass>();
                private readonly JaggedAutoDictionary<MatchClass, ConcurrentSet<Match>> _matches = new JaggedAutoDictionary<MatchClass, ConcurrentSet<Match>>(_ => new ConcurrentSet<Match>());

                private Position Position { get { return _matchCategory.Position; } }
                private Grammar.ISymbol Symbol { get { return _matchCategory.Symbol; } }
                private Grammar.Recognizer Recognizer { get { return (Grammar.Recognizer)Symbol; } }
                private Job Job { get { return _matchCategory.Job; } }

                // ReSharper disable MemberCanBePrivate.Global
                public readonly Input<Match> MatchInput = null;
                public readonly Output<MatchClass> MatchClassOutput = null;
                // ReSharper restore MemberCanBePrivate.Global

                public SubJob(MatchCategory matchCategory)
                    : base(matchCategory.Job) {
                    _matchCategory = matchCategory;
                    ////Debug.WriteLine("Created SubJob " + this);
                    ConstructionCompleted();
                }

                protected override void Initializer() {
                    var firstConfiguration = new RecognizerState(this, Position, ((Grammar.Recognizer)Symbol).StartStates, null, null);
                }

                internal class RecognizerState : Node {
                    private readonly SubJob _subJob;
                    private Grammar.Recognizer Recognizer { get { return _subJob.Recognizer; } }
                    private Job Job { get { return _subJob.Job; } }
                    private readonly Position _position;
                    private readonly Nfa<Grammar.ISymbol>.State[] _configuration;
                    private readonly RecognizerState _antecedent;
                    private readonly MatchClass _entranceMatchClass;
                    private int _subsequentRecognizerStateCount;
                    private readonly ManualResetEventSlim _subsequentRecognizersCompleted = new ManualResetEventSlim(true);
                    private bool _subsequentMadeMatch;

                    private bool IsAcceptConfiguration {
                        get { return _configuration.Any(x => Recognizer.AcceptStates.Contains(x)); }
                    }

                    // ReSharper disable MemberCanBePrivate.Global
                    public readonly Input<MatchClass> MatchClassInput = null;
                    public readonly Output<Match> MatchOutput = null;
                    // ReSharper restore MemberCanBePrivate.Global

                    public RecognizerState(SubJob subJob, Position position, IEnumerable<Nfa<Grammar.ISymbol>.State> configuration, RecognizerState antecedent, MatchClass entranceMatchClass)
                        : base(subJob.Job) {
                        _subJob = subJob;
                        _position = position;
                        _configuration = configuration.ToArray();
                        _antecedent = antecedent;
                        if (_antecedent != null) _antecedent.SubsequentCreated();
                        _entranceMatchClass = entranceMatchClass;
                        ////Debug.WriteLine("Created RecognizerState " + this);
                        ConstructionCompleted();
                        Job.Connect(_subJob.MatchInput, MatchOutput);
                    }

                    private void SubsequentCreated() {
                        Interlocked.Increment(ref _subsequentRecognizerStateCount);
                        _subsequentRecognizersCompleted.Reset();
                    }

                    private void SetSubsequentMadeMatch() {
                        _subsequentMadeMatch = true;
                        if (_antecedent != null) _antecedent.SetSubsequentMadeMatch();
                    }

                    private IEnumerable<Grammar.ISymbol> GetCandidateSymbols() {
                        var results = new List<Grammar.ISymbol>();
                        foreach (Nfa<Grammar.ISymbol>.State state in _configuration) {
                            results.AddRange(Recognizer.TransitionFunction[state].Keys);
                        }
                        results = results.Distinct().ToList();
                        return results;
                    }

                    protected override void Initializer() {
                        if (!Recognizer.Greedy) {
                            OutputResults();
                        }
                        foreach (var candidateSymbol in GetCandidateSymbols()) {
                            var matchCategory = new MatchCategory(_position, candidateSymbol, Job);
                            Job.Connect(MatchClassInput, Job.GetMatchClassOutput(matchCategory));
                        }
                    }

                    protected override void Computer() {
                        while (true) {
                            //Debug.WriteLine("RecognizerState (" + this + ") entering Dequeue");
                            MatchClass matchClass;
                            if (!MatchClassInput.Dequeue(out matchClass)) return;
                            //Debug.WriteLine("RecognizerState (" + this + ") got matchClass " + matchClass);
                            var nextStates = new List<Nfa<Grammar.ISymbol>.State>();
                            foreach (Nfa<Grammar.ISymbol>.State currentState in _configuration) {
                                if (Recognizer.TransitionFunction[currentState].Keys.Contains(matchClass.Symbol)) {
                                    nextStates.AddRange(Recognizer.TransitionFunction[currentState][matchClass.Symbol]);
                                }
                            }
                            nextStates = nextStates.Distinct().ToList();
                            var nextPosition = _position + matchClass.Length;
                            // ReSharper disable once ObjectCreationAsStatement
                            new RecognizerState(_subJob, nextPosition, nextStates.ToArray(), this, matchClass);
                        }
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

                    private void OutputResults() {
                        if (IsAcceptConfiguration &&
                            (!Recognizer.Greedy || !_subsequentMadeMatch)) {
                            if (_antecedent != null) _antecedent.SetSubsequentMadeMatch();
                            MatchOutput.Enqueue(new Match(new MatchClass(_subJob.Position, _subJob.Symbol, _position - _subJob.Position, _subJob.Job), GetChildren().ToArray()));
                        }
                    }

                    protected override void Terminator() {
                        //Debug.WriteLine("Terminating RecognizerState " + this);
                        if (Recognizer.Greedy) {
                            _subsequentRecognizersCompleted.Wait();
                            OutputResults();
                        }
                        if (_antecedent != null) _antecedent.TerminatedSubsequent();
                        //Debug.WriteLine("Terminated RecognizerState " + this);
                    }

                    private void TerminatedSubsequent() {
                        if (Interlocked.Decrement(ref _subsequentRecognizerStateCount) == 0) {
                            _subsequentRecognizersCompleted.Set();
                        }
                    }

                    public override string ToString() {
                        return "SubJob (" + _subJob + ") at " + _position;
                    }
                }

                protected override void Computer() {
                    while (true) {
                        Match match;
                        if (!MatchInput.Dequeue(out match)) return;
                        //Debug.WriteLine("SubJob (" + this + ") got match " + match);
                        _matches[match.MatchClass].TryAdd(match);
                        if (_matchClasses.TryAdd(match.MatchClass)) {
                            MatchClassOutput.Enqueue(match.MatchClass);
                        }
                    }
                }

                internal IEnumerable<MatchClass> GetMatchClasses() {
                    return _matchClasses;
                }

                internal IEnumerable<Match> GetMatches(MatchClass matchClass) {
                    if (_matches.Keys.Contains(matchClass)) {
                        return _matches[matchClass];
                    }
                    return new Match[] { };
                }

                public override string ToString() {
                    return Symbol.Name + " at " + Position;
                }
            }

            private Output<MatchClass> GetMatchClassOutput(MatchCategory search) {
                if (search.Symbol is Grammar.Recognizer) {
                    return _subJobs[search].MatchClassOutput;
                } else {
                    return _terminalMatches[search].MatchClassOutput;
                }
            }

            private class TerminalMatcher : Node {
                private readonly MatchCategory _matchCategory;
                private Match _match;

                internal TerminalMatcher(MatchCategory matchCategory)
                    : base(matchCategory.Job) {
                    _matchCategory = matchCategory;
                    ConstructionCompleted();
                }

                // ReSharper disable once MemberCanBePrivate.Global
                public readonly Output<MatchClass> MatchClassOutput = null;

                protected override void Initializer() {
                    var terminal = (Grammar.ITerminal)_matchCategory.Symbol;
                    if (terminal.Matches(_matchCategory.Job.UnicodeCodePoints, _matchCategory.Position)) {
                        var matchClass = new MatchClass(_matchCategory, terminal.Length);
                        _match = new Match(matchClass, new MatchClass[0]);
                        MatchClassOutput.Enqueue(matchClass);
                    }
                }

                protected override void Computer() { }

                internal IEnumerable<Match> GetMatches() {
                    return _match != null ? new[] { _match } : new Match[0];
                }
            }

            private void MakeFirstSubJob(Grammar.ISymbol mainSymbol, int startPosition) {
                var documentMatchCategory = new MatchCategory(startPosition, mainSymbol, this);
                GetMatchClassOutput(documentMatchCategory);
            }

            private void PruneAbstractSyntaxForest() {
                var usedMatchClasses = new HashSet<MatchClass> { AbstractSyntaxForest.Root };
                var priorAdditions = new HashSet<MatchClass> { AbstractSyntaxForest.Root };
                bool addedAnyClasses = true;
                while (addedAnyClasses) {
                    addedAnyClasses = false;
                    var toAdds = new HashSet<MatchClass>();
                    foreach (MatchClass matchClass in priorAdditions) {
                        if (AbstractSyntaxForest.NodeTable.ContainsKey(matchClass)) {
                            foreach (Match match in AbstractSyntaxForest.NodeTable[matchClass]) {
                                foreach (MatchClass child in match.Children) {
                                    toAdds.Add(child);
                                }
                            }
                        } else if (matchClass == AbstractSyntaxForest.Root) {
                            var temp = AbstractSyntaxForest.NodeTable.Where(x => x.Key.Symbol == matchClass.Symbol);
                            var length = temp.Select(x => x.Key.Length).Max();
                            var followingText = Text.Utf32Substring(length);
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
                var toRemoves = AbstractSyntaxForest.NodeTable.Keys.Where(matchClass => !usedMatchClasses.Contains(matchClass)).ToList();
                foreach (var matchClassToRemove in toRemoves) {
                    AbstractSyntaxForest.NodeTable.Remove(matchClassToRemove);
                }
            }

            private void AddProductionMatchesToAbstractSyntaxForest() {
                foreach (var subJob in _subJobs.Values) {
                    foreach (var matchClass in subJob.GetMatchClasses()) {
                        var classMatches = (ConcurrentSet<Match>)subJob.GetMatches(matchClass);
                        if (classMatches.Count == 0) {
                            continue;
                        }
                        if (!AbstractSyntaxForest.NodeTable.Keys.Contains(matchClass)) {
                            AbstractSyntaxForest.NodeTable[matchClass] = new List<Match>();
                        }
                        List<Match> subTable = AbstractSyntaxForest.NodeTable[matchClass];
                        subTable.AddRange(classMatches);
                    }
                }
            }

            private void AddTerminalMatchesToAbstractSyntaxGraph() {
                foreach (var matcher in _terminalMatches.Values) {
                    foreach (Match match in matcher.GetMatches()) {
                        if (!AbstractSyntaxForest.NodeTable.Keys.Contains(match.MatchClass)) {
                            AbstractSyntaxForest.NodeTable[match.MatchClass] = new List<Match>();
                        }
                        AbstractSyntaxForest.NodeTable[match.MatchClass].Add(match);
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

            protected override void Terminator() {
                ConstructAbstractSyntaxForest();
            }
        }

        private readonly Grammar _grammar;

        public Parser(Grammar grammar) {
            _grammar = grammar;
        }

        public Job Parse(String document, Position startPosition = 0, Length length = -1, Grammar.ISymbol mainSymbol = null) {
            if (mainSymbol == null) {
                mainSymbol = _grammar.MainSymbol;
            }
            return new Job(_grammar, document, mainSymbol, startPosition, length);
        }
    }
}
