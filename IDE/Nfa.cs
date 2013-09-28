using System.Collections.Concurrent.More;
using Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Generic.More;
using System.Linq;
using System.Linq.More;
using System.Threading;
using System.Threading.Tasks;

namespace IDE {
    /// <summary>
    /// A Nondeterministic Finite Automaton (∪-NFA)
    /// </summary>
    /// <typeparam name="TAlphabet">The domain of the transition function is S x TAlphabet, where S is the set of states.</typeparam>
    /// <typeparam name="TAssignment">The type of the value associated with a state.</typeparam>
    public class Nfa<TAlphabet, TAssignment> {

        /// <summary>
        /// A State of an NFA
        /// </summary>
        public class State {
            public readonly TAssignment Value;

            public State(TAssignment value) {
                Value = value;
            }
        }

        public readonly HashSet<State> States = new HashSet<State>();
        public readonly JaggedAutoDictionary<State, TAlphabet, HashSet<State>> TransitionFunction = new JaggedAutoDictionary<State, TAlphabet, HashSet<State>>((dontCare0, dontCare1) => new HashSet<State>());
        public readonly HashSet<State> StartStates = new HashSet<State>();
        public HashSet<State> AcceptStates = new HashSet<State>();

        public HashSet<State> TransitionFunctionEx(IEnumerable<State> states, TAlphabet input) {
            var result = new HashSet<State>();
            foreach (var state in states) {
                result.UnionWith(TransitionFunction[state][input]);
            }
            return result;
        }

        public HashSet<State> TransitionFunctionEx(State state, IEnumerable<TAlphabet> inputString) {
            var currentConfiguration = new HashSet<State> {state};
            return inputString.Aggregate(currentConfiguration, TransitionFunctionEx);
        }

        public HashSet<State> TransitionFunctionEx(IEnumerable<State> states, IEnumerable<TAlphabet> inputString) {
            var currentConfiguration = new HashSet<State>(states);
            return inputString.Aggregate(currentConfiguration, TransitionFunctionEx);
        }

        /// <summary>
        /// An immutable set of States that can be quickly tested for inequality
        /// </summary>
        public class Configuration : ReadOnlyHashSet<State> {
            public Configuration(IEnumerable<State> items) : base(items) {
            }
        }

        /// <summary>
        /// Creates a new Nfa that has only one transition for each input symbol for each state - i.e. it is deterministic
        /// </summary>
        /// <returns>The new DFA</returns>
        public Nfa<TAlphabet, Configuration> Determinize() {
            var configurationToDState = new ConcurrentDictionary<Configuration, Nfa<TAlphabet, Configuration>.State>();
            var result = new Nfa<TAlphabet, Configuration>();
            var resultAcceptStates = new ConcurrentBag<Nfa<TAlphabet, Configuration>.State>();

            Func<Configuration, Nfa<TAlphabet, Configuration>.State> adder = null;
            adder = configuration => configurationToDState.GetOrAdd(configuration, configurationProxy => {
                var newState = new Nfa<TAlphabet, Configuration>.State(configurationProxy);
                Task.Factory.StartNew(() => {
                    bool isAcceptState = configurationProxy.Any(x => AcceptStates.Contains(x));
                    if (isAcceptState) {
                        resultAcceptStates.Add(newState);
                    }
                    IEnumerable<TAlphabet> awayInputs = configurationProxy.Select(x => TransitionFunction[x]).SelectMany(y => y.Keys).Distinct();
                    Parallel.ForEach(awayInputs, input => {
                        HashSet<State> nextConfiguration = TransitionFunctionEx(configurationProxy, input);
                        if (nextConfiguration.Count > 0) {
                            Nfa<TAlphabet, Configuration>.State nextState = adder(new Configuration(nextConfiguration));
                            result.TransitionFunction[newState][input].Add(nextState);
                        }
                    });
                }, TaskCreationOptions.AttachedToParent);
                return newState;
            });

            var startConfiguration = new Configuration(StartStates);
            Task.Factory.StartNew(() => {
                result.StartStates.Add(adder(startConfiguration));
            }).Wait();
            result.States.UnionWith(configurationToDState.Values);
            result.AcceptStates = new HashSet<Nfa<TAlphabet, Configuration>.State>(resultAcceptStates);
            return result;
        }

