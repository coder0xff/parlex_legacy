//#define FORCE_SINGLE_THREAD

using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NondeterministicFiniteAutomata;

namespace Parlex
{
    // A zero-based index into the source text
    using Position = Int32;
    // A character count
    using Length = Int32;
    using Recognizer = Grammar.Recognizer;

    public class Parser
    {
        /// <summary>
        /// All matches with the same Position, and recognizer are in the same MatchCategory
        /// </summary>
        public class MatchCategory
        {
            public readonly Position Position;
            public readonly Grammar.ISymbol Symbol;

            protected MatchCategory(MatchCategory other)
            {
                Position = other.Position;
                Symbol = other.Symbol;
            }

            public MatchCategory(Position position, Grammar.ISymbol symbol)
            {
                Position = position;
                Symbol = symbol;
            }

            public override bool Equals(object obj)
            {
                var castObj = obj as MatchCategory;
                if (castObj == null) return false;
                return castObj.Position.Equals(Position) && castObj.Symbol.Equals(Symbol);
            }

            public override Length GetHashCode()
            {
                unchecked
                {
                    var hash = 17;
                    hash = hash * 31 + Position.GetHashCode();
                    hash = hash * 32 + Symbol.GetHashCode();
                    return hash;
                }
            }

            public override string ToString()
            {
                return Position + " " + Symbol;
            }
        }

        /// <summary>
        /// All matches with the same Position, Length, and recognizer are in the same MatchClass.
        /// </summary>
        public class MatchClass : MatchCategory
        {
            public readonly Length Length;

            protected MatchClass(MatchClass other) : base(other)
            {
                Length = other.Length;
            }

            public MatchClass(MatchCategory matchCategory, Length length) : base(matchCategory) {
                Length = length;
            }

            public MatchClass(Position position, Grammar.ISymbol symbol, Length length)
                : base(position, symbol)
            {
                Length = length;
            }

            public override bool Equals(object obj)
            {
                var castObj = obj as MatchClass;
                if (castObj == null) return false;
                return base.Equals(castObj) && Length.Equals(castObj.Length);
            }

            public override Length GetHashCode()
            {
                unchecked
                {
                    var hash = 17;
                    hash = hash * 31 + base.GetHashCode();
                    hash = hash * 32 + Length.GetHashCode();
                    return hash;
                }
            }

            public override string ToString()
            {
                return Position + " " + Symbol + " " + Length;
            }
        }

        public class Match : MatchClass
        {
            public readonly MatchClass[] Children;

            public Match(MatchClass matchClass, MatchClass[] children) : base(matchClass)
            {
                Children = children;
            }           
        }

        public class AbstractSyntaxForest
        {
            public MatchClass Root;
            public Dictionary<MatchClass, List<Match>> NodeTable;

            public bool IsAmbiguous
            {
                get
                { return NodeTable.Keys.Any(matchClass => NodeTable[matchClass].Count > 1); }
            }
        }

        public class Job
        {
            internal class SubJob : MatchCategory
            {
                internal class RecognizerState
                {
                    private readonly SubJob _subJob;
                    private readonly Position _position;
                    private readonly NondeterministicFiniteAutomaton<Grammar.ISymbol>.State[] _states;
                    private bool IsAcceptState { get { return _states.Any(x => ((Recognizer)_subJob.Symbol).AcceptStates.Contains(x)); } }
                    int _unterminatedSubsequentRecognizerStateCount;
                    bool _subsequentMadeMatch;
                    readonly RecognizerState _antecedent;
                    private readonly MatchClass _entranceMatchClass;

                    public RecognizerState(SubJob subJob, Position position, NondeterministicFiniteAutomaton<Grammar.ISymbol>.State[] states, RecognizerState antecedent = null, MatchClass entranceMatchClass = null) {
                        _subJob = subJob;
                        _position = position;
                        _states = states;
                        _antecedent = antecedent;
                        _entranceMatchClass = entranceMatchClass;
                        subJob.RecognizerStateCreated();
                        if (antecedent != null)
                        {
                            antecedent.SubsequentStateCreated();
                        }
#if FORCE_SINGLE_THREAD
                        Evaluate();
#else
                        new Thread(_ => Evaluate()).Start();
#endif
                    }

                    private void SubsequentStateCreated()
                    {
                        Interlocked.Increment(ref _unterminatedSubsequentRecognizerStateCount);
                    }

                    private void SubsequentStateTerminated()
                    {
                        if (Interlocked.Decrement(ref _unterminatedSubsequentRecognizerStateCount) == 0)
                        {
                            Terminate();
                        }
                    }

                    private void SetSubsequentMadeMatch()
                    {
                        _subsequentMadeMatch = true;
                        if (_antecedent != null) _antecedent.SetSubsequentMadeMatch();
                    }

                    IEnumerable<Grammar.ISymbol> GetCandidateSymbols()
                    {
                        var results = new List<Grammar.ISymbol>();
                        foreach (var state in _states)
                        {
                            results.AddRange(((Recognizer)_subJob.Symbol).TransitionFunction[state].Keys);
                        }
                        results = results.Distinct().ToList();
                        return results;
                    }

