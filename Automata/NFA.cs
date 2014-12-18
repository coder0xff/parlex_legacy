using System;
using System.Collections.Concurrent;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Collections.Generic.More;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.More;
using System.Text;
using System.Threading;
using System.Threading.More;
using System.Threading.Tasks;

namespace Automata {
    /// <summary>
    ///     A Nondeterministic Finite Automaton (∪-Nfa) with the ability to store an additional value with each State
    /// </summary>
    /// <typeparam name="TAlphabet">The domain of the transition function is S x TAlphabet, where S is the set of states.</typeparam>
    /// <typeparam name="TAssignment">The type of the value associated with a state.</typeparam>
    public class Nfa<TAlphabet, TAssignment> {
        private readonly HashSet<State> _acceptStates = new HashSet<State>();
        private readonly HashSet<State> _startStates = new HashSet<State>();
        private readonly HashSet<State> _states = new HashSet<State>();

        private readonly JaggedAutoDictionary<State, TAlphabet, HashSet<State>> _transitionFunction = new JaggedAutoDictionary<State, TAlphabet, HashSet<State>>((dontCare0, dontCare1) => new HashSet<State>());

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public HashSet<State> States {
            get { return _states; }
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public JaggedAutoDictionary<State, TAlphabet, HashSet<State>> TransitionFunction {
            get { return _transitionFunction; }
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public HashSet<State> StartStates {
            get { return _startStates; }
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public HashSet<State> AcceptStates {
            get { return _acceptStates; }
        }

        public String GraphvizDotLanguage {
            get {
                var result = new StringBuilder();
                var nodeNames = new AutoDictionary<State, string>(x => x.Value.ToString());
                result.AppendLine("digraph nfa {");
                result.AppendLine("\trankdir=LR;");
                result.AppendLine("\tsize=\"8,5\"");
                result.AppendLine("\tnode [shape = point]; start;");

                result.Append("\tnode [shape = doublecircle];");
                foreach (State acceptState in AcceptStates) {
                    result.Append(" ");
                    result.Append(nodeNames[acceptState]);
                }
                result.AppendLine(";");
                result.AppendLine("\tnode [shape = circle];");
                foreach (State startState in StartStates) {
                    result.Append("\tstart -> ");
                    result.Append(nodeNames[startState]);
                    result.AppendLine(";");
                }
                foreach (Transition transition in GetTransitions()) {
                    result.Append("\t");
                    result.Append(nodeNames[transition.FromState]);
                    result.Append(" -> ");
                    result.Append(nodeNames[transition.ToState]);
                    result.Append(" [ label = \"");
                    result.Append(transition.Symbol);
                    result.AppendLine("\" ];");
                }
                result.AppendLine("}");

                return result.ToString();
            }
        }

        public IEnumerable<Transition> GetTransitions() {
            return
                _transitionFunction.SelectMany(
                    x => x.Value.SelectMany(y => y.Value.Select(z => new Transition {FromState = x.Key, Symbol = y.Key, ToState = z})));
        }

        public HashSet<State> TransitionFunctionExtended(IEnumerable<State> fromStates, TAlphabet input) {
            if (fromStates == null) {
                throw new ArgumentNullException("fromStates");
            }
            var result = new HashSet<State>();
            foreach (State state in fromStates) {
                result.UnionWith(_transitionFunction[state][input]);
            }
            return result;
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public HashSet<State> TransitionFunctionExtended(State fromState, IEnumerable<TAlphabet> inputString) {
            var currentStateSet = new HashSet<State> {fromState};
            return inputString.Aggregate(currentStateSet, TransitionFunctionExtended);
        }

        public HashSet<State> TransitionFunctionExtended(IEnumerable<State> fromStates, IEnumerable<TAlphabet> inputString) {
            var currentStateSet = new HashSet<State>(fromStates);
            return inputString.Aggregate(currentStateSet, TransitionFunctionExtended);
        }

        /// <summary>
        ///     Creates a new Nfa that has only one transition for each input symbol for each state - i.e. it is deterministic
        /// </summary>
        /// <returns>The new DFA</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Determinize")]
        public Nfa<TAlphabet, StateSet> Determinize() {
            var stateSetToDeterminizedState = new ConcurrentDictionary<StateSet, Nfa<TAlphabet, StateSet>.State>();
            var result = new Nfa<TAlphabet, StateSet>();
            var resultAcceptStates = new ConcurrentBag<Nfa<TAlphabet, StateSet>.State>();

            Func<StateSet, Nfa<TAlphabet, StateSet>.State> adder = null;
            adder = stateSet => stateSetToDeterminizedState.GetOrAdd(stateSet, stateSetProxy => {
                var newState = new Nfa<TAlphabet, StateSet>.State(stateSetProxy);
                Task.Factory.StartNew(() => {
                    bool isAcceptState = stateSetProxy.Any(x => AcceptStates.Contains(x));
                    if (isAcceptState) {
                        resultAcceptStates.Add(newState);
                    }
                    IEnumerable<TAlphabet> transitions = stateSetProxy.Select(x => TransitionFunction[x]).SelectMany(y => y.Keys).Distinct();
                    Parallel.ForEach(transitions, transition => {
                        HashSet<State> nextStateSet = TransitionFunctionExtended(stateSetProxy, transition);
                        if (nextStateSet.Count > 0) {
                            Nfa<TAlphabet, StateSet>.State nextState = adder(new StateSet(nextStateSet));
                            result.TransitionFunction[newState][transition].Add(nextState);
                        }
                    });
                }, TaskCreationOptions.AttachedToParent);
                return newState;
            });

            var startStateSet = new StateSet(StartStates);
            Task.Factory.StartNew(() => { result.StartStates.Add(adder(startStateSet)); }).Wait();
            result.States.UnionWith(stateSetToDeterminizedState.Values);
            foreach (Nfa<TAlphabet, StateSet>.State acceptState in resultAcceptStates) {
                result.AcceptStates.Add(acceptState);
            }
            return result;
        }

        /// <summary>
        ///     Creates a new Nfa that recognizes the reversed language
        /// </summary>
        /// <returns>The new Nfa</returns>
        public Nfa<TAlphabet, TAssignment> Dual() {
            var result = new Nfa<TAlphabet, TAssignment>();
            result._startStates.UnionWith(_acceptStates);
            result._states.UnionWith(_states);
            foreach (var keyValuePair in _transitionFunction) {
                foreach (var valuePair in keyValuePair.Value) {
                    foreach (State state in valuePair.Value) {
                        result._transitionFunction[state][valuePair.Key].Add(keyValuePair.Key);
                    }
                }
            }
            result._acceptStates.UnionWith(_startStates);
            return result;
        }

        /// <summary>
        ///     Creates a state map (SM) as described in [1]
        /// </summary>
        /// <returns></returns>
        private StateMap MakeStateMap(out Nfa<TAlphabet, StateSet> determinized) {
            determinized = Determinize();
            Nfa<TAlphabet, StateSet> determinizedDual = Dual().Determinize();

            List<Nfa<TAlphabet, StateSet>.State> orderedRows = determinized.States.ToList();
            orderedRows.Remove(determinized.StartStates.First());
            orderedRows.Insert(0, determinized.StartStates.First());

            List<Nfa<TAlphabet, StateSet>.State> orderedColumns = determinizedDual.States.ToList();
            orderedColumns.Remove(determinizedDual.StartStates.First());
            orderedColumns.Insert(0, determinizedDual.StartStates.First());

            var result = new StateMap(orderedRows.Count, orderedColumns.Count);
            for (int rowIndex = 0; rowIndex < orderedRows.Count; rowIndex++) {
                Nfa<TAlphabet, StateSet>.State rowState = orderedRows[rowIndex];
                var rowStateSet = new StateSet(rowState.Value);
                result.Rows.Left.Add(rowStateSet, rowIndex);
                for (int columnIndex = 0; columnIndex < orderedColumns.Count; columnIndex++) {
                    Nfa<TAlphabet, StateSet>.State columnState = orderedColumns[columnIndex];
                    var columnStateSet = new StateSet(columnState.Value);
                    result.Map[rowIndex, columnIndex] = new HashSet<State>(rowStateSet.Intersect(columnStateSet));
                }
            }
            for (int columnIndex = 0; columnIndex < orderedColumns.Count; columnIndex++) {
                Nfa<TAlphabet, StateSet>.State columnState = orderedColumns[columnIndex];
                var columnStateSet = new StateSet(columnState.Value);
                result.Columns.Left.Add(columnStateSet, columnIndex);
            }
            return result;
        }

        private static bool[,] MakeElementaryAutomatonMatrix(StateMap stateMap) {
            var result = new bool[stateMap.Rows.Count, stateMap.Columns.Count];
            for (int rowIndex = 0; rowIndex < stateMap.Rows.Count; rowIndex++) {
                for (int columnIndex = 0; columnIndex < stateMap.Columns.Count; columnIndex++) {
                    result[rowIndex, columnIndex] = stateMap.Map[rowIndex, columnIndex].Count > 0;
                }
            }
            return result;
        }

        private static Nfa<TAlphabet, int> GenerateEquivalenceClassReducedDfa(Nfa<TAlphabet, StateSet> subsetConstructionDfa, Dictionary<StateSet, int> equivalenceClassLookup) {
            var result = new Nfa<TAlphabet, int>();
            var intToResultState = new AutoDictionary<int, Nfa<TAlphabet, int>.State>(i => new Nfa<TAlphabet, int>.State(i));
            result.StartStates.Add(intToResultState[equivalenceClassLookup[subsetConstructionDfa.StartStates.First().Value]]);
            foreach (Nfa<TAlphabet, StateSet>.State acceptState in subsetConstructionDfa.AcceptStates) {
                result.AcceptStates.Add(intToResultState[equivalenceClassLookup[acceptState.Value]]);
            }
            foreach (var keyValuePair in subsetConstructionDfa.TransitionFunction) {
                Nfa<TAlphabet, int>.State fromState = intToResultState[equivalenceClassLookup[keyValuePair.Key.Value]];
                foreach (var valuePair in keyValuePair.Value) {
                    TAlphabet inputSymbol = valuePair.Key;
                    foreach (Nfa<TAlphabet, StateSet>.State state in valuePair.Value) {
                        Nfa<TAlphabet, int>.State toState = intToResultState[equivalenceClassLookup[state.Value]];
                        result.TransitionFunction[fromState][inputSymbol].Add(toState);
                    }
                }
            }
            result.States.UnionWith(intToResultState.Values);
            return result;
        }

        private static ReducedStateMap ReduceStateMap(StateMap stateMap, Nfa<TAlphabet, StateSet> subsetConstructionDfa, out Nfa<TAlphabet, int> minimizedSubsetConstructionDfa) {
            //construct an elementary automata matrix (EAM) [1]
            bool[,] elementaryAutomataMatrix = MakeElementaryAutomatonMatrix(stateMap);

            //determine which rows can be merged
            var rowsToMerge = new List<HashSet<int>>();
            {
                List<int> unmergedRows = Enumerable.Range(0, stateMap.Rows.Count).ToList();
                while (unmergedRows.Count > 0) {
                    rowsToMerge.Add(new HashSet<int> {unmergedRows[0]});
                    for (int rowIndex = 1; rowIndex < unmergedRows.Count; rowIndex++) {
                        int columnIndex;
                        for (columnIndex = 0; columnIndex < stateMap.Columns.Count; columnIndex++) {
                            if (elementaryAutomataMatrix[unmergedRows[0], columnIndex] != elementaryAutomataMatrix[unmergedRows[rowIndex], columnIndex]) {
                                break;
                            }
                        }
                        if (columnIndex != stateMap.Columns.Count) {
                            continue;
                        }
                        rowsToMerge[rowsToMerge.Count - 1].Add(unmergedRows[rowIndex]);
                        unmergedRows.RemoveAt(rowIndex);
                        rowIndex--;
                    }
                    unmergedRows.RemoveAt(0);
                }
            }

            //determine which columns can be merged
            var columnsToMerge = new List<HashSet<int>>();
            {
                List<int> unmergedColumns = Enumerable.Range(0, stateMap.Columns.Count).ToList();
                while (unmergedColumns.Count > 0) {
                    columnsToMerge.Add(new HashSet<int> {unmergedColumns[0]});
                    for (int columnIndex = 1; columnIndex < unmergedColumns.Count; columnIndex++) {
                        int rowIndex;
                        for (rowIndex = 0; rowIndex < stateMap.Rows.Count; rowIndex++) {
                            if (elementaryAutomataMatrix[rowIndex, unmergedColumns[0]] != elementaryAutomataMatrix[rowIndex, unmergedColumns[columnIndex]]) {
                                break;
                            }
                        }
                        if (rowIndex != stateMap.Rows.Count) {
                            continue;
                        }
                        columnsToMerge[columnsToMerge.Count - 1].Add(unmergedColumns[columnIndex]);
                        unmergedColumns.RemoveAt(columnIndex);
                        columnIndex--;
                    }
                    unmergedColumns.RemoveAt(0);
                }
            }

            var result = new ReducedStateMap(rowsToMerge.Count, columnsToMerge.Count);
            var stateSetToEquivalenceClassRowIndex = new Dictionary<StateSet, int>();
            for (int equivalenceClassRowIndex = 0; equivalenceClassRowIndex < rowsToMerge.Count; equivalenceClassRowIndex++) {
                foreach (int row in rowsToMerge[equivalenceClassRowIndex]) {
                    stateSetToEquivalenceClassRowIndex[stateMap.Rows.Right[row]] = equivalenceClassRowIndex;
                }
                var rowName = new ReadOnlyHashSet<int>(rowsToMerge[equivalenceClassRowIndex]);
                result.Rows.Left.Add(rowName, equivalenceClassRowIndex);
            }
            minimizedSubsetConstructionDfa = GenerateEquivalenceClassReducedDfa(subsetConstructionDfa, stateSetToEquivalenceClassRowIndex);

            for (int equivalenceClassColumnIndex = 0; equivalenceClassColumnIndex < columnsToMerge.Count; equivalenceClassColumnIndex++) {
                var columnName = new ReadOnlyHashSet<int>(columnsToMerge[equivalenceClassColumnIndex]);
                result.Columns.Left.Add(columnName, equivalenceClassColumnIndex);
            }

            for (int equivalenceClassRowIndex = 0; equivalenceClassRowIndex < rowsToMerge.Count; equivalenceClassRowIndex++) {
                for (int equivalenceClassColumnIndex = 0; equivalenceClassColumnIndex < columnsToMerge.Count; equivalenceClassColumnIndex++) {
                    HashSet<State> statesUnion = result.Map[equivalenceClassRowIndex, equivalenceClassColumnIndex] = new HashSet<State>();
                    foreach (int rowIndex in rowsToMerge[equivalenceClassRowIndex]) {
                        foreach (int columnIndex in columnsToMerge[equivalenceClassColumnIndex]) {
                            statesUnion.UnionWith(stateMap.Map[rowIndex, columnIndex]);
                        }
                    }
                }
            }

            return result;
        }

        private static bool[,] MakeReducedAutomataMatrix(ReducedStateMap reducedStateMap) {
            var result = new bool[reducedStateMap.Rows.Count, reducedStateMap.Columns.Count];
            for (int rowIndex = 0; rowIndex < reducedStateMap.Rows.Count; rowIndex++) {
                for (int columnIndex = 0; columnIndex < reducedStateMap.Columns.Count; columnIndex++) {
                    result[rowIndex, columnIndex] = reducedStateMap.Map[rowIndex, columnIndex].Count > 0;
                }
            }
            return result;
        }

        private static Grid[] ComputePrimeGrids(bool[,] reducedAutomataMatrix) {
            var gridsToProcess = new DistinctRecursiveAlgorithmProcessor<Grid>();
            int rowCount = reducedAutomataMatrix.GetUpperBound(0) + 1;
            int columnCount = reducedAutomataMatrix.GetUpperBound(1) + 1;

            //make initial grids which contain only one element
            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++) {
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++) {
                    if (reducedAutomataMatrix[rowIndex, columnIndex]) {
                        var grid = new Grid(new[] {rowIndex}, new[] {columnIndex});
                        gridsToProcess.Add(grid);
                    }
                }
            }

            //then, grow them incrementally, adding them back into the queue
            //or saving them if they cannot be grown
            var results = new ConcurrentSet<Grid>();
            gridsToProcess.Run(grid => {
                bool isPrime = true;
                //try expanding to other rows
                {
                    int comparisonRow = grid.Rows.First();
                    foreach (int testRow in Enumerable.Range(0, rowCount).Except(grid.Rows)) {
                        bool canExpand = grid.Columns.All(columnIndex => reducedAutomataMatrix[testRow, columnIndex] == reducedAutomataMatrix[comparisonRow, columnIndex]);
                        if (!canExpand) {
                            continue;
                        }
                        var newGrid = new Grid(grid.Rows.Concat(new[] {testRow}), grid.Columns);
                        gridsToProcess.Add(newGrid);
                        isPrime = false;
                    }
                }
                //try expanding to other columns
                {
                    int comparisonColumn = grid.Columns.First();
                    foreach (int testColumn in Enumerable.Range(0, columnCount).Except(grid.Columns)) {
                        bool canExpand = grid.Rows.All(rowIndex => reducedAutomataMatrix[rowIndex, testColumn] == reducedAutomataMatrix[rowIndex, comparisonColumn]);
                        if (!canExpand) {
                            continue;
                        }
                        var newGrid = new Grid(grid.Rows, grid.Columns.Concat(new[] {testColumn}));
                        gridsToProcess.Add(newGrid);
                        isPrime = false;
                    }
                }
                //if it's prime, then save it to the results
                if (isPrime) {
                    results.TryAdd(grid);
                }
            });
            return results.ToArray();
        }

        private static IEnumerable<Cover> EnumerateCovers(Grid[] primeGrids, int firstGridIndex, Dictionary<Grid, HashSet<int>> gridToFlattenedIndicesSet, HashSet<int> flattenedIndicesWithTrue, int gridCount) {
            if (gridCount > primeGrids.Length - firstGridIndex) { //can't reach gridCount == 0 before the recursion runs out of grids
                yield break;
            }
            for (int gridIndex = firstGridIndex; gridIndex < primeGrids.Length; gridIndex++) {
                Grid primeGrid = primeGrids[gridIndex];
                var primeGridAsEnumerable = new[] {primeGrid};
                var remainingFlattenedIndicesWithTrueToSatisfy = new HashSet<int>(flattenedIndicesWithTrue);
                remainingFlattenedIndicesWithTrueToSatisfy.ExceptWith(gridToFlattenedIndicesSet[primeGrid]);
                if (gridCount == 1) {
                    if (remainingFlattenedIndicesWithTrueToSatisfy.Count == 0) {
                        yield return new Cover(primeGridAsEnumerable);
                    }
                } else {
                    foreach (Cover enumerateCover in EnumerateCovers(primeGrids, gridIndex + 1, gridToFlattenedIndicesSet, remainingFlattenedIndicesWithTrueToSatisfy, gridCount - 1)) {
                        yield return new Cover(enumerateCover.Concat(primeGridAsEnumerable));
                    }
                }
            }
        }

        private static IEnumerable<Cover> EnumerateCovers(bool[,] reducedAutomataMatrix, Grid[] primeGrids) {
            int rowCount = reducedAutomataMatrix.GetUpperBound(0) + 1;
            int columnCount = reducedAutomataMatrix.GetUpperBound(1) + 1;

            var flattenedIndicesWithTrue = new HashSet<int>();
            for (int row = 0; row < rowCount; row++) {
                for (int column = 0; column < columnCount; column++) {
                    if (reducedAutomataMatrix[row, column]) {
                        flattenedIndicesWithTrue.Add(column*rowCount + row);
                    }
                }
            }

            if (flattenedIndicesWithTrue.Count == 0) {
                yield break;
            }

            Dictionary<Grid, HashSet<int>> gridToFlattenedIndicesSet = primeGrids.ToDictionary(grid => grid, grid => {
                var flattenedIndices = new HashSet<int>();
                foreach (int row in grid.Rows) {
                    foreach (int column in grid.Columns) {
                        flattenedIndices.Add(column*rowCount + row);
                    }
                }
                return flattenedIndices;
            });

            for (int gridCount = 1; gridCount <= primeGrids.Length; gridCount++) {
                foreach (Cover enumerateCover in EnumerateCovers(primeGrids, 0, gridToFlattenedIndicesSet, flattenedIndicesWithTrue, gridCount)) {
                    yield return enumerateCover;
                }
            }
        }

        private static AutoDictionary<int, HashSet<Grid>> MakeSubsetAssignmentFunction(Cover cover) {
            var result = new AutoDictionary<int, HashSet<Grid>>(dontCare0 => new HashSet<Grid>());
            foreach (Grid grid in cover) {
                foreach (int row in grid.Rows) {
                    result[row].Add(grid);
                }
            }
            return result;
        }

        private static Nfa<TAlphabet, int> FromIntersectionRule(Nfa<TAlphabet, int> reducedDfa, Cover cover, out Bimap<int, Grid> orderedGrids) {
            List<Nfa<TAlphabet, int>.State> orderedReducedDfaStates = reducedDfa._states.OrderBy(x => x.Value).ToList();
            AutoDictionary<int, HashSet<Grid>> subsetAssignmentFunction = MakeSubsetAssignmentFunction(cover);
            int counter = 0;
            Bimap<int, Grid> orderedGridsTemp = cover.ToBimap(x => counter++, x => x);
            var result = new Nfa<TAlphabet, int>();
            var intToResultState = new AutoDictionary<int, Nfa<TAlphabet, int>.State>(i => new Nfa<TAlphabet, int>.State(i));
            for (int resultStateIndex = 0; resultStateIndex < orderedGridsTemp.Count; resultStateIndex++) {
                Grid grid = orderedGridsTemp.Left[resultStateIndex];
                Nfa<TAlphabet, int>.State resultState = intToResultState[resultStateIndex];
                JaggedAutoDictionary<TAlphabet, HashSet<Nfa<TAlphabet, int>.State>> resultTransitionPartialLambda = result._transitionFunction[resultState];
                IEnumerable<Nfa<TAlphabet, int>.State> rows = grid.Rows.Select(rowIndex => orderedReducedDfaStates[rowIndex]);
                ReadOnlyHashSet<TAlphabet> symbols = ReadOnlyHashSet<TAlphabet>.IntersectMany(rows.Select(row => reducedDfa._transitionFunction[row].Keys));
                foreach (TAlphabet symbol in symbols) {
                    TAlphabet symbol1 = symbol;
                    IEnumerable<HashSet<Grid>> gridSets = rows.Select(row => subsetAssignmentFunction[reducedDfa._transitionFunction[row][symbol1].First().Value]);
                    ReadOnlyHashSet<Grid> nextGrids = ReadOnlyHashSet<Grid>.IntersectMany(gridSets);
                    IEnumerable<int> nextIndices = nextGrids.Select(nextGrid => orderedGridsTemp.Right[nextGrid]);
                    IEnumerable<Nfa<TAlphabet, int>.State> nextStates = nextIndices.Select(gridIndex => intToResultState[gridIndex]);
                    resultTransitionPartialLambda[symbol].UnionWith(nextStates);
                }
                if (grid.Columns.Contains(0)) {
                    result._acceptStates.Add(resultState);
                }
                if (grid.Rows.Contains(0)) {
                    result._startStates.Add(resultState);
                }
            }

            result._states.UnionWith(intToResultState.Values);
            orderedGrids = orderedGridsTemp;
            return result;
        }

        private static bool GridSetSpansRow(Bimap<int, Grid> orderedGrids, IEnumerable<int> gridIndices, bool[,] reducedAutomataMatrix, int rowIndex) {
            var neededColumns = new HashSet<int>(Enumerable.Range(0, reducedAutomataMatrix.GetUpperBound(1) + 1).Where(columnIndex => reducedAutomataMatrix[rowIndex, columnIndex]));
            foreach (int gridIndex in gridIndices) {
                Grid grid = orderedGrids.Left[gridIndex];
                if (grid.Rows.Contains(rowIndex)) {
                    neededColumns.ExceptWith(grid.Columns);
                    if (neededColumns.Count == 0) {
                        break;
                    }
                }
            }
            return neededColumns.Count == 0;
        }

        private static bool SubsetAssignmentIsLegitimate(Nfa<TAlphabet, int> intersectionRuleNFA, Nfa<TAlphabet, int> minimizedDfa, bool[,] reducedAutomataMatrix, Bimap<int, Grid> orderedGrids) {
            Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet> intersectionRuleDfa = intersectionRuleNFA.Determinize();
            List<Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet>.State> intersectionRuleDfaOrderedStates = intersectionRuleDfa.States.ToList();
            intersectionRuleDfaOrderedStates.Remove(intersectionRuleDfa.StartStates.First());
            intersectionRuleDfaOrderedStates.Insert(0, intersectionRuleDfa.StartStates.First());

            var processor = new DistinctRecursiveAlgorithmProcessor<KeyValuePair<Nfa<TAlphabet, int>.State /*minimized*/, Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet>.State /*intersection rule*/>>();
            processor.Add(new KeyValuePair<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet>.State>(minimizedDfa.StartStates.First(), intersectionRuleDfa.StartStates.First()));
            bool isLegitimate = true;
            processor.Run(pair => {
                if (isLegitimate) {
                    Nfa<TAlphabet, int>.State minimizedDfaState = pair.Key;
                    Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet>.State intersectionRuleDfaState = pair.Value;
                    IEnumerable<TAlphabet> inputSymbols = minimizedDfa.TransitionFunction[minimizedDfaState].Keys;
                    foreach (TAlphabet inputSymbol in inputSymbols) {
                        if (!intersectionRuleDfa.TransitionFunction[intersectionRuleDfaState][inputSymbol].Any()) {
                            isLegitimate = false;
                            continue;
                        }
                        Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet>.State nextIntersectionRuleDfaState = intersectionRuleDfa.TransitionFunction[intersectionRuleDfaState][inputSymbol].First();
                        Nfa<TAlphabet, int>.State nextMinimizedDfaState = minimizedDfa.TransitionFunction[minimizedDfaState][inputSymbol].First();
                        if (!intersectionRuleDfa.AcceptStates.Contains(nextIntersectionRuleDfaState) && minimizedDfa.AcceptStates.Contains(nextMinimizedDfaState)) {
                            isLegitimate = false;
                        } else if (!GridSetSpansRow(orderedGrids, nextIntersectionRuleDfaState.Value.Select(s => s.Value), reducedAutomataMatrix, nextMinimizedDfaState.Value)) {
                            isLegitimate = false;
                        } else {
                            processor.Add(new KeyValuePair<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet>.State>(nextMinimizedDfaState, nextIntersectionRuleDfaState));
                        }
                    }
                }
            });
            return isLegitimate;
        }

        public Nfa<TAlphabet, TAssignment2> Reassign<TAssignment2>(Func<State, TAssignment2> func) {
            var result = new Nfa<TAlphabet, TAssignment2>();
            var stateMapper = new AutoDictionary<State, Nfa<TAlphabet, TAssignment2>.State>(state => new Nfa<TAlphabet, TAssignment2>.State(func(state)));
            foreach (State state in _states) {
                stateMapper.EnsureCreated(state);
            }
            foreach (State state in _transitionFunction.Keys) {
                JaggedAutoDictionary<TAlphabet, HashSet<State>> sourcePartialEvaluation0 = _transitionFunction[state];
                JaggedAutoDictionary<TAlphabet, HashSet<Nfa<TAlphabet, TAssignment2>.State>> targetPartialEvaluation0 = result._transitionFunction[stateMapper[state]];
                foreach (TAlphabet inputSymbol in _transitionFunction[state].Keys) {
                    HashSet<State> sourcePartialEvaluation1 = sourcePartialEvaluation0[inputSymbol];
                    HashSet<Nfa<TAlphabet, TAssignment2>.State> targetPartialEvaluation1 = targetPartialEvaluation0[inputSymbol];
                    foreach (State state1 in sourcePartialEvaluation1) {
                        targetPartialEvaluation1.Add(stateMapper[state1]);
                    }
                }
            }
            result._startStates.UnionWith(_startStates.Select(state => stateMapper[state]));
            result._acceptStates.UnionWith(_acceptStates.Select(state => stateMapper[state]));
            result._states.UnionWith(stateMapper.Values);
            return result;
        }

        public Nfa<TAlphabet> Reassign() {
            var result = new Nfa<TAlphabet>();
            var stateMapper = new AutoDictionary<State, Nfa<TAlphabet>.State>(state => new Nfa<TAlphabet>.State());
            foreach (State state in _states) {
                stateMapper.EnsureCreated(state);
            }
            foreach (var fromStateKeyValuePair in TransitionFunction) {
                foreach (var transitionKeyValuePair in fromStateKeyValuePair.Value) {
                    foreach (State toState in transitionKeyValuePair.Value) {
                        result.TransitionFunction[stateMapper[fromStateKeyValuePair.Key]][transitionKeyValuePair.Key].Add(stateMapper[toState]);
                    }
                }
            }
            result.StartStates.UnionWith(_startStates.Select(state => stateMapper[state]));
            result.AcceptStates.UnionWith(_acceptStates.Select(state => stateMapper[state]));
            result.States.UnionWith(stateMapper.Values);
            return result;
        }

        /// <summary>
        ///     If there are any nodes that cannot be reached
        ///     or cannot reach an accept state then remove them
        /// </summary>
        public void RemoveRedundancies() {
            start:
            foreach (State state in States.ToArray()) {
                if (StartStates.Select(x => GetRoutes(x, state).Any()).All(x => x != true) &&
                    AcceptStates.Select(x => GetRoutes(state, x).Any()).All(x => x != true)) {
                    States.Remove(state);
                    AcceptStates.Remove(state);
                    StartStates.Remove(state);
                    _transitionFunction.TryRemove(state);
                    State state1 = state;
                    foreach (Transition transition in GetTransitions().Where(x => x.ToState == state1)) {
                        _transitionFunction[transition.FromState][transition.Symbol].Remove(state);
                        if (!_transitionFunction[transition.FromState][transition.Symbol].Any()) {
                            _transitionFunction[transition.FromState].TryRemove(transition.Symbol);
                        }
                    }
                    goto start;
                }
            }
        }

        /// <summary>
        ///     Minimize this Nfa using the Kameda-Weiner algorithm [1]
        /// </summary>
        /// <returns>A minimal-state Nfa accepting the same language</returns>
        public Nfa<TAlphabet, int> Minimized() {
            Nfa<TAlphabet, StateSet> determinized;
            StateMap sm = MakeStateMap(out determinized);
            Nfa<TAlphabet, int> minimizedSubsetConstructionDfa;
            ReducedStateMap rsm = ReduceStateMap(sm, determinized, out minimizedSubsetConstructionDfa);
            bool[,] ram = MakeReducedAutomataMatrix(rsm);
            Grid[] primeGrids = ComputePrimeGrids(ram);
            IEnumerable<Cover> covers = EnumerateCovers(ram, primeGrids);
            foreach (Cover cover in covers) {
                if (cover.Count == _states.Count) {
                    break;
                }
                Bimap<int, Grid> orderedGrids;
                Nfa<TAlphabet, int> minNFA = FromIntersectionRule(minimizedSubsetConstructionDfa, cover, out orderedGrids);
                bool isLegitimate = SubsetAssignmentIsLegitimate(minNFA, minimizedSubsetConstructionDfa, ram, orderedGrids);
                if (isLegitimate) {
                    minNFA.RemoveRedundancies();
                    return minNFA;
                }
            }
            int stateCount = 0;
            return Reassign(x => Interlocked.Increment(ref stateCount)); //did not find a smaller Nfa. Return this;
        }

        public static Nfa<TAlphabet, TAssignment> Union(IEnumerable<Nfa<TAlphabet, TAssignment>> nfas) {
            var result = new Nfa<TAlphabet, TAssignment>();
            foreach (var nfa in nfas) {
                //don't need to clone the states because they are immutable
                result._startStates.UnionWith(nfa._startStates);
                result._acceptStates.UnionWith(nfa._acceptStates);
                foreach (State fromState in nfa._transitionFunction.Keys) {
                    foreach (TAlphabet inputSymbol in nfa._transitionFunction[fromState].Keys) {
                        result._transitionFunction[fromState][inputSymbol].UnionWith(nfa._transitionFunction[fromState][inputSymbol]);
                    }
                }
                result._states.UnionWith(nfa._states);
            }
            return result;
        }

        public Nfa<TAlphabet, int> MinimizedDfa() {
            Nfa<TAlphabet, StateSet> determinized;
            StateMap sm = MakeStateMap(out determinized);
            Nfa<TAlphabet, int> minimizedSubsetConstructionDfa;
            ReduceStateMap(sm, determinized, out minimizedSubsetConstructionDfa);
            return minimizedSubsetConstructionDfa;
        }

        public bool IsEquivalent<TAssignment2>(Nfa<TAlphabet, TAssignment2> that) {
            Nfa<TAlphabet, int> thisMinDfa = MinimizedDfa();
            Nfa<TAlphabet, int> thatMinDfa = that.MinimizedDfa();
            bool equivalent = true;
            var stateMap = new ConcurrentDictionary<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, int>.State>();
            var processor = new DistinctRecursiveAlgorithmProcessor<KeyValuePair<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, int>.State>>();
            processor.Add(new KeyValuePair<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, int>.State>(thisMinDfa._startStates.First(), thatMinDfa._startStates.First())); //only one start state since it's a min dfa
            processor.Run(pair => {
                if (!equivalent) {
                    return;
                }
                foreach (var inputSymbolAndStates in thisMinDfa._transitionFunction[pair.Key]) {
                    TAlphabet thisMinDfaInputSymbol = inputSymbolAndStates.Key;
                    Nfa<TAlphabet, int>.State thisMinDfaNextState = inputSymbolAndStates.Value.First(); //deterministic, so only one state
                    HashSet<Nfa<TAlphabet, int>.State> thatMinDfaNextStates = thatMinDfa._transitionFunction[pair.Value][thisMinDfaInputSymbol];
                    if (thatMinDfaNextStates.Count != 1) { //it will always be either 0 or 1
                        equivalent = false;
                    } else {
                        Nfa<TAlphabet, int>.State thatMinDfaNextState = thatMinDfaNextStates.First();
                        Nfa<TAlphabet, int>.State mappedThisMinDfaNextState = stateMap.GetOrAdd(thisMinDfaNextState, thisMinDfaNextStateProxy => {
                            processor.Add(new KeyValuePair<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, int>.State>(thisMinDfaNextState, thatMinDfaNextState));
                            return thatMinDfaNextState;
                        });
                        if (thatMinDfaNextState != mappedThisMinDfaNextState) {
                            equivalent = false;
                        }
                    }
                }
            });
            return equivalent;
        }

        public static Nfa<TAlphabet, int> Intersect(IEnumerable<Nfa<TAlphabet, TAssignment>> nfas) {
            IEnumerable<Nfa<TAlphabet, int>> minDets = nfas.Select(nfa => nfa.MinimizedDfa());
            Nfa<TAlphabet, int> singleTransitionNFA = Nfa<TAlphabet, int>.Union(minDets);
            JaggedAutoDictionary<Nfa<TAlphabet, int>.State, TAlphabet, HashSet<Nfa<TAlphabet, int>.State>> singleTransitionFunction = singleTransitionNFA._transitionFunction;
            HashSet<Nfa<TAlphabet, int>.State> singleAcceptStates = singleTransitionNFA._acceptStates;
            int stateCount = 0;
            var resultStates = new AutoDictionary<ReadOnlyHashSet<Nfa<TAlphabet, int>.State>, Nfa<TAlphabet, int>.State>(x => new Nfa<TAlphabet, int>.State(Interlocked.Increment(ref stateCount)));
            var processor = new DistinctRecursiveAlgorithmProcessor<ReadOnlyHashSet<Nfa<TAlphabet, int>.State>>();
            var startStateSet = new ReadOnlyHashSet<Nfa<TAlphabet, int>.State>(minDets.Select(x => x._startStates.First()));
            var result = new Nfa<TAlphabet, int>();
            var acceptStates = new ConcurrentSet<Nfa<TAlphabet, int>.State>();
            processor.Add(startStateSet);
            processor.Run(stateSet => {
                Nfa<TAlphabet, int>.State fromState = resultStates[stateSet];
                if (singleAcceptStates.IsSupersetOf(stateSet)) {
                    acceptStates.TryAdd(fromState);
                }
                ReadOnlyHashSet<TAlphabet> fromSymbols = ReadOnlyHashSet<TAlphabet>.IntersectMany(stateSet.Select(state => singleTransitionFunction[state].Keys));
                foreach (TAlphabet fromSymbol in fromSymbols) {
                    TAlphabet symbol = fromSymbol;
                    var nextStateSet = new ReadOnlyHashSet<Nfa<TAlphabet, int>.State>(stateSet.Select(state => singleTransitionFunction[state][symbol].First()));
                    Nfa<TAlphabet, int>.State toState = resultStates[nextStateSet];
                    processor.Add(nextStateSet);
                    result._transitionFunction[fromState][fromSymbol].Add(toState);
                }
            });
            result._states.UnionWith(resultStates.Values);
            result._startStates.Add(resultStates[startStateSet]);
            result._acceptStates.UnionWith(acceptStates);
            return result;
        }

        public bool Contains(Nfa<TAlphabet, TAssignment> that) {
            return Intersect(new[] {this, that}).IsEquivalent(that);
        }

        private IEnumerable<IEnumerable<State>> GetRoutes(State fromState, State toState, HashSet<State> ignoredStates = null) {
            if (ignoredStates == null) {
                ignoredStates = new HashSet<State>();
            }
            List<State> subsequentStates = _transitionFunction[fromState].SelectMany(inputSymbolAndToStates => inputSymbolAndToStates.Value).Distinct().Where(s => !ignoredStates.Contains(s)).ToList();
            foreach (State subsequentState in subsequentStates) {
                if (subsequentState == toState) {
                    yield return new[] {toState};
                }
                ignoredStates.Add(subsequentState);
                foreach (var route in GetRoutes(subsequentState, toState, ignoredStates)) {
                    yield return new[] {subsequentState}.Concat(route);
                }
                ignoredStates.Remove(subsequentState);
            }
        }

        public IEnumerable<IEnumerable<State>> GetCycles() {
            return _states.SelectMany(state => GetRoutes(state, state, new HashSet<State>()));
        }

        public string ToString(Func<TAlphabet, String> transitionStringifier) {
            var result = new StringBuilder();
            int nodeCounter = 0;
            var labels = new AutoDictionary<State, int>(_ => Interlocked.Increment(ref nodeCounter));
            result.Append("Start: ");
            result.Append(String.Join(", ", StartStates.Select(x => labels[x]).ToArray()));
            //make sure non-accept states are numbered begore accept states
            foreach (State nonAccept in States.Except(AcceptStates)) {
                labels.EnsureCreated(nonAccept);
            }
            result.Append(" Accept: ");
            result.Append(String.Join(", ", AcceptStates.Select(x => labels[x]).ToArray()));
            result.Append(" Transitions:");
            result.Append(Environment.NewLine);

            foreach (var fromStateKeyValuePair in TransitionFunction) {
                string fromStateBuilder = labels[fromStateKeyValuePair.Key].ToString();
                foreach (var transitionKeyValuePair in fromStateKeyValuePair.Value) {
                    string transitionBuilder = transitionStringifier(transitionKeyValuePair.Key);
                    foreach (State toState in transitionKeyValuePair.Value) {
                        result.Append(fromStateBuilder);
                        result.Append(" -> ");
                        result.Append(transitionBuilder);
                        result.Append(" -> ");
                        result.Append(labels[toState]);
                        result.Append(Environment.NewLine);
                    }
                }
            }
            return result.ToString();
        }

        public override string ToString() {
            return ToString(x => x.ToString());
        }

        private class Cover : ReadOnlyHashSet<Grid> {
            public Cover(IEnumerable<Grid> items)
                : base(items) {}
        }

        private class Grid {
            public readonly ReadOnlyHashSet<int> Columns;
            public readonly ReadOnlyHashSet<int> Rows;

            public Grid(IEnumerable<int> rows, IEnumerable<int> columns) {
                Rows = new ReadOnlyHashSet<int>(rows);
                Columns = new ReadOnlyHashSet<int>(columns);
            }

            protected bool Equals(Grid other) {
                return Columns.Equals(other.Columns) && Rows.Equals(other.Rows);
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) {
                    return false;
                }
                if (ReferenceEquals(this, obj)) {
                    return true;
                }
                if (obj.GetType() != GetType()) {
                    return false;
                }
                return Equals((Grid)obj);
            }

            public override int GetHashCode() {
                unchecked {
                    return (Columns.GetHashCode()*397) ^ Rows.GetHashCode();
                }
            }

            public static bool operator ==(Grid left, Grid right) {
                return Equals(left, right);
            }

            public static bool operator !=(Grid left, Grid right) {
                return !Equals(left, right);
            }
        }

        private class ReducedStateMap {
            public readonly Bimap<ReadOnlyHashSet<int>, int> Columns = new Bimap<ReadOnlyHashSet<int>, int>();
            [SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")] public readonly HashSet<State>[,] Map;
            public readonly Bimap<ReadOnlyHashSet<int>, int> Rows = new Bimap<ReadOnlyHashSet<int>, int>();

            public ReducedStateMap(int rowCount, int columnCount) {
                Map = new HashSet<State>[rowCount, columnCount];
            }
        }

        /// <summary>
        ///     A State of an Nfa
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public class State {
            private readonly TAssignment _value;

            public State(TAssignment value) {
                _value = value;
            }

            public TAssignment Value {
                get { return _value; }
            }

            public override string ToString() {
                return _value.ToString();
            }
        }

        private class StateMap {
            public readonly Bimap<StateSet, int> Columns = new Bimap<StateSet, int>();
            [SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")] public readonly HashSet<State>[,] Map;
            public readonly Bimap<StateSet, int> Rows = new Bimap<StateSet, int>();

            public StateMap(int rowCount, int columnCount) {
                Map = new HashSet<State>[rowCount, columnCount];
            }
        }

        /// <summary>
        ///     An immutable set of States that can be quickly tested for inequality
        /// </summary>
        public class StateSet : ReadOnlyHashSet<State> {
            public StateSet(IEnumerable<State> items)
                : base(items) {}
        }

        public class Transition {
            public State FromState;
            public TAlphabet Symbol;
            public State ToState;
        }
    }

    /// <summary>
    ///     A Nondeterministic Finite Automaton (∪-Nfa)
    /// </summary>
    /// <typeparam name="TAlphabet">The domain of the transition function is S x TAlphabet, where S is the set of states.</typeparam>
    public class Nfa<TAlphabet> {
        public readonly HashSet<State> AcceptStates = new HashSet<State>();
        public readonly HashSet<State> StartStates = new HashSet<State>();
        public readonly HashSet<State> States = new HashSet<State>();
        public readonly JaggedAutoDictionary<State, TAlphabet, HashSet<State>> TransitionFunction = new JaggedAutoDictionary<State, TAlphabet, HashSet<State>>((dontCare0, dontCare1) => new HashSet<State>());

        public Nfa() {}

        public Nfa(Nfa<TAlphabet> other) {
            foreach (State state in other.StartStates) {
                StartStates.Add(state);
            }
            foreach (State state in other.States) {
                States.Add(state);
            }
            foreach (State state in other.AcceptStates) {
                AcceptStates.Add(state);
            }
            foreach (Transition transition in other.GetTransitions()) {
                TransitionFunction[transition.FromState][transition.Symbol].Add(transition.ToState);
            }
        }

        public String GraphvizDotLanguage {
            get {
                var result = new StringBuilder();
                int counter = 0;
                var nodeNames =
                    new AutoDictionary<State, string>(_ => "N" + Interlocked.Increment(ref counter).ToString());
                result.AppendLine("digraph nfa {");
                result.AppendLine("\trankdir=LR;");
                result.AppendLine("\tsize=\"8,5\"");
                result.AppendLine("\tnode [shape = point]; start;");

                result.Append("\tnode [shape = doublecircle];");
                foreach (State acceptState in AcceptStates) {
                    result.Append(" ");
                    result.Append(nodeNames[acceptState]);
                }
                result.AppendLine(";");
                result.AppendLine("\tnode [shape = circle];");
                foreach (State startState in StartStates) {
                    result.Append("\tstart -> ");
                    result.Append(nodeNames[startState]);
                    result.AppendLine(";");
                }
                foreach (State state in States) {
                    result.Append("\t");
                    result.Append(nodeNames[state]);
                    result.AppendLine(";");
                }
                foreach (Transition transition in GetTransitions()) {
                    result.Append("\t");
                    result.Append(nodeNames[transition.FromState]);
                    result.Append(" -> ");
                    result.Append(nodeNames[transition.ToState]);
                    result.Append(" [ label = \"");
                    result.Append(transition.Symbol);
                    result.AppendLine("\" ];");
                }
                result.AppendLine("}");

                return result.ToString();
            }
        }

        public IEnumerable<Transition> GetTransitions() {
            return (from fromStateKeyValuePair in TransitionFunction from transitionKeyValuePair in fromStateKeyValuePair.Value from toState in transitionKeyValuePair.Value select new Transition {
                FromState = fromStateKeyValuePair.Key,
                Symbol = transitionKeyValuePair.Key,
                ToState = toState
            }).ToArray();
        }

        public HashSet<State> TransitionFunctionEx(IEnumerable<State> states, TAlphabet input) {
            var result = new HashSet<State>();
            foreach (State state in states) {
                result.UnionWith(TransitionFunction[state][input]);
            }
            return result;
        }

        public HashSet<State> TransitionFunctionEx(State state, IEnumerable<TAlphabet> inputString) {
            var currentStateSet = new HashSet<State> {state};
            return inputString.Aggregate(currentStateSet, TransitionFunctionEx);
        }

        public HashSet<State> TransitionFunctionEx(IEnumerable<State> states, IEnumerable<TAlphabet> inputString) {
            var currentStateSet = new HashSet<State>(states);
            return inputString.Aggregate(currentStateSet, TransitionFunctionEx);
        }

        /// <summary>
        ///     Creates a new Nfa that has only one transition for each input symbol for each state - i.e. it is deterministic
        /// </summary>
        /// <returns>The new DFA</returns>
        public Nfa<TAlphabet, StateSet> Determinize() {
            var stateSetToDState = new ConcurrentDictionary<StateSet, Nfa<TAlphabet, StateSet>.State>();
            var result = new Nfa<TAlphabet, StateSet>();
            var resultAcceptStates = new ConcurrentBag<Nfa<TAlphabet, StateSet>.State>();

            Func<StateSet, Nfa<TAlphabet, StateSet>.State> adder = null;
            adder = stateSet => stateSetToDState.GetOrAdd(stateSet, stateSetProxy => {
                var newState = new Nfa<TAlphabet, StateSet>.State(stateSetProxy);
                Task.Factory.StartNew(() => {
                    bool isAcceptState = stateSetProxy.Any(x => AcceptStates.Contains(x));
                    if (isAcceptState) {
                        resultAcceptStates.Add(newState);
                    }
                    IEnumerable<TAlphabet> awayInputs = stateSetProxy.Select(x => TransitionFunction[x]).SelectMany(y => y.Keys).Distinct();
                    Parallel.ForEach(awayInputs, input => {
                        HashSet<State> nextStateSet = TransitionFunctionEx(stateSetProxy, input);
                        if (nextStateSet.Count > 0) {
                            Nfa<TAlphabet, StateSet>.State nextState = adder(new StateSet(nextStateSet));
                            result.TransitionFunction[newState][input].Add(nextState);
                        }
                    });
                }, TaskCreationOptions.AttachedToParent);
                return newState;
            });

            var startStateSet = new StateSet(StartStates);
            Task.Factory.StartNew(() => { result.StartStates.Add(adder(startStateSet)); }).Wait();
            result.States.UnionWith(stateSetToDState.Values);
            foreach (Nfa<TAlphabet, StateSet>.State acceptState in resultAcceptStates) {
                result.AcceptStates.Add(acceptState);
            }
            return result;
        }

        /// <summary>
        ///     Creates a new Nfa that recognizes the reversed language
        /// </summary>
        /// <returns>The new Nfa</returns>
        public Nfa<TAlphabet> Dual() {
            var result = new Nfa<TAlphabet>();
            result.StartStates.UnionWith(AcceptStates);
            result.States.UnionWith(States);
            foreach (var keyValuePair in TransitionFunction) {
                foreach (var valuePair in keyValuePair.Value) {
                    foreach (State state in valuePair.Value) {
                        result.TransitionFunction[state][valuePair.Key].Add(keyValuePair.Key);
                    }
                }
            }
            result.AcceptStates.UnionWith(StartStates);
            return result;
        }

        /// <summary>
        ///     Creates a state map (SM) as described in [1]
        /// </summary>
        /// <returns></returns>
        private StateMap MakeStateMap(out Nfa<TAlphabet, StateSet> determinized) {
            determinized = Determinize();
            Nfa<TAlphabet, StateSet> determinizedDual = Dual().Determinize();

            List<Nfa<TAlphabet, StateSet>.State> orderedRows = determinized.States.ToList();
            orderedRows.Remove(determinized.StartStates.First());
            orderedRows.Insert(0, determinized.StartStates.First());

            List<Nfa<TAlphabet, StateSet>.State> orderedColumns = determinizedDual.States.ToList();
            orderedColumns.Remove(determinizedDual.StartStates.First());
            orderedColumns.Insert(0, determinizedDual.StartStates.First());

            var result = new StateMap(orderedRows.Count, orderedColumns.Count);
            for (int rowIndex = 0; rowIndex < orderedRows.Count; rowIndex++) {
                Nfa<TAlphabet, StateSet>.State rowState = orderedRows[rowIndex];
                var rowStateSet = new StateSet(rowState.Value);
                result.Rows.Left.Add(rowStateSet, rowIndex);
                for (int columnIndex = 0; columnIndex < orderedColumns.Count; columnIndex++) {
                    Nfa<TAlphabet, StateSet>.State columnState = orderedColumns[columnIndex];
                    var columnStateSet = new StateSet(columnState.Value);
                    result.Map[rowIndex, columnIndex] = new HashSet<State>(rowStateSet.Intersect(columnStateSet));
                }
            }
            for (int columnIndex = 0; columnIndex < orderedColumns.Count; columnIndex++) {
                Nfa<TAlphabet, StateSet>.State columnState = orderedColumns[columnIndex];
                var columnStateSet = new StateSet(columnState.Value);
                result.Columns.Left.Add(columnStateSet, columnIndex);
            }
            return result;
        }

        private static bool[,] MakeElementaryAutomatonMatrix(StateMap stateMap) {
            var result = new bool[stateMap.Rows.Count, stateMap.Columns.Count];
            for (int rowIndex = 0; rowIndex < stateMap.Rows.Count; rowIndex++) {
                for (int columnIndex = 0; columnIndex < stateMap.Columns.Count; columnIndex++) {
                    result[rowIndex, columnIndex] = stateMap.Map[rowIndex, columnIndex].Count > 0;
                }
            }
            return result;
        }

        private static Nfa<TAlphabet, int> GenerateEquivalenceClassReducedDfa(Nfa<TAlphabet, StateSet> subsetConstructionDfa, Dictionary<StateSet, int> equivalenceClassLookup) {
            var result = new Nfa<TAlphabet, int>();
            var intToResultState = new AutoDictionary<int, Nfa<TAlphabet, int>.State>(i => new Nfa<TAlphabet, int>.State(i));
            result.StartStates.Add(intToResultState[equivalenceClassLookup[subsetConstructionDfa.StartStates.First().Value]]);
            foreach (Nfa<TAlphabet, StateSet>.State acceptState in subsetConstructionDfa.AcceptStates) {
                result.AcceptStates.Add(intToResultState[equivalenceClassLookup[acceptState.Value]]);
            }
            foreach (var keyValuePair in subsetConstructionDfa.TransitionFunction) {
                Nfa<TAlphabet, int>.State fromState = intToResultState[equivalenceClassLookup[keyValuePair.Key.Value]];
                foreach (var valuePair in keyValuePair.Value) {
                    TAlphabet inputSymbol = valuePair.Key;
                    foreach (Nfa<TAlphabet, StateSet>.State state in valuePair.Value) {
                        Nfa<TAlphabet, int>.State toState = intToResultState[equivalenceClassLookup[state.Value]];
                        result.TransitionFunction[fromState][inputSymbol].Add(toState);
                    }
                }
            }
            result.States.UnionWith(intToResultState.Values);
            return result;
        }

        private static ReducedStateMap ReduceStateMap(StateMap stateMap, Nfa<TAlphabet, StateSet> subsetConstructionDfa, out Nfa<TAlphabet, int> minimizedSubsetConstructionDfa) {
            //construct an elementary automata matrix (EAM) [1]
            bool[,] elementaryAutomataMatrix = MakeElementaryAutomatonMatrix(stateMap);

            //determine which rows can be merged
            var rowsToMerge = new List<HashSet<int>>();
            {
                List<int> unmergedRows = Enumerable.Range(0, stateMap.Rows.Count).ToList();
                while (unmergedRows.Count > 0) {
                    rowsToMerge.Add(new HashSet<int> {unmergedRows[0]});
                    for (int rowIndex = 1; rowIndex < unmergedRows.Count; rowIndex++) {
                        int columnIndex;
                        for (columnIndex = 0; columnIndex < stateMap.Columns.Count; columnIndex++) {
                            if (elementaryAutomataMatrix[unmergedRows[0], columnIndex] != elementaryAutomataMatrix[unmergedRows[rowIndex], columnIndex]) {
                                break;
                            }
                        }
                        if (columnIndex != stateMap.Columns.Count) {
                            continue;
                        }
                        rowsToMerge[rowsToMerge.Count - 1].Add(unmergedRows[rowIndex]);
                        unmergedRows.RemoveAt(rowIndex);
                        rowIndex--;
                    }
                    unmergedRows.RemoveAt(0);
                }
            }

            //determine which columns can be merged
            var columnsToMerge = new List<HashSet<int>>();
            {
                List<int> unmergedColumns = Enumerable.Range(0, stateMap.Columns.Count).ToList();
                while (unmergedColumns.Count > 0) {
                    columnsToMerge.Add(new HashSet<int> {unmergedColumns[0]});
                    for (int columnIndex = 1; columnIndex < unmergedColumns.Count; columnIndex++) {
                        int rowIndex;
                        for (rowIndex = 0; rowIndex < stateMap.Rows.Count; rowIndex++) {
                            if (elementaryAutomataMatrix[rowIndex, unmergedColumns[0]] != elementaryAutomataMatrix[rowIndex, unmergedColumns[columnIndex]]) {
                                break;
                            }
                        }
                        if (rowIndex != stateMap.Rows.Count) {
                            continue;
                        }
                        columnsToMerge[columnsToMerge.Count - 1].Add(unmergedColumns[columnIndex]);
                        unmergedColumns.RemoveAt(columnIndex);
                        columnIndex--;
                    }
                    unmergedColumns.RemoveAt(0);
                }
            }

            var result = new ReducedStateMap(rowsToMerge.Count, columnsToMerge.Count);
            var stateSetToEquivalenceClassRowIndex = new Dictionary<StateSet, int>();
            for (int equivalenceClassRowIndex = 0; equivalenceClassRowIndex < rowsToMerge.Count; equivalenceClassRowIndex++) {
                foreach (int row in rowsToMerge[equivalenceClassRowIndex]) {
                    stateSetToEquivalenceClassRowIndex[stateMap.Rows.Right[row]] = equivalenceClassRowIndex;
                }
                var rowName = new ReadOnlyHashSet<int>(rowsToMerge[equivalenceClassRowIndex]);
                result.Rows.Left.Add(rowName, equivalenceClassRowIndex);
            }
            minimizedSubsetConstructionDfa = GenerateEquivalenceClassReducedDfa(subsetConstructionDfa, stateSetToEquivalenceClassRowIndex);

            for (int equivalenceClassColumnIndex = 0; equivalenceClassColumnIndex < columnsToMerge.Count; equivalenceClassColumnIndex++) {
                var columnName = new ReadOnlyHashSet<int>(columnsToMerge[equivalenceClassColumnIndex]);
                result.Columns.Left.Add(columnName, equivalenceClassColumnIndex);
            }

            for (int equivalenceClassRowIndex = 0; equivalenceClassRowIndex < rowsToMerge.Count; equivalenceClassRowIndex++) {
                for (int equivalenceClassColumnIndex = 0; equivalenceClassColumnIndex < columnsToMerge.Count; equivalenceClassColumnIndex++) {
                    HashSet<State> statesUnion = result.Map[equivalenceClassRowIndex, equivalenceClassColumnIndex] = new HashSet<State>();
                    foreach (int rowIndex in rowsToMerge[equivalenceClassRowIndex]) {
                        foreach (int columnIndex in columnsToMerge[equivalenceClassColumnIndex]) {
                            statesUnion.UnionWith(stateMap.Map[rowIndex, columnIndex]);
                        }
                    }
                }
            }

            return result;
        }

        private static bool[,] MakeReducedAutomataMatrix(ReducedStateMap reducedStateMap) {
            var result = new bool[reducedStateMap.Rows.Count, reducedStateMap.Columns.Count];
            for (int rowIndex = 0; rowIndex < reducedStateMap.Rows.Count; rowIndex++) {
                for (int columnIndex = 0; columnIndex < reducedStateMap.Columns.Count; columnIndex++) {
                    result[rowIndex, columnIndex] = reducedStateMap.Map[rowIndex, columnIndex].Count > 0;
                }
            }
            return result;
        }

        private static Grid[] ComputePrimeGrids(bool[,] reducedAutomataMatrix) {
            var gridsToProcess = new DistinctRecursiveAlgorithmProcessor<Grid>();
            int rowCount = reducedAutomataMatrix.GetUpperBound(0) + 1;
            int columnCount = reducedAutomataMatrix.GetUpperBound(1) + 1;

            //make initial grids which contain only one element
            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++) {
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++) {
                    if (reducedAutomataMatrix[rowIndex, columnIndex]) {
                        var grid = new Grid(new[] {rowIndex}, new[] {columnIndex});
                        gridsToProcess.Add(grid);
                    }
                }
            }

            //then, grow them incrementally, adding them back into the queue
            //or saving them if they cannot be grown
            var results = new ConcurrentSet<Grid>();
            gridsToProcess.Run(grid => {
                bool isPrime = true;
                //try expanding to other rows
                {
                    int comparisonRow = grid.Rows.First();
                    foreach (int testRow in Enumerable.Range(0, rowCount).Except(grid.Rows)) {
                        bool canExpand = grid.Columns.All(columnIndex => reducedAutomataMatrix[testRow, columnIndex] == reducedAutomataMatrix[comparisonRow, columnIndex]);
                        if (!canExpand) {
                            continue;
                        }
                        var newGrid = new Grid(grid.Rows.Concat(new[] {testRow}), grid.Columns);
                        gridsToProcess.Add(newGrid);
                        isPrime = false;
                    }
                }
                //try expanding to other columns
                {
                    int comparisonColumn = grid.Columns.First();
                    foreach (int testColumn in Enumerable.Range(0, columnCount).Except(grid.Columns)) {
                        bool canExpand = grid.Rows.All(rowIndex => reducedAutomataMatrix[rowIndex, testColumn] == reducedAutomataMatrix[rowIndex, comparisonColumn]);
                        if (!canExpand) {
                            continue;
                        }
                        var newGrid = new Grid(grid.Rows, grid.Columns.Concat(new[] {testColumn}));
                        gridsToProcess.Add(newGrid);
                        isPrime = false;
                    }
                }
                //if it's prime, then save it to the results
                if (isPrime) {
                    results.TryAdd(grid);
                }
            });
            return results.ToArray();
        }

        private static IEnumerable<Cover> EnumerateCovers(Grid[] primeGrids, int firstGridIndex, Dictionary<Grid, HashSet<int>> gridToFlattenedIndicesSet, HashSet<int> flattenedIndicesWithTrue, int gridCount) {
            if (gridCount > primeGrids.Length - firstGridIndex) { //can't reach gridCount == 0 before the recursion runs out of grids
                yield break;
            }
            for (int gridIndex = firstGridIndex; gridIndex < primeGrids.Length; gridIndex++) {
                Grid primeGrid = primeGrids[gridIndex];
                var primeGridAsEnumerable = new[] {primeGrid};
                var remainingFlattenedIndicesWithTrueToSatisfy = new HashSet<int>(flattenedIndicesWithTrue);
                remainingFlattenedIndicesWithTrueToSatisfy.ExceptWith(gridToFlattenedIndicesSet[primeGrid]);
                if (gridCount == 1) {
                    if (remainingFlattenedIndicesWithTrueToSatisfy.Count == 0) {
                        yield return new Cover(primeGridAsEnumerable);
                    }
                } else {
                    foreach (Cover enumerateCover in EnumerateCovers(primeGrids, gridIndex + 1, gridToFlattenedIndicesSet, remainingFlattenedIndicesWithTrueToSatisfy, gridCount - 1)) {
                        yield return new Cover(enumerateCover.Concat(primeGridAsEnumerable));
                    }
                }
            }
        }

        private static IEnumerable<Cover> EnumerateCovers(bool[,] reducedAutomataMatrix, Grid[] primeGrids) {
            int rowCount = reducedAutomataMatrix.GetUpperBound(0) + 1;
            int columnCount = reducedAutomataMatrix.GetUpperBound(1) + 1;

            var flattenedIndicesWithTrue = new HashSet<int>();
            for (int row = 0; row < rowCount; row++) {
                for (int column = 0; column < columnCount; column++) {
                    if (reducedAutomataMatrix[row, column]) {
                        flattenedIndicesWithTrue.Add(column*rowCount + row);
                    }
                }
            }

            if (flattenedIndicesWithTrue.Count == 0) {
                yield break;
            }

            Dictionary<Grid, HashSet<int>> gridToFlattenedIndicesSet = primeGrids.ToDictionary(grid => grid, grid => {
                var flattenedIndices = new HashSet<int>();
                foreach (int row in grid.Rows) {
                    foreach (int column in grid.Columns) {
                        flattenedIndices.Add(column*rowCount + row);
                    }
                }
                return flattenedIndices;
            });

            for (int gridCount = 1; gridCount <= primeGrids.Length; gridCount++) {
                foreach (Cover enumerateCover in EnumerateCovers(primeGrids, 0, gridToFlattenedIndicesSet, flattenedIndicesWithTrue, gridCount)) {
                    yield return enumerateCover;
                }
            }
        }

        private static AutoDictionary<int, HashSet<Grid>> MakeSubsetAssignmentFunction(Cover cover) {
            var result = new AutoDictionary<int, HashSet<Grid>>(dontCare0 => new HashSet<Grid>());
            foreach (Grid grid in cover) {
                foreach (int row in grid.Rows) {
                    result[row].Add(grid);
                }
            }
            return result;
        }

        private static Nfa<TAlphabet, int> FromIntersectionRule(Nfa<TAlphabet, int> reducedDfa, Cover cover, out Bimap<int, Grid> orderedGrids) {
            List<Nfa<TAlphabet, int>.State> orderedReducedDfaStates = reducedDfa.States.OrderBy(x => x.Value).ToList();
            AutoDictionary<int, HashSet<Grid>> subsetAssignmentFunction = MakeSubsetAssignmentFunction(cover);
            int counter = 0;
            Bimap<int, Grid> orderedGridsTemp = cover.ToBimap(x => counter++, x => x);
            var result = new Nfa<TAlphabet, int>();
            var intToResultState = new AutoDictionary<int, Nfa<TAlphabet, int>.State>(i => new Nfa<TAlphabet, int>.State(i));
            for (int resultStateIndex = 0; resultStateIndex < orderedGridsTemp.Count; resultStateIndex++) {
                Grid grid = orderedGridsTemp.Left[resultStateIndex];
                Nfa<TAlphabet, int>.State resultState = intToResultState[resultStateIndex];
                JaggedAutoDictionary<TAlphabet, HashSet<Nfa<TAlphabet, int>.State>> resultTransitionPartialLambda = result.TransitionFunction[resultState];
                IEnumerable<Nfa<TAlphabet, int>.State> rows = grid.Rows.Select(rowIndex => orderedReducedDfaStates[rowIndex]);
                ReadOnlyHashSet<TAlphabet> symbols = ReadOnlyHashSet<TAlphabet>.IntersectMany(rows.Select(row => reducedDfa.TransitionFunction[row].Keys));
                foreach (TAlphabet symbol in symbols) {
                    TAlphabet symbol1 = symbol;
                    IEnumerable<HashSet<Grid>> gridSets = rows.Select(row => subsetAssignmentFunction[reducedDfa.TransitionFunction[row][symbol1].First().Value]);
                    ReadOnlyHashSet<Grid> nextGrids = ReadOnlyHashSet<Grid>.IntersectMany(gridSets);
                    IEnumerable<int> nextIndices = nextGrids.Select(nextGrid => orderedGridsTemp.Right[nextGrid]);
                    IEnumerable<Nfa<TAlphabet, int>.State> nextStates = nextIndices.Select(gridIndex => intToResultState[gridIndex]);
                    resultTransitionPartialLambda[symbol].UnionWith(nextStates);
                }
                if (grid.Columns.Contains(0)) {
                    result.AcceptStates.Add(resultState);
                }
                if (grid.Rows.Contains(0)) {
                    result.StartStates.Add(resultState);
                }
            }

            result.States.UnionWith(intToResultState.Values);
            orderedGrids = orderedGridsTemp;
            return result;
        }

        private static bool GridSetSpansRow(Bimap<int, Grid> orderedGrids, IEnumerable<int> gridIndices, bool[,] reducedAutomataMatrix, int rowIndex) {
            var neededColumns = new HashSet<int>(Enumerable.Range(0, reducedAutomataMatrix.GetUpperBound(1) + 1).Where(columnIndex => reducedAutomataMatrix[rowIndex, columnIndex]));
            foreach (int gridIndex in gridIndices) {
                Grid grid = orderedGrids.Left[gridIndex];
                if (grid.Rows.Contains(rowIndex)) {
                    neededColumns.ExceptWith(grid.Columns);
                    if (neededColumns.Count == 0) {
                        break;
                    }
                }
            }
            return neededColumns.Count == 0;
        }

        private static bool SubsetAssignmentIsLegitimate(Nfa<TAlphabet, int> intersectionRuleNFA, Nfa<TAlphabet, int> minimizedDfa, bool[,] reducedAutomataMatrix, Bimap<int, Grid> orderedGrids) {
            Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet> intersectionRuleDfa = intersectionRuleNFA.Determinize();
            List<Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet>.State> intersectionRuleDfaOrderedStates = intersectionRuleDfa.States.ToList();
            intersectionRuleDfaOrderedStates.Remove(intersectionRuleDfa.StartStates.First());
            intersectionRuleDfaOrderedStates.Insert(0, intersectionRuleDfa.StartStates.First());

            var processor = new DistinctRecursiveAlgorithmProcessor<KeyValuePair<Nfa<TAlphabet, int>.State /*minimized*/, Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet>.State /*intersection rule*/>>();
            processor.Add(new KeyValuePair<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet>.State>(minimizedDfa.StartStates.First(), intersectionRuleDfa.StartStates.First()));
            bool isLegitimate = true;
            processor.Run(pair => {
                if (isLegitimate) {
                    Nfa<TAlphabet, int>.State minimizedDfaState = pair.Key;
                    Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet>.State intersectionRuleDfaState = pair.Value;
                    IEnumerable<TAlphabet> inputSymbols = minimizedDfa.TransitionFunction[minimizedDfaState].Keys;
                    foreach (TAlphabet inputSymbol in inputSymbols) {
                        Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet>.State nextIntersectionRuleDfaState = intersectionRuleDfa.TransitionFunction[intersectionRuleDfaState][inputSymbol].First();
                        Nfa<TAlphabet, int>.State nextMinimizedDfaState = minimizedDfa.TransitionFunction[minimizedDfaState][inputSymbol].First();
                        if (!intersectionRuleDfa.AcceptStates.Contains(nextIntersectionRuleDfaState) && minimizedDfa.AcceptStates.Contains(nextMinimizedDfaState)) {
                            isLegitimate = false;
                        } else if (!GridSetSpansRow(orderedGrids, nextIntersectionRuleDfaState.Value.Select(s => s.Value), reducedAutomataMatrix, nextMinimizedDfaState.Value)) {
                            isLegitimate = false;
                        } else {
                            processor.Add(new KeyValuePair<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet>.State>(nextMinimizedDfaState, nextIntersectionRuleDfaState));
                        }
                    }
                }
            });
            return isLegitimate;
        }

        public Nfa<TAlphabet, int> Reassign() {
            int counter = 0;
            return Reassign(_ => Interlocked.Increment(ref counter));
        }

        public Nfa<TAlphabet, TAssignment2> Reassign<TAssignment2>(Func<State, TAssignment2> func) {
            var result = new Nfa<TAlphabet, TAssignment2>();
            var stateMapper = new AutoDictionary<State, Nfa<TAlphabet, TAssignment2>.State>(state => new Nfa<TAlphabet, TAssignment2>.State(func(state)));
            foreach (State state in States) {
                stateMapper.EnsureCreated(state);
            }
            foreach (State state in TransitionFunction.Keys) {
                JaggedAutoDictionary<TAlphabet, HashSet<State>> sourcePartialEvaluation0 = TransitionFunction[state];
                JaggedAutoDictionary<TAlphabet, HashSet<Nfa<TAlphabet, TAssignment2>.State>> targetPartialEvaluation0 = result.TransitionFunction[stateMapper[state]];
                foreach (TAlphabet inputSymbol in TransitionFunction[state].Keys) {
                    HashSet<State> sourcePartialEvaluation1 = sourcePartialEvaluation0[inputSymbol];
                    HashSet<Nfa<TAlphabet, TAssignment2>.State> targetPartialEvaluation1 = targetPartialEvaluation0[inputSymbol];
                    foreach (State state1 in sourcePartialEvaluation1) {
                        targetPartialEvaluation1.Add(stateMapper[state1]);
                    }
                }
            }
            result.StartStates.UnionWith(StartStates.Select(state => stateMapper[state]));
            result.AcceptStates.UnionWith(AcceptStates.Select(state => stateMapper[state]));
            result.States.UnionWith(stateMapper.Values);
            return result;
        }

        public Nfa<TAlphabet> Clone() {
            var result = new Nfa<TAlphabet>();
            var stateMapper = new AutoDictionary<State, State>(state => new State());
            foreach (State state in States) {
                stateMapper.EnsureCreated(state);
            }
            foreach (State state in TransitionFunction.Keys) {
                JaggedAutoDictionary<TAlphabet, HashSet<State>> sourcePartialEvaluation0 = TransitionFunction[state];
                JaggedAutoDictionary<TAlphabet, HashSet<State>> targetPartialEvaluation0 = result.TransitionFunction[stateMapper[state]];
                foreach (TAlphabet inputSymbol in TransitionFunction[state].Keys) {
                    HashSet<State> sourcePartialEvaluation1 = sourcePartialEvaluation0[inputSymbol];
                    HashSet<State> targetPartialEvaluation1 = targetPartialEvaluation0[inputSymbol];
                    foreach (State state1 in sourcePartialEvaluation1) {
                        targetPartialEvaluation1.Add(stateMapper[state1]);
                    }
                }
            }
            result.StartStates.UnionWith(StartStates.Select(state => stateMapper[state]));
            result.AcceptStates.UnionWith(AcceptStates.Select(state => stateMapper[state]));
            result.States.UnionWith(stateMapper.Values);
            return result;
        }

        /// <summary>
        ///     Minimize this Nfa using the Kameda-Weiner algorithm [1]
        /// </summary>
        /// <returns>A minimal-state Nfa accepting the same language</returns>
        public Nfa<TAlphabet> Minimized() {
            return Reassign().Minimized().Reassign();
        }

        public static Nfa<TAlphabet> Union(IEnumerable<Nfa<TAlphabet>> nfas) {
            var result = new Nfa<TAlphabet>();
            foreach (var nfa in nfas) {
                //don't need to clone the states because they are immutable
                result.StartStates.UnionWith(nfa.StartStates);
                result.AcceptStates.UnionWith(nfa.AcceptStates);
                foreach (State fromState in nfa.TransitionFunction.Keys) {
                    foreach (TAlphabet inputSymbol in nfa.TransitionFunction[fromState].Keys) {
                        result.TransitionFunction[fromState][inputSymbol].UnionWith(nfa.TransitionFunction[fromState][inputSymbol]);
                    }
                }
                result.States.UnionWith(nfa.States);
            }
            return result;
        }

        public Nfa<TAlphabet, int> MinimizedDfa() {
            Nfa<TAlphabet, StateSet> determinized;
            StateMap sm = MakeStateMap(out determinized);
            Nfa<TAlphabet, int> minimizedSubsetConstructionDfa;
            ReduceStateMap(sm, determinized, out minimizedSubsetConstructionDfa);
            return minimizedSubsetConstructionDfa;
        }

        public bool IsEquivalent<TAssignment2>(Nfa<TAlphabet, TAssignment2> that) {
            Nfa<TAlphabet, int> thisMinDfa = MinimizedDfa();
            Nfa<TAlphabet, int> thatMinDfa = that.MinimizedDfa();
            bool equivalent = true;
            var stateMap = new ConcurrentDictionary<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, int>.State>();
            var processor = new DistinctRecursiveAlgorithmProcessor<KeyValuePair<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, int>.State>>();
            processor.Add(new KeyValuePair<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, int>.State>(thisMinDfa.StartStates.First(), thatMinDfa.StartStates.First())); //only one start state since it's a min dfa
            processor.Run(pair => {
                if (!equivalent) {
                    return;
                }
                foreach (var inputSymbolAndStates in thisMinDfa.TransitionFunction[pair.Key]) {
                    TAlphabet thisMinDfaInputSymbol = inputSymbolAndStates.Key;
                    Nfa<TAlphabet, int>.State thisMinDfaNextState = inputSymbolAndStates.Value.First(); //deterministic, so only one state
                    HashSet<Nfa<TAlphabet, int>.State> thatMinDfaNextStates = thatMinDfa.TransitionFunction[pair.Value][thisMinDfaInputSymbol];
                    if (thatMinDfaNextStates.Count != 1) { //it will always be either 0 or 1
                        equivalent = false;
                    } else {
                        Nfa<TAlphabet, int>.State thatMinDfaNextState = thatMinDfaNextStates.First();
                        Nfa<TAlphabet, int>.State mappedThisMinDfaNextState = stateMap.GetOrAdd(thisMinDfaNextState, thisMinDfaNextStateProxy => {
                            processor.Add(new KeyValuePair<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, int>.State>(thisMinDfaNextState, thatMinDfaNextState));
                            return thatMinDfaNextState;
                        });
                        if (thatMinDfaNextState != mappedThisMinDfaNextState) {
                            equivalent = false;
                        }
                    }
                }
            });
            return equivalent;
        }

        public static Nfa<TAlphabet, int> Intersect(IEnumerable<Nfa<TAlphabet>> nfas) {
            IEnumerable<Nfa<TAlphabet, int>> minDets = nfas.Select(nfa => nfa.MinimizedDfa());
            Nfa<TAlphabet, int> singleTransitionNFA = Nfa<TAlphabet, int>.Union(minDets);
            JaggedAutoDictionary<Nfa<TAlphabet, int>.State, TAlphabet, HashSet<Nfa<TAlphabet, int>.State>> singleTransitionFunction = singleTransitionNFA.TransitionFunction;
            HashSet<Nfa<TAlphabet, int>.State> singleAcceptStates = singleTransitionNFA.AcceptStates;
            int stateCount = 0;
            var resultStates = new AutoDictionary<ReadOnlyHashSet<Nfa<TAlphabet, int>.State>, Nfa<TAlphabet, int>.State>(x => new Nfa<TAlphabet, int>.State(Interlocked.Increment(ref stateCount)));
            var processor = new DistinctRecursiveAlgorithmProcessor<ReadOnlyHashSet<Nfa<TAlphabet, int>.State>>();
            var startStateSet = new ReadOnlyHashSet<Nfa<TAlphabet, int>.State>(minDets.Select(x => x.StartStates.First()));
            var result = new Nfa<TAlphabet, int>();
            var acceptStates = new ConcurrentSet<Nfa<TAlphabet, int>.State>();
            processor.Add(startStateSet);
            processor.Run(stateSet => {
                Nfa<TAlphabet, int>.State fromState = resultStates[stateSet];
                if (singleAcceptStates.IsSupersetOf(stateSet)) {
                    acceptStates.TryAdd(fromState);
                }
                ReadOnlyHashSet<TAlphabet> fromSymbols = ReadOnlyHashSet<TAlphabet>.IntersectMany(stateSet.Select(state => singleTransitionFunction[state].Keys));
                foreach (TAlphabet fromSymbol in fromSymbols) {
                    TAlphabet symbol = fromSymbol;
                    var nextStateSet = new ReadOnlyHashSet<Nfa<TAlphabet, int>.State>(stateSet.Select(state => singleTransitionFunction[state][symbol].First()));
                    Nfa<TAlphabet, int>.State toState = resultStates[nextStateSet];
                    processor.Add(nextStateSet);
                    result.TransitionFunction[fromState][fromSymbol].Add(toState);
                }
            });
            result.States.UnionWith(resultStates.Values);
            result.StartStates.Add(resultStates[startStateSet]);
            result.AcceptStates.UnionWith(acceptStates);
            return result;
        }

        //public bool Contains(Nfa<TAlphabet> that)
        //{
        //    return Intersect(new[] { this, that }).IsEquivalent(that);
        //}

        public IEnumerable<State> GetReachables(State fromState, HashSet<State> ignoredStates = null) {
            if (ignoredStates == null) {
                ignoredStates = new HashSet<State>();
            }
            var processor = new DistinctRecursiveAlgorithmProcessor<State>();
            processor.Add(fromState);
            var results = new List<State>();
            processor.Run(state => {
                foreach (State reachable in TransitionFunction[state].SelectMany(kvp => kvp.Value)) {
                    if (ignoredStates.Contains(reachable)) {
                        continue;
                    }
                    processor.Add(reachable);
                    results.Add(reachable);
                }
            });
            return results;
        }

        public IEnumerable<IEnumerable<State>> GetRoutes(State fromState, State toState, HashSet<State> ignoredStates = null) {
            if (ignoredStates == null) {
                ignoredStates = new HashSet<State>();
            }
            List<State> subsequentStates = TransitionFunction[fromState].SelectMany(inputSymbolAndToStates => inputSymbolAndToStates.Value).Distinct().Where(s => !ignoredStates.Contains(s)).ToList();
            foreach (State subsequentState in subsequentStates) {
                if (subsequentState == toState) {
                    yield return new[] {toState};
                }
                ignoredStates.Add(subsequentState);
                foreach (var route in GetRoutes(subsequentState, toState, ignoredStates)) {
                    yield return new[] {subsequentState}.Concat(route);
                }
                ignoredStates.Remove(subsequentState);
            }
        }

        public IEnumerable<IEnumerable<State>> GetCycles() {
            return States.SelectMany(state => GetRoutes(state, state, new HashSet<State>()));
        }

        /// <summary>
        ///     Inserts an Nfa 'require' at the 'at' state
        ///     Any transitions leaving 'at' are removed and stored in outgoingTransitions
        ///     Any start states of 'require' become synonymous with at
        ///     Any accept states of 'require' have outgoingTransitions added to them
        /// </summary>
        /// <param name="at">The state to insert the Nfa at</param>
        /// <param name="require">The Nfa to insert</param>
        public void Insert(State at, Nfa<TAlphabet> require) {
            bool atIsAcceptState = AcceptStates.Contains(at);
            //Store copies of all the transitions leaving 'at'
            var outgoingTransitions = new JaggedAutoDictionary<TAlphabet, List<State>>(_ => new List<State>());
            foreach (TAlphabet symbol in TransitionFunction[at].Keys) {
                foreach (State toState in TransitionFunction[at][symbol]) {
                    if (!ReferenceEquals(at, toState)) {
                        outgoingTransitions[symbol].Add(toState);
                    }
                }
            }

            foreach (TAlphabet outGoingSymbol in outgoingTransitions.Keys) {
                foreach (State toState in outgoingTransitions[outGoingSymbol]) {
                    TransitionFunction[at][outGoingSymbol].Remove(toState);
                }
                if (TransitionFunction[at][outGoingSymbol].Count == 0) {
                    TransitionFunction[at].TryRemove(outGoingSymbol);
                }
            }

            if (!TransitionFunction[at].Any()) {
                TransitionFunction.TryRemove(at);
            }

            //Add all of 'require's states to storage, except start and accept states
            //Simultaneously, create a map from 'require's states to storage's states
            //which for the most part is identity, but start states map to 'at'
            //Also, make a list of the mapped accept states, which we'll use later
            var stateMap = new Dictionary<State, State>();
            var mappedAcceptStates = new List<State>();
            foreach (State state in require.States) {
                if (!require.StartStates.Contains(state)) {
                    States.Add(state);
                    stateMap[state] = state;
                } else {
                    stateMap[state] = at;
                }
                if (require.AcceptStates.Contains(state)) {
                    mappedAcceptStates.Add(stateMap[state]);
                    if (atIsAcceptState) {
                        AcceptStates.Add(stateMap[state]);
                    }
                }
            }
            mappedAcceptStates = mappedAcceptStates.Distinct().ToList();

            //now that the map is complete, copy the transitions from 'require' to storage
            //using the stateMap to make necessary alterations
            foreach (State state in require.States) {
                foreach (TAlphabet symbol in require.TransitionFunction[state].Keys) {
                    foreach (State toState in require.TransitionFunction[state][symbol]) {
                        TransitionFunction[stateMap[state]][symbol].Add(stateMap[toState]);
                    }
                }
            }

            //lastly, hook up the mappedAcceptStates using the saved outgoingTransitions
            foreach (State mappedAcceptState in mappedAcceptStates) {
                foreach (TAlphabet symbol in outgoingTransitions.Keys) {
                    foreach (State toState in outgoingTransitions[symbol]) {
                        TransitionFunction[mappedAcceptState][symbol].Add(toState);
                    }
                }
            }

            //one more thing, 'at' can't be an accept state anymore
            if (atIsAcceptState) {
                //unless one or more of 'require's start states is an accept state
                if (require.AcceptStates.Intersect(require.StartStates).Count() == 0) {
                    AcceptStates.Remove(at);
                }
            }
        }

        public void Append(Nfa<TAlphabet> require) {
            foreach (var state in require.States) {
                States.Add(state);
            }
            var beforeAcceptTransitions = GetTransitions().Where(t => AcceptStates.Contains(t.ToState)).ToArray();
            bool anyAcceptIsStart = AcceptStates.Any(state => StartStates.Contains(state));
            AcceptStates.Clear();
            foreach (var acceptState in require.AcceptStates) {
                AcceptStates.Add(acceptState);
            }
            foreach (var beforeAcceptTransition in beforeAcceptTransitions) {
                foreach (var startState in require.StartStates) {
                    TransitionFunction[beforeAcceptTransition.FromState][beforeAcceptTransition.Symbol].Add(startState);
                }
            }
            foreach (var transition in require.GetTransitions()) {
                TransitionFunction[transition.FromState][transition.Symbol].Add(transition.ToState);
            }
            if (anyAcceptIsStart) {
                foreach (var startState in require.StartStates) {
                    StartStates.Add(startState);
                }
            }
        }

        public string ToString(Func<TAlphabet, String> transitionStringifier) {
            int counter = 0;
            return Reassign(_ => Interlocked.Increment(ref counter)).ToString(transitionStringifier);
        }

        public override string ToString() {
            return ToString(x => x.ToString());
        }

        private class Cover : ReadOnlyHashSet<Grid> {
            public Cover(IEnumerable<Grid> items)
                : base(items) {}
        }

        private class Grid {
            public readonly ReadOnlyHashSet<int> Columns;
            public readonly ReadOnlyHashSet<int> Rows;

            public Grid(IEnumerable<int> rows, IEnumerable<int> columns) {
                Rows = new ReadOnlyHashSet<int>(rows);
                Columns = new ReadOnlyHashSet<int>(columns);
            }

            protected bool Equals(Grid other) {
                return Columns.Equals(other.Columns) && Rows.Equals(other.Rows);
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) {
                    return false;
                }
                if (ReferenceEquals(this, obj)) {
                    return true;
                }
                if (obj.GetType() != GetType()) {
                    return false;
                }
                return Equals((Grid)obj);
            }

            public override int GetHashCode() {
                unchecked {
                    return (Columns.GetHashCode()*397) ^ Rows.GetHashCode();
                }
            }

            public static bool operator ==(Grid left, Grid right) {
                return Equals(left, right);
            }

            public static bool operator !=(Grid left, Grid right) {
                return !Equals(left, right);
            }
        }