        /// <summary>
        /// Creates a new Nfa that recognizes the reversed language
        /// </summary>
        /// <returns>The new Nfa</returns>
        public Nfa<TAlphabet, TAssignment> Dual() {
            var result = new Nfa<TAlphabet, TAssignment>();
            result.StartStates.UnionWith(AcceptStates);
            result.States.UnionWith(States);
            foreach (var keyValuePair in TransitionFunction) {
                foreach (var valuePair in keyValuePair.Value) {
                    foreach (var state in valuePair.Value) {
                        result.TransitionFunction[state][valuePair.Key].Add(keyValuePair.Key);
                    }
                }
            }
            result.AcceptStates.UnionWith(StartStates);
            return result;
        }

        public class StateMap {
            public readonly HashSet<State>[,] Map; 
            public readonly Bimap<Configuration, int> Rows = new Bimap<Configuration, int>();
            public readonly Bimap<Configuration, int> Columns = new Bimap<Configuration, int>();

            public StateMap(int rowCount, int columnCount) {
                Map = new HashSet<State>[rowCount, columnCount];
            }
        }

        public class ReducedStateMap {
            public readonly HashSet<State>[,] Map;
            public readonly Bimap<ReadOnlyHashSet<int>, int> Rows = new Bimap<ReadOnlyHashSet<int>, int>();
            public readonly Bimap<ReadOnlyHashSet<int>, int> Columns = new Bimap<ReadOnlyHashSet<int>, int>();

            public ReducedStateMap(int rowCount, int columnCount) {
                Map = new HashSet<State>[rowCount, columnCount];
            }
        }
        /// <summary>
        /// Creates a state map (SM) as described in [1]
        /// </summary>
        /// <returns></returns>
        public StateMap MakeStateMap(out Nfa<TAlphabet, Configuration> determinized) {
            determinized = Determinize();
            var determinizedDual = Dual().Determinize();

            var orderedRows = determinized.States.ToList();
            orderedRows.Remove(determinized.StartStates.First());
            orderedRows.Insert(0, determinized.StartStates.First());

            var orderedColumns = determinizedDual.States.ToList();
            orderedColumns.Remove(determinizedDual.StartStates.First());
            orderedColumns.Insert(0, determinizedDual.StartStates.First());

            var result = new StateMap(orderedRows.Count, orderedColumns.Count);
            for (int rowIndex = 0; rowIndex < orderedRows.Count; rowIndex++) {
                var rowState = orderedRows[rowIndex];
                var rowConfiguration = new Configuration(rowState.Value);
                result.Rows.Left.Add(rowConfiguration, rowIndex);
                for (int columnIndex = 0; columnIndex < orderedColumns.Count; columnIndex++) {
                    var columnState = orderedColumns[columnIndex];
                    var columnConfiguration = new Configuration(columnState.Value);
                    result.Map[rowIndex, columnIndex] = new HashSet<State>(rowConfiguration.Intersect(columnConfiguration));
                }
            }
            for (int columnIndex = 0; columnIndex < orderedColumns.Count; columnIndex++) {
                var columnState = orderedColumns[columnIndex];
                var columnConfiguration = new Configuration(columnState.Value);
                result.Columns.Left.Add(columnConfiguration, columnIndex);
            }
            return result;
        }

        public static bool[,] MakeElementaryAutomataMatrix(StateMap stateMap) {
            var result = new bool[stateMap.Rows.Count, stateMap.Columns.Count];
            for (var rowIndex = 0; rowIndex < stateMap.Rows.Count; rowIndex++) {
                for (var columnIndex = 0; columnIndex < stateMap.Columns.Count; columnIndex++) {
                    result[rowIndex, columnIndex] = stateMap.Map[rowIndex, columnIndex].Count > 0;
                }
            }
            return result;
        }