                    void Apply(MatchClass match)
                    {
                        var nextStates = new List<NondeterministicFiniteAutomaton<Grammar.ISymbol>.State>();
                        foreach (var currentState in _states)
                        {
                            if (((Recognizer)_subJob.Symbol).TransitionFunction[currentState].Keys.Contains(match.Symbol))
                            {
                                nextStates.AddRange(((Recognizer)_subJob.Symbol).TransitionFunction[currentState][match.Symbol]);
                            }
                        }
                        nextStates = nextStates.Distinct().ToList();
// ReSharper disable once ObjectCreationAsStatement
                        new RecognizerState(_subJob, _position + match.Length, nextStates.ToArray(), this, match);
                    }

                    void Evaluate()
                    {
                        var searches = GetCandidateSymbols().Select(symbol => new MatchCategory(_position, symbol)).ToList();
                        foreach (var search in searches)
                        {
                            _subJob._job.BeginMatching(search);
                        }
                        var didApply = false;
                        foreach (var search in searches)
                        {
                            foreach (var matchClass in _subJob._job.GetMatchClasses(search))
                            {
                                didApply = true;
                                Apply(matchClass);
                            }
                        }
                        if (!didApply)
                        {
                            Terminate();
                        }
                    }

                    private void Terminate()
                    {
                        if (IsAcceptState) 
                        {
                            if (!((Recognizer)_subJob.Symbol).Greedy || !_subsequentMadeMatch)
                            {
                                _subJob.AddMatch(new Match(new MatchClass(_subJob.Position, _subJob.Symbol, _position - _subJob.Position), GetChildren().ToArray()));
                                if (_antecedent != null)
                                {
                                    _antecedent.SetSubsequentMadeMatch();
                                }
                            }
                        }
                        if (_antecedent != null)
                        {
                            _antecedent.SubsequentStateTerminated();
                        }
                        _subJob.RecognizerStateTerminated();
                    }

                    private IEnumerable<MatchClass> GetChildren()
                    {
                        var results = new List<MatchClass>();
                        if (_antecedent != null)
                        {
                            results.AddRange(_antecedent.GetChildren());
                        }
                        if (_entranceMatchClass != null)
                        {
                            results.Add(_entranceMatchClass);
                        }
                        return results;
                    }
                }

                private readonly Job _job;

                int _unterminatedRecognizerStateCount;

                readonly AsyncSet<MatchClass> _matchClasses = new AsyncSet<MatchClass>();
                readonly JaggedAutoDictionary<MatchClass, ConcurrentSet<Match>> _matches = new JaggedAutoDictionary<MatchClass, ConcurrentSet<Match>>(_ => new ConcurrentSet<Match>());

                public SubJob(Job job, MatchCategory matchCategory) : base(matchCategory)
                {
                    _job = job;
                    job.SubJobCreated();
                    CreateFirstRecognizerState();
                }

                void CreateFirstRecognizerState()
                {
// ReSharper disable once ObjectCreationAsStatement
                    new RecognizerState(this, Position, ((Recognizer)Symbol).StartStates.ToArray());
                }

                private void AddMatch(Match match)
                {
                    _matchClasses.Add(match);
                    _matches[match].TryAdd(match);
                }

                private void RecognizerStateCreated()
                {
                    Interlocked.Increment(ref _unterminatedRecognizerStateCount);
                }

                private void RecognizerStateTerminated()
                {
                    if (Interlocked.Decrement(ref _unterminatedRecognizerStateCount) == 0)
                    {
                        Terminate();
                    }
                }

                private void Terminate()
                {
                    _matchClasses.Close();
                    _job.SubJobTerminated();
                }

                public IEnumerable<MatchClass> GetMatchClasses()
                {
                    return _matchClasses;
                }

                public IEnumerable<Match> GetMatches(MatchClass matchClass)
                {
                    if (_matches.Keys.Contains(matchClass))
                    {
                        return _matches[matchClass];
                    }
                    return new Match[] { };
                }
            }

            private readonly Grammar _grammar;
            public readonly String Text;
            private readonly Int32[] _unicodeCodePoints;
            private readonly JaggedAutoDictionary<MatchCategory, SubJob> _subJobs;
            private readonly JaggedAutoDictionary<MatchCategory, List<Match>> _terminalMatches;
            int _unterminatedSubJobAndConstructorCount = 1;
            readonly ManualResetEventSlim _blocker = new ManualResetEventSlim(false);
            public bool IsDone { get { return _blocker.IsSet; } }
            public AbstractSyntaxForest AbstractSyntaxForest { get; private set; }
            public Job(Grammar grammar, String text)
            {
                _grammar = grammar;
                Text = text;
                _unicodeCodePoints = text.GetUtf32CodePoints();

                _subJobs = new JaggedAutoDictionary<MatchCategory, SubJob>(matchCategory => new SubJob(this, matchCategory));
                _terminalMatches = new JaggedAutoDictionary<MatchCategory, List<Match>>(_ => new List<Match>());

                CreateFirstSubJob();
                ConstructorTerminated();
            }