        private class ReducedStateMap {
            public readonly Bimap<ReadOnlyHashSet<int>, int> Columns = new Bimap<ReadOnlyHashSet<int>, int>();
            public readonly HashSet<State>[,] Map;
            public readonly Bimap<ReadOnlyHashSet<int>, int> Rows = new Bimap<ReadOnlyHashSet<int>, int>();

            public ReducedStateMap(int rowCount, int columnCount) {
                Map = new HashSet<State>[rowCount, columnCount];
            }
        }

        /// <summary>
        ///     A State of an Nfa
        /// </summary>
        public class State {
            public override string ToString() {
                return "State";
            }
        }

        private class StateMap {
            public readonly Bimap<StateSet, int> Columns = new Bimap<StateSet, int>();
            public readonly HashSet<State>[,] Map;
            public readonly Bimap<StateSet, int> Rows = new Bimap<StateSet, int>();

            public StateMap(int rowCount, int columnCount) {
                Map = new HashSet<State>[rowCount, columnCount];
            }
        }

        /// <summary>
        ///     An immutable set of States that can be quickly tested for inequality
        /// </summary>
        public class StateSet : ReadOnlyHashSet<State> {
            public StateSet(IEnumerable<State> items)
                : base(items) {}
        }

        public class Transition {
            public State FromState;
            public TAlphabet Symbol;
            public State ToState;

            public Transition(State fromState, TAlphabet symbol, State toState) {
                FromState = fromState;
                Symbol = symbol;
                ToState = toState;
            }

            public Transition() {}
        }
    }
}

/*
 * References:
 * [1] Kameda, T. ; IEEE ; Weiner, Peter
 *      "On the State Minimization of Nondeterministic Finite Automata"
 *      Computers, IEEE Transactions on  (Volume:C-19 ,  Issue: 7 )
 */