        public static Nfa<TAlphabet, int> GenerateEquivalenceClassReducedDfa(Nfa<TAlphabet, Configuration> subsetConstructionDfa, Dictionary<Configuration, int> equivalenceClassLookup) {
            var result = new Nfa<TAlphabet, int>();
            var intToResultState = new AutoDictionary<int, Nfa<TAlphabet, int>.State>(i => new Nfa<TAlphabet, int>.State(i));
            result.StartStates.Add(intToResultState[equivalenceClassLookup[subsetConstructionDfa.StartStates.First().Value]]);
            foreach (var acceptState in subsetConstructionDfa.AcceptStates) {
                result.AcceptStates.Add(intToResultState[equivalenceClassLookup[acceptState.Value]]);
            }
            foreach (var keyValuePair in subsetConstructionDfa.TransitionFunction) {
                Nfa<TAlphabet, int>.State fromState = intToResultState[equivalenceClassLookup[keyValuePair.Key.Value]];
                foreach (var valuePair in keyValuePair.Value) {
                    TAlphabet inputSymbol = valuePair.Key;
                    foreach (var state in valuePair.Value) {
                        Nfa<TAlphabet, int>.State toState = intToResultState[equivalenceClassLookup[state.Value]];
                        result.TransitionFunction[fromState][inputSymbol].Add(toState);
                    }
                }
            }
            result.States.UnionWith(intToResultState.Values);
            return result;
        }

