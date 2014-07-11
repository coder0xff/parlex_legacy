//#define FORCE_SINGLE_THREAD

using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex
{
    // A zero-based index into the source text
    using Position = System.Int32;
    // A character count
    using Length = System.Int32;
    using Recognizer = Grammar.Recognizer;

    public class Parser
    {
        /// <summary>
        /// All matches with the same position, and recognizer are in the same MatchCategory
        /// </summary>
        public class MatchCategory
        {
            public readonly Position position;
            public readonly Grammar.Symbol symbol;

            public MatchCategory(MatchCategory other)
            {
                this.position = other.position;
                this.symbol = other.symbol;
            }

            public MatchCategory(Position position, Grammar.Symbol symbol)
            {
                this.position = position;
                this.symbol = symbol;
            }

            public override bool Equals(object obj)
            {
                MatchCategory castObj = obj as MatchCategory;
                if (castObj == null) return false;
                return castObj.position.Equals(position) && castObj.symbol.Equals(symbol);
            }

            public override Length GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + position.GetHashCode();
                    hash = hash * 32 + symbol.GetHashCode();
                    return hash;
                }
            }

            public override string ToString()
            {
                return position + " " + symbol;
            }
        }

        /// <summary>
        /// All matches with the same position, length, and recognizer are in the same MatchClass.
        /// </summary>
        public class MatchClass : MatchCategory
        {
            public readonly Length length;

            public MatchClass(MatchClass other) : base(other)
            {
                length = other.length;
            }

            public MatchClass(MatchCategory matchCategory, Length length) : base(matchCategory) {
                this.length = length;
            }

            public MatchClass(Position position, Grammar.Symbol symbol, Length length)
                : base(position, symbol)
            {
                this.length = length;
            }

            public override bool Equals(object obj)
            {
                MatchClass castObj = obj as MatchClass;
                if (castObj == null) return false;
                return base.Equals(castObj) && length.Equals(castObj.length);
            }

            public override Length GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + base.GetHashCode();
                    hash = hash * 32 + length.GetHashCode();
                    return hash;
                }
            }

            public override string ToString()
            {
                return position + " " + symbol + " " + length;
            }
        }

        public class Match : MatchClass
        {
            public readonly MatchClass[] children;

            public Match(MatchClass matchClass, MatchClass[] children) : base(matchClass)
            {
                this.children = children;
            }           
        }

        public class AbstractSyntaxForest
        {
            public MatchClass root;
            public Dictionary<MatchClass, List<Match>> nodeTable;

            public bool IsAmbiguous
            {
                get
                {
                    foreach (MatchClass matchClass in nodeTable.Keys)
                    {
                        if (nodeTable[matchClass].Count > 1) return true;
                    }
                    return false;
                }
            }
        }

        public class Job
        {
            public class SubJob : MatchCategory
            {
                public class RecognizerState
                {
                    public readonly SubJob subJob;
                    public readonly Position position;
                    public readonly Recognizer.State[] states;
                    public bool isAcceptState { get { return states.Any(x => ((Recognizer)subJob.symbol).AcceptStates.Contains(x)); } }
                    int unterminatedSubsequentRecognizerStateCount = 0;
                    bool subsequentMadeMatch = false;
                    readonly RecognizerState antecedent;
                    public MatchClass entranceMatchClass;

                    public RecognizerState(SubJob subJob, Position position, Recognizer.State[] states, RecognizerState antecedent = null, MatchClass entranceMatchClass = null) {
                        this.subJob = subJob;
                        this.position = position;
                        this.states = states;
                        this.antecedent = antecedent;
                        this.entranceMatchClass = entranceMatchClass;
                        subJob.RecognizerStateCreated();
                        if (antecedent != null)
                        {
                            antecedent.SubsequentStateCreated();
                        }
#if FORCE_SINGLE_THREAD
                        Evaluate();
#else
                        new System.Threading.Thread(_ => Evaluate()).Start();
#endif
                    }

                    public void SubsequentStateCreated()
                    {
                        System.Threading.Interlocked.Increment(ref unterminatedSubsequentRecognizerStateCount);
                    }

                    public void SubsequentStateTerminated()
                    {
                        if (System.Threading.Interlocked.Decrement(ref unterminatedSubsequentRecognizerStateCount) == 0)
                        {
                            Terminate();
                        }
                    }

                    public void SetSubsequentMadeMatch()
                    {
                        subsequentMadeMatch = true;
                        if (antecedent != null) antecedent.SetSubsequentMadeMatch();
                    }

                    IEnumerable<Grammar.Symbol> GetCandidateSymbols()
                    {
                        List<Grammar.Symbol> results = new List<Grammar.Symbol>();
                        foreach (Recognizer.State state in states)
                        {
                            results.AddRange(((Recognizer)subJob.symbol).TransitionFunction[state].Keys);
                        }
                        results = results.Distinct().ToList();
                        return results;
                    }

                    void Apply(MatchClass match)
                    {
                        List<Recognizer.State> nextStates = new List<Nfa<Grammar.Symbol>.State>();
                        foreach (Recognizer.State currentState in states)
                        {
                            if (((Recognizer)subJob.symbol).TransitionFunction[currentState].Keys.Contains(match.symbol))
                            {
                                nextStates.AddRange(((Recognizer)subJob.symbol).TransitionFunction[currentState][match.symbol]);
                            }
                        }
                        nextStates = nextStates.Distinct().ToList();
                        new RecognizerState(subJob, position + match.length, nextStates.ToArray(), this, match);
                    }

                    void Evaluate()
                    {
                        List<MatchCategory> searches = GetCandidateSymbols().Select(symbol => new MatchCategory(position, symbol)).ToList();
                        foreach (MatchCategory search in searches)
                        {
                            subJob.job.BeginMatching(search);
                        }
                        bool didApply = false;
                        foreach (MatchCategory search in searches)
                        {
                            foreach (MatchClass matchClass in subJob.job.GetMatchClasses(search))
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

                    public void Terminate()
                    {
                        if (isAcceptState) 
                        {
                            if (!((Recognizer)subJob.symbol).Greedy || !subsequentMadeMatch)
                            {
                                subJob.AddMatch(new Match(new MatchClass(subJob.position, subJob.symbol, position - subJob.position), GetChildren().ToArray()));
                                if (antecedent != null)
                                {
                                    antecedent.SetSubsequentMadeMatch();
                                }
                            }
                        }
                        if (antecedent != null)
                        {
                            antecedent.SubsequentStateTerminated();
                        }
                        subJob.RecognizerStateTerminated();
                    }

                    public IEnumerable<MatchClass> GetChildren()
                    {
                        List<MatchClass> results = new List<MatchClass>();
                        if (antecedent != null)
                        {
                            results.AddRange(antecedent.GetChildren());
                        }
                        if (entranceMatchClass != null)
                        {
                            results.Add(entranceMatchClass);
                        }
                        return results;
                    }
                }

                public readonly Job job;

                int unterminatedRecognizerStateCount = 0;

                AsyncSet<MatchClass> matchClasses = new AsyncSet<MatchClass>();
                JaggedAutoDictionary<MatchClass, ConcurrentSet<Match>> matches = new JaggedAutoDictionary<MatchClass, ConcurrentSet<Match>>(_ => new ConcurrentSet<Match>());

                public SubJob(Job job, MatchCategory matchCategory) : base(matchCategory)
                {
                    this.job = job;
                    job.SubJobCreated();
                    CreateFirstRecognizerState();
                }

                void CreateFirstRecognizerState()
                {
                    new RecognizerState(this, position, ((Recognizer)symbol).StartStates.ToArray());
                }

                public void AddMatch(Match match)
                {
                    matchClasses.Add(match);
                    matches[match].TryAdd(match);
                }

                public void RecognizerStateCreated()
                {
                    System.Threading.Interlocked.Increment(ref unterminatedRecognizerStateCount);
                }

                public void RecognizerStateTerminated()
                {
                    if (System.Threading.Interlocked.Decrement(ref unterminatedRecognizerStateCount) == 0)
                    {
                        Terminate();
                    }
                }

                public void Terminate()
                {
                    matchClasses.Close();
                    job.SubJobTerminated();
                }

                public IEnumerable<MatchClass> GetMatchClasses()
                {
                    return matchClasses;
                }

                public IEnumerable<Match> GetMatches(MatchClass matchClass)
                {
                    if (matches.Keys.Contains(matchClass))
                    {
                        return matches[matchClass];
                    }
                    else
                    {
                        return new Match[] { };
                    }
                }
            }

            public readonly Grammar grammar;
            public readonly String text;
            public readonly Int32[] unicodeCodePoints;
            public readonly JaggedAutoDictionary<MatchCategory, SubJob> subJobs;
            public readonly JaggedAutoDictionary<MatchCategory, List<Match>> terminalMatches;
            int unterminatedSubJobAndConstructorCount = 1;
            System.Threading.ManualResetEventSlim blocker = new System.Threading.ManualResetEventSlim(false);
            public bool IsDone { get { return blocker.IsSet; } }
            public AbstractSyntaxForest abstractSyntaxForest { get; private set; }
            public Job(Grammar grammar, String text)
            {
                this.grammar = grammar;
                this.text = text;
                this.unicodeCodePoints = text.GetUtf32CodePoints();

                subJobs = new JaggedAutoDictionary<MatchCategory, SubJob>(matchCategory => new SubJob(this, matchCategory));
                terminalMatches = new JaggedAutoDictionary<MatchCategory, List<Match>>(_ => new List<Match>());

                CreateFirstSubJob();
                ConstructorTerminated();
            }

            void CreateFirstSubJob()
            {
                BeginMatching(new MatchCategory(0, grammar.MainProduction));
            }

            void ConstructorTerminated()
            {
                SubJobTerminated();
            }

            public IEnumerable<MatchClass> GetMatchClasses(MatchCategory matchCategory)
            {
                if (matchCategory.symbol is Recognizer)
                {
                    return subJobs[matchCategory].GetMatchClasses();
                }
                else
                {
                    if (terminalMatches.Keys.Contains(matchCategory))
                    {
                        return terminalMatches[matchCategory];
                    }
                    else
                    {
                        return new MatchClass[] { };
                    }
                }
            }

            public void BeginMatching(MatchCategory search)
            {
                if (search.symbol is Recognizer)
                {
                    subJobs.EnsureCreated(search);
                }
                else
                {
                    Grammar.Terminal asTerminal = (Grammar.Terminal)search.symbol;
                    if (terminalMatches.EnsureCreated(search)) {
                        if (asTerminal.Matches(unicodeCodePoints, search.position)) {
                            terminalMatches[search].Add(new Match(new MatchClass(search, asTerminal.Length), new MatchClass[] { }));
                        }
                    }
                }
            }

            public void SubJobCreated()
            {
                System.Threading.Interlocked.Increment(ref unterminatedSubJobAndConstructorCount);
            }

            public void SubJobTerminated()
            {
                if (System.Threading.Interlocked.Decrement(ref unterminatedSubJobAndConstructorCount) == 0)
                {
                    Terminate();
                }
            }

            public void PruneAbstractSyntaxForest()
            {
                HashSet<MatchClass> usedMatchClasses = new HashSet<MatchClass>();
                usedMatchClasses.Add(abstractSyntaxForest.root);
                HashSet<MatchClass> priorAdditions = new HashSet<MatchClass>();
                priorAdditions.Add(abstractSyntaxForest.root);
                bool addedAnyClasses = true;
                while (addedAnyClasses)
                {
                    addedAnyClasses = false;
                    HashSet<MatchClass> toAdds = new HashSet<MatchClass>();
                    foreach (MatchClass matchClass in priorAdditions)
                    {
                        if (abstractSyntaxForest.nodeTable.ContainsKey(matchClass))
                        {
                            foreach (Match match in abstractSyntaxForest.nodeTable[matchClass])
                            {
                                foreach (MatchClass child in match.children)
                                {
                                    toAdds.Add(child);
                                }
                            }
                        }
                    }
                    priorAdditions.Clear();
                    foreach (MatchClass toAdd in toAdds)
                    {
                        if (!usedMatchClasses.Contains(toAdd))
                        {
                            addedAnyClasses = true;
                            usedMatchClasses.Add(toAdd);
                            priorAdditions.Add(toAdd);
                        }
                    }
                }
                List<MatchClass> toRemoves = new List<MatchClass>();
                foreach (var matchClass in abstractSyntaxForest.nodeTable.Keys)
                {
                    if (!usedMatchClasses.Contains(matchClass))
                    {
                        toRemoves.Add(matchClass);
                    }
                }
                foreach (var matchClassToRemove in toRemoves)
                {
                    abstractSyntaxForest.nodeTable.Remove(matchClassToRemove);
                }
            }

            public void AddProductionMatchesToAbstractSyntaxForest()
            {
                foreach (SubJob subJob in subJobs.Values)
                {
                    foreach (MatchClass matchClass in subJob.GetMatchClasses())
                    {
                        ConcurrentSet<Match> classMatches = (ConcurrentSet<Match>)subJob.GetMatches(matchClass);
                        if (classMatches.Count == 0) continue;
                        if (!abstractSyntaxForest.nodeTable.Keys.Contains(matchClass))
                        {
                            abstractSyntaxForest.nodeTable[matchClass] = new List<Match>();
                        }
                        List<Match> subTable = abstractSyntaxForest.nodeTable[matchClass];
                        foreach (Match match in classMatches)
                        {
                            subTable.Add(match);
                        }
                    }
                }
            }

            public void AddTerminalMatchesToAbstractSyntaxGraph()
            {
                foreach (List<Match> matches in terminalMatches.Values)
                {
                    foreach (Match match in matches)
                    {
                        if (!abstractSyntaxForest.nodeTable.Keys.Contains(match))
                        {
                            abstractSyntaxForest.nodeTable[match] = new List<Match>();
                        }
                        abstractSyntaxForest.nodeTable[match].Add(match);
                    }
                }
            }

            public void ConstructAbstractSyntaxForest()
            {
                abstractSyntaxForest = new AbstractSyntaxForest();
                abstractSyntaxForest.root = new MatchClass(0, grammar.MainProduction, unicodeCodePoints.Length);
                abstractSyntaxForest.nodeTable = new Dictionary<MatchClass, List<Match>>();
                AddProductionMatchesToAbstractSyntaxForest();
                AddTerminalMatchesToAbstractSyntaxGraph();
                PruneAbstractSyntaxForest();
            }

            public void Terminate()
            {
                ConstructAbstractSyntaxForest();
                blocker.Set();
            }

            public void Wait()
            {
                blocker.Wait();
            }
        }

        public Grammar grammar;

        public Parser(Grammar grammar)
        {
            this.grammar = grammar;
        }

        public Job Parse(String text)
        {
            return new Job(grammar, text);
        }
    }
}