            void CreateFirstSubJob()
            {
                BeginMatching(new MatchCategory(0, _grammar.MainProduction));
            }

            void ConstructorTerminated()
            {
                SubJobTerminated();
            }

            private IEnumerable<MatchClass> GetMatchClasses(MatchCategory matchCategory)
            {
                if (matchCategory.Symbol is Recognizer)
                {
                    return _subJobs[matchCategory].GetMatchClasses();
                }
                if (_terminalMatches.Keys.Contains(matchCategory))
                {
                    return _terminalMatches[matchCategory];
                }
                return new MatchClass[] { };
            }

            private void BeginMatching(MatchCategory search)
            {
                if (search.Symbol is Recognizer)
                {
                    _subJobs.EnsureCreated(search);
                }
                else
                {
                    var asTerminal = (Grammar.ITerminal)search.Symbol;
                    if (_terminalMatches.EnsureCreated(search)) {
                        if (asTerminal.Matches(_unicodeCodePoints, search.Position)) {
                            _terminalMatches[search].Add(new Match(new MatchClass(search, asTerminal.Length), new MatchClass[] { }));
                        }
                    }
                }
            }

            private void SubJobCreated()
            {
                Interlocked.Increment(ref _unterminatedSubJobAndConstructorCount);
            }

            private void SubJobTerminated()
            {
                if (Interlocked.Decrement(ref _unterminatedSubJobAndConstructorCount) == 0)
                {
                    Terminate();
                }
            }

            private void PruneAbstractSyntaxForest()
            {
                var usedMatchClasses = new HashSet<MatchClass> {AbstractSyntaxForest.Root};
                var priorAdditions = new HashSet<MatchClass> {AbstractSyntaxForest.Root};
                var addedAnyClasses = true;
                while (addedAnyClasses)
                {
                    addedAnyClasses = false;
                    var toAdds = new HashSet<MatchClass>();
                    foreach (var matchClass in priorAdditions)
                    {
                        if (AbstractSyntaxForest.NodeTable.ContainsKey(matchClass))
                        {
                            foreach (var match in AbstractSyntaxForest.NodeTable[matchClass])
                            {
                                foreach (var child in match.Children)
                                {
                                    toAdds.Add(child);
                                }
                            }
                        }
                    }
                    priorAdditions.Clear();
                    foreach (var toAdd in toAdds)
                    {
                        if (!usedMatchClasses.Contains(toAdd))
                        {
                            addedAnyClasses = true;
                            usedMatchClasses.Add(toAdd);
                            priorAdditions.Add(toAdd);
                        }
                    }
                }
                var toRemoves = AbstractSyntaxForest.NodeTable.Keys.Where(matchClass => !usedMatchClasses.Contains(matchClass)).ToList();
                foreach (var matchClassToRemove in toRemoves)
                {
                    AbstractSyntaxForest.NodeTable.Remove(matchClassToRemove);
                }
            }

            private void AddProductionMatchesToAbstractSyntaxForest()
            {
                foreach (var subJob in _subJobs.Values)
                {
                    foreach (var matchClass in subJob.GetMatchClasses())
                    {
                        var classMatches = (ConcurrentSet<Match>)subJob.GetMatches(matchClass);
                        if (classMatches.Count == 0) continue;
                        if (!AbstractSyntaxForest.NodeTable.Keys.Contains(matchClass))
                        {
                            AbstractSyntaxForest.NodeTable[matchClass] = new List<Match>();
                        }
                        var subTable = AbstractSyntaxForest.NodeTable[matchClass];
                        subTable.AddRange(classMatches);
                    }
                }
            }

            private void AddTerminalMatchesToAbstractSyntaxGraph()
            {
                foreach (var matches in _terminalMatches.Values)
                {
                    foreach (var match in matches)
                    {
                        if (!AbstractSyntaxForest.NodeTable.Keys.Contains(match))
                        {
                            AbstractSyntaxForest.NodeTable[match] = new List<Match>();
                        }
                        AbstractSyntaxForest.NodeTable[match].Add(match);
                    }
                }
            }

            private void ConstructAbstractSyntaxForest()
            {
                AbstractSyntaxForest = new AbstractSyntaxForest
                {
                    Root = new MatchClass(0, _grammar.MainProduction, _unicodeCodePoints.Length),
                    NodeTable = new Dictionary<MatchClass, List<Match>>()
                };
                AddProductionMatchesToAbstractSyntaxForest();
                AddTerminalMatchesToAbstractSyntaxGraph();
                PruneAbstractSyntaxForest();
            }

            private void Terminate()
            {
                ConstructAbstractSyntaxForest();
                _blocker.Set();
            }

            public void Wait()
            {
                _blocker.Wait();
            }
        }

        private readonly Grammar _grammar;

        public Parser(Grammar grammar)
        {
            _grammar = grammar;
        }

        public Job Parse(String text)
        {
            return new Job(_grammar, text);
        }
    }
}