        public static ReducedStateMap ReduceStateMap(StateMap stateMap, Nfa<TAlphabet, Configuration> subsetConstructionDfa, out Nfa<TAlphabet, int> minimizedSubsetConstructionDfa) {
            //construct an elementary automata matrix (EAM) [1]
            var elementaryAutomataMatrix = MakeElementaryAutomataMatrix(stateMap);

            //determine which rows can be merged
            var rowsToMerge = new List<HashSet<int>>();
            {
                var unmergedRows = Enumerable.Range(0, stateMap.Rows.Count).ToList();
                while(unmergedRows.Count > 0) {
                    rowsToMerge.Add(new HashSet<int> {unmergedRows[0]});
                    for (var rowIndex = 1; rowIndex < unmergedRows.Count; rowIndex++) {
                        int columnIndex;
                        for (columnIndex = 0; columnIndex < stateMap.Columns.Count; columnIndex++) {
                            if (elementaryAutomataMatrix[unmergedRows[0], columnIndex] != elementaryAutomataMatrix[unmergedRows[rowIndex], columnIndex]) break;
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
                var unmergedColumns = Enumerable.Range(0, stateMap.Columns.Count).ToList();
                while(unmergedColumns.Count > 0) {
                    columnsToMerge.Add(new HashSet<int> {unmergedColumns[0]});
                    for (var columnIndex = 1; columnIndex < unmergedColumns.Count; columnIndex++) {
                        int rowIndex;
                        for (rowIndex = 0; rowIndex < stateMap.Rows.Count; rowIndex++) {
                            if (elementaryAutomataMatrix[rowIndex, unmergedColumns[0]] != elementaryAutomataMatrix[rowIndex, unmergedColumns[columnIndex]]) break;
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
            var configurationToEquivalenceClassRowIndex = new Dictionary<Configuration, int>();
            for (var equivalenceClassRowIndex = 0; equivalenceClassRowIndex < rowsToMerge.Count; equivalenceClassRowIndex++) {
                foreach (var row in rowsToMerge[equivalenceClassRowIndex]) {
                    configurationToEquivalenceClassRowIndex[stateMap.Rows.Right[row]] = equivalenceClassRowIndex;
                }
                var rowName = new ReadOnlyHashSet<int>(rowsToMerge[equivalenceClassRowIndex]);
                result.Rows.Left.Add(rowName, equivalenceClassRowIndex);
            }
            minimizedSubsetConstructionDfa = GenerateEquivalenceClassReducedDfa(subsetConstructionDfa, configurationToEquivalenceClassRowIndex);

            for (var equivalenceClassColumnIndex = 0; equivalenceClassColumnIndex < columnsToMerge.Count; equivalenceClassColumnIndex++) {
                var columnName = new ReadOnlyHashSet<int>(columnsToMerge[equivalenceClassColumnIndex]);
                result.Columns.Left.Add(columnName, equivalenceClassColumnIndex);
            }

            for (var equivalenceClassRowIndex = 0; equivalenceClassRowIndex < rowsToMerge.Count; equivalenceClassRowIndex++) {
                for (var equivalenceClassColumnIndex = 0; equivalenceClassColumnIndex < columnsToMerge.Count; equivalenceClassColumnIndex++) {
                    var statesUnion = result.Map[equivalenceClassRowIndex, equivalenceClassColumnIndex] = new HashSet<State>();
                    foreach (var rowIndex in rowsToMerge[equivalenceClassRowIndex]) {
                        foreach (var columnIndex in columnsToMerge[equivalenceClassColumnIndex]) {
                            statesUnion.UnionWith(stateMap.Map[rowIndex, columnIndex]);
                        }
                    }
                }
            }

            return result;
        }

        public static bool[,] MakeReducedAutomataMatrix(ReducedStateMap reducedStateMap) {
            var result = new bool[reducedStateMap.Rows.Count, reducedStateMap.Columns.Count];
            for (var rowIndex = 0; rowIndex < reducedStateMap.Rows.Count; rowIndex++) {
                for (var columnIndex = 0; columnIndex < reducedStateMap.Columns.Count; columnIndex++) {
                    result[rowIndex, columnIndex] = reducedStateMap.Map[rowIndex, columnIndex].Count > 0;
                }
            }
            return result;            
        }

        public class Grid {
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
                return Equals((Grid) obj);
            }

            public override int GetHashCode() {
                unchecked {
                    return (Columns.GetHashCode() * 397) ^ Rows.GetHashCode();
                }
            }

            public static bool operator ==(Grid left, Grid right) {
                return Equals(left, right);
            }

            public static bool operator !=(Grid left, Grid right) {
                return !Equals(left, right);
            }

            public readonly ReadOnlyHashSet<int> Rows;
            public readonly ReadOnlyHashSet<int> Columns;

            public Grid(IEnumerable<int> rows, IEnumerable<int> columns) {
                Rows = new ReadOnlyHashSet<int>(rows);
                Columns = new ReadOnlyHashSet<int>(columns);
            }
        }

        public static Grid[] ComputePrimeGrids(bool[,] reducedAutomataMatrix) {
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
                var isPrime = true;
                //try expanding to other rows
                {
                    int comparisonRow = grid.Rows.First();
                    foreach (var testRow in Enumerable.Range(0, rowCount).Except(grid.Rows)) {
                        var canExpand = grid.Columns.All(columnIndex => reducedAutomataMatrix[testRow, columnIndex] == reducedAutomataMatrix[comparisonRow, columnIndex]);
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
                    foreach (var testColumn in Enumerable.Range(0, columnCount).Except(grid.Columns)) {
                        var canExpand = grid.Rows.All(rowIndex => reducedAutomataMatrix[rowIndex, testColumn] == reducedAutomataMatrix[rowIndex, comparisonColumn]);
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

        public class Cover : ReadOnlyHashSet<Grid> {
            public Cover(IEnumerable<Grid> items)
                : base(items) {
            }
        }

        static IEnumerable<Cover> EnumerateCovers(Grid[] primeGrids, int firstGridIndex, Dictionary<Grid, HashSet<int>> gridToFlattenedIndicesSet, HashSet<int> flattenedIndicesWithTrue, int gridCount) {
            if (gridCount > primeGrids.Length - firstGridIndex) { //can't reach gridCount == 0 before the recursion runs out of grids
                yield break;
            }
            for (int gridIndex = firstGridIndex; gridIndex < primeGrids.Length; gridIndex++) {
                var primeGrid = primeGrids[gridIndex];
                var primeGridAsEnumerable = new[] {primeGrid};
                var remainingFlattenedIndicesWithTrueToSatisfy = new HashSet<int>(flattenedIndicesWithTrue);
                remainingFlattenedIndicesWithTrueToSatisfy.ExceptWith(gridToFlattenedIndicesSet[primeGrid]);
                if (gridCount == 1) {
                    if (remainingFlattenedIndicesWithTrueToSatisfy.Count == 0) {
                        yield return new Cover(primeGridAsEnumerable);
                    }
                } else {
                    foreach (var enumerateCover in EnumerateCovers(primeGrids, gridIndex + 1, gridToFlattenedIndicesSet, remainingFlattenedIndicesWithTrueToSatisfy, gridCount - 1)) {
                        yield return new Cover(enumerateCover.Concat(primeGridAsEnumerable));
                    }
                }
            }
        }

        public static IEnumerable<Cover> EnumerateCovers(bool[,] reducedAutomataMatrix, Grid[] primeGrids) {
            var rowCount = reducedAutomataMatrix.GetUpperBound(0) + 1;
            var columnCount = reducedAutomataMatrix.GetUpperBound(1) + 1;

            var flattenedIndicesWithTrue = new HashSet<int>();
            for (var row = 0; row < rowCount; row++) {
                for (var column = 0; column < columnCount; column++) {
                    if (reducedAutomataMatrix[row, column]) {
                        flattenedIndicesWithTrue.Add(column * rowCount + row);
                    }
                }
            }

            if (flattenedIndicesWithTrue.Count == 0) {
                yield break;
            }

            var gridToFlattenedIndicesSet = primeGrids.ToDictionary(grid => grid, grid => {
                var flattenedIndices = new HashSet<int>();
                foreach (var row in grid.Rows) {
                    foreach (var column in grid.Columns) {
                        flattenedIndices.Add(column * rowCount + row);
                    }
                }
                return flattenedIndices;
            });

            for (var gridCount = 1; gridCount <= primeGrids.Length; gridCount++) {
                foreach (var enumerateCover in EnumerateCovers(primeGrids, 0, gridToFlattenedIndicesSet, flattenedIndicesWithTrue, gridCount)) {
                    yield return enumerateCover;
                }
            }
        }

        static AutoDictionary<int, HashSet<Grid>> MakeSubsetAssignmentFunction(Cover cover) {
            var result = new AutoDictionary<int, HashSet<Grid>>(dontCare0 => new HashSet<Grid>());
            foreach (var grid in cover) {
                foreach (var row in grid.Rows) {
                    result[row].Add(grid);
                }
            }
            return result;
        }

        public static Nfa<TAlphabet, int> FromIntersectionRule(Nfa<TAlphabet, int> reducedDfa, Cover cover, out Bimap<int, Grid> orderedGrids) {
            var orderedReducedDfaStates = reducedDfa.States.OrderBy(x => x.Value).ToList();
            var subsetAssignmentFunction = MakeSubsetAssignmentFunction(cover);
            var counter = 0;
            var orderedGridsTemp = cover.ToBimap(x => counter++, x => x);
            var result = new Nfa<TAlphabet, int>();
            var intToResultState = new AutoDictionary<int, Nfa<TAlphabet, int>.State>(i => new Nfa<TAlphabet, int>.State(i));
            for (var resultStateIndex = 0; resultStateIndex < orderedGridsTemp.Count; resultStateIndex++) {
                var grid = orderedGridsTemp.Left[resultStateIndex];
                var resultState = intToResultState[resultStateIndex];
                var resultTransitionPartialLambda = result.TransitionFunction[resultState];
                var rows = grid.Rows.Select(rowIndex => orderedReducedDfaStates[rowIndex]);
                var symbols = ReadOnlyHashSet<TAlphabet>.MultiIntersect(rows.Select(row => reducedDfa.TransitionFunction[row].Keys));
                foreach (var symbol in symbols) {
                    var symbol1 = symbol;
                    var gridSets = rows.Select(row => subsetAssignmentFunction[reducedDfa.TransitionFunction[row][symbol1].First().Value]);
                    var nextGrids = ReadOnlyHashSet<Grid>.MultiIntersect(gridSets);
                    var nextIndices = nextGrids.Select(nextGrid => orderedGridsTemp.Right[nextGrid]);
                    var nextStates = nextIndices.Select(gridIndex => intToResultState[gridIndex]);
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

        public static bool GridSetSpansRow(Bimap<int, Grid> orderedGrids, IEnumerable<int> gridIndices, bool[,] reducedAutomataMatrix, int rowIndex) {
            var neededColumns = new HashSet<int>(Enumerable.Range(0, reducedAutomataMatrix.GetUpperBound(1) + 1).Where(columnIndex => reducedAutomataMatrix[rowIndex, columnIndex]));
            foreach (var gridIndex in gridIndices) {
                var grid = orderedGrids.Left[gridIndex];
                if (grid.Rows.Contains(rowIndex)) {
                    neededColumns.ExceptWith(grid.Columns);
                    if (neededColumns.Count == 0) {
                        break;
                    }
                }
            }
            return neededColumns.Count == 0;
        }

        public static bool SubsetAssignmentIsLegitimate(Nfa<TAlphabet, int> intersectionRuleNfa, Nfa<TAlphabet, int> minimizedDfa, bool[,] reducedAutomataMatrix, Bimap<int, Grid> orderedGrids) {
            var intersectionRuleDfa = intersectionRuleNfa.Determinize();
            var intersectionRuleDfaOrderedStates = intersectionRuleDfa.States.ToList();
            intersectionRuleDfaOrderedStates.Remove(intersectionRuleDfa.StartStates.First());
            intersectionRuleDfaOrderedStates.Insert(0, intersectionRuleDfa.StartStates.First());

            var processor = new DistinctRecursiveAlgorithmProcessor<KeyValuePair<Nfa<TAlphabet, int>.State /*minimized*/, Nfa<TAlphabet, Nfa<TAlphabet, int>.Configuration>.State /*intersection rule*/>>();
            processor.Add(new KeyValuePair<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, Nfa<TAlphabet, int>.Configuration>.State>(minimizedDfa.StartStates.First(), intersectionRuleDfa.StartStates.First()));
            var isLegitimate = true;
            processor.Run(pair => {
                if (Volatile.Read(ref isLegitimate)) {
                    var minimizedDfaState = pair.Key;
                    var intersectionRuleDfaState = pair.Value;
                    var inputSymbols = minimizedDfa.TransitionFunction[minimizedDfaState].Keys;
                    foreach (var inputSymbol in inputSymbols) {
                        var nextIntersectionRuleDfaState = intersectionRuleDfa.TransitionFunction[intersectionRuleDfaState][inputSymbol].First();
                        var nextMinimizedDfaState = minimizedDfa.TransitionFunction[minimizedDfaState][inputSymbol].First();
                        if (!intersectionRuleDfa.AcceptStates.Contains(nextIntersectionRuleDfaState) && minimizedDfa.AcceptStates.Contains(nextMinimizedDfaState)) {
                            Volatile.Write(ref isLegitimate, false);
                        } else if (!GridSetSpansRow(orderedGrids, nextIntersectionRuleDfaState.Value.Select(s => s.Value), reducedAutomataMatrix, nextMinimizedDfaState.Value)) {
                            Volatile.Write(ref isLegitimate, false);
                        } else {
                            processor.Add(new KeyValuePair<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, Nfa<TAlphabet, int>.Configuration>.State>(nextMinimizedDfaState, nextIntersectionRuleDfaState));
                        }
                    }
                }
            });
            return isLegitimate;
        }

        public Nfa<TAlphabet, TAssignment2> Reassign<TAssignment2>(Func<State, TAssignment2> func) {
            var result = new Nfa<TAlphabet, TAssignment2>();
            var stateMapper = new AutoDictionary<State, Nfa<TAlphabet, TAssignment2>.State>(state => new Nfa<TAlphabet, TAssignment2>.State(func(state)));
            foreach (var state in States) {
                stateMapper.EnsureCreated(state);
            }
            foreach (var state in TransitionFunction.Keys) {
                var sourcePartialEvaluation0 = TransitionFunction[state];
                var targetPartialEvaluation0 = result.TransitionFunction[stateMapper[state]];
                foreach (var inputSymbol in TransitionFunction[state].Keys) {
                    var sourcePartialEvaluation1 = sourcePartialEvaluation0[inputSymbol];
                    var targetPartialEvaluation1 = targetPartialEvaluation0[inputSymbol];
                    foreach (var state1 in sourcePartialEvaluation1) {
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
        /// Minimize this Nfa using the Kameda-Weiner algorithm [1]
        /// </summary>
        /// <returns>A minimal-state Nfa accepting the same language</returns>
        public Nfa<TAlphabet, int> Minimized() {
            Nfa<TAlphabet, Configuration> determinized;
            var sm = MakeStateMap(out determinized);
            Nfa<TAlphabet, int> minimizedSubsetConstructionDfa;
            var rsm = ReduceStateMap(sm, determinized, out minimizedSubsetConstructionDfa);
            var ram = MakeReducedAutomataMatrix(rsm);
            var primeGrids = ComputePrimeGrids(ram);
            var covers = EnumerateCovers(ram, primeGrids);
            foreach (var cover in covers) {
                if (cover.Count == States.Count) {
                    break;
                }
                Bimap<int, Grid> orderedGrids;
                var minNfa = FromIntersectionRule(minimizedSubsetConstructionDfa, cover, out orderedGrids);
                var isLegitimate = SubsetAssignmentIsLegitimate(minNfa, minimizedSubsetConstructionDfa, ram, orderedGrids);
                if (isLegitimate) {
                    return minNfa;
                }
            }
            var stateCount = 0;
            return Reassign(x => Interlocked.Increment(ref stateCount)); //did not find a smaller Nfa. Return this;
        }

        public static Nfa<TAlphabet, TAssignment> Union(IEnumerable<Nfa<TAlphabet, TAssignment>> nfas) {
            var result = new Nfa<TAlphabet, TAssignment>();
            foreach (var nfa in nfas) {
                //don't need to clone the states because they are immutable
                result.StartStates.UnionWith(nfa.StartStates);
                result.AcceptStates.UnionWith(nfa.AcceptStates);
                foreach (var fromState in nfa.TransitionFunction.Keys) {
                    foreach (var inputSymbol in nfa.TransitionFunction[fromState].Keys) {
                        result.TransitionFunction[fromState][inputSymbol].UnionWith(nfa.TransitionFunction[fromState][inputSymbol]);
                    }
                }
                result.States.UnionWith(nfa.States);
            }
            return result;
        }

        public Nfa<TAlphabet, int> MinimizedDfa() {
            Nfa<TAlphabet, Configuration> determinized;
            var sm = MakeStateMap(out determinized);
            Nfa<TAlphabet, int> minimizedSubsetConstructionDfa;
            ReduceStateMap(sm, determinized, out minimizedSubsetConstructionDfa);
            return minimizedSubsetConstructionDfa;
        }

        public bool IsEquivalent<TAssignment2>(Nfa<TAlphabet, TAssignment2> that) {
            var thisMinDfa = MinimizedDfa();
            var thatMinDfa = that.MinimizedDfa();
            var equivalent = true;
            var stateMap = new ConcurrentDictionary<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, int>.State>();
            var processor = new DistinctRecursiveAlgorithmProcessor<KeyValuePair<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, int>.State>>();
            processor.Add(new KeyValuePair<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, int>.State>(thisMinDfa.StartStates.First(), thatMinDfa.StartStates.First())); //only one start state since it's a min dfa
            processor.Run(pair => {
                if (!equivalent) {
                    return;
                }
                foreach (var inputSymbolAndStates in thisMinDfa.TransitionFunction[pair.Key]) {
                    var thisMinDfaInputSymbol = inputSymbolAndStates.Key;
                    var thisMinDfaNextState = inputSymbolAndStates.Value.First(); //deterministic, so only one state
                    var thatMinDfaNextStates = thatMinDfa.TransitionFunction[pair.Value][thisMinDfaInputSymbol];
                    if (thatMinDfaNextStates.Count != 1) { //it will always be either 0 or 1
                        equivalent = false;
                    } else {
                        var thatMinDfaNextState = thatMinDfaNextStates.First();
                        var mappedThisMinDfaNextState = stateMap.GetOrAdd(thisMinDfaNextState, thisMinDfaNextStateProxy => {
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
            var minDets = nfas.Select(nfa => nfa.MinimizedDfa());
            var singleTransitionNfa = Nfa<TAlphabet, int>.Union(minDets);
            var singleTransitionFunction = singleTransitionNfa.TransitionFunction;
            var singleAcceptStates = singleTransitionNfa.AcceptStates;
            var stateCount = 0;
            var resultStates = new AutoDictionary<ReadOnlyHashSet<Nfa<TAlphabet, int>.State>, Nfa<TAlphabet, int>.State>(x => new Nfa<TAlphabet, int>.State(Interlocked.Increment(ref stateCount)));
            var processor = new DistinctRecursiveAlgorithmProcessor<ReadOnlyHashSet<Nfa<TAlphabet, int>.State>>();
            var startStateSet = new ReadOnlyHashSet<Nfa<TAlphabet, int>.State>(minDets.Select(x => x.StartStates.First()));
            var result = new Nfa<TAlphabet, int>();
            var acceptStates = new ConcurrentSet<Nfa<TAlphabet, int>.State>();
            processor.Add(startStateSet);
            processor.Run(stateSet => {
                var fromState = resultStates[stateSet];
                if (singleAcceptStates.IsSupersetOf(stateSet)) {
                    acceptStates.TryAdd(fromState);
                }
                var fromSymbols = ReadOnlyHashSet<TAlphabet>.MultiIntersect(stateSet.Select(state => singleTransitionFunction[state].Keys));
                foreach (var fromSymbol in fromSymbols) {
                    var symbol = fromSymbol;
                    var nextStateSet = new ReadOnlyHashSet<Nfa<TAlphabet, int>.State>(stateSet.Select(state => singleTransitionFunction[state][symbol].First()));
                    var toState = resultStates[nextStateSet];
                    processor.Add(nextStateSet);
                    result.TransitionFunction[fromState][fromSymbol].Add(toState);
                }
            });
            result.States.UnionWith(resultStates.Values);
            result.StartStates.Add(resultStates[startStateSet]);
            result.AcceptStates.UnionWith(acceptStates);
            return result;
        }

        public bool Contains(Nfa<TAlphabet, TAssignment> that) {
            return Intersect(new[] {this, that}).IsEquivalent(that);
        }

        public IEnumerable<IEnumerable<State>> GetRoutes(State fromState, State toState, HashSet<State> ignoredStates) {
            var subsequentStates = TransitionFunction[fromState].SelectMany(inputSymbolAndToStates => inputSymbolAndToStates.Value).Distinct().Where(s => !ignoredStates.Contains(s)).ToList();
            foreach (var subsequentState in subsequentStates) {
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

        public struct Transition {
            public readonly State FromState;
            public readonly TAlphabet InputSymbol;
            public readonly State ToState;

            public Transition(State fromState, TAlphabet inputSymbol, State toState) : this() {
                FromState = fromState;
                InputSymbol = inputSymbol;
                ToState = toState;
            }

            public bool Equals(Transition other) {
                return FromState.Equals(other.FromState) && InputSymbol.Equals(other.InputSymbol) && ToState.Equals(other.ToState);
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) {
                    return false;
                }
                return obj is Transition && Equals((Transition) obj);
            }

            public override int GetHashCode() {
                unchecked {
                    int hashCode = FromState.GetHashCode();
                    hashCode = (hashCode * 397) ^ InputSymbol.GetHashCode();
                    hashCode = (hashCode * 397) ^ ToState.GetHashCode();
                    return hashCode;
                }
            }

            public static bool operator ==(Transition left, Transition right) {
                return left.Equals(right);
            }

            public static bool operator !=(Transition left, Transition right) {
                return !left.Equals(right);
            }
        }
    }
}

/*
 * References:
 * [1] Kameda, T. ; IEEE ; Weiner, Peter
 *      "On the State Minimization of Nondeterministic Finite Automata"
 *      Computers, IEEE Transactions on  (Volume:C-19 ,  Issue: 7 )
 */
