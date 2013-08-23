using System.Collections.Generic.More;
using System.Diagnostics;
using Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.More;
using System.Threading.Tasks;

namespace IDE {
    /// <summary>
    /// A Nondeterministic Finite Automaton (∪-NFA)
    /// </summary>
    /// <typeparam name="TAlphabet">The domain of the transition function is S x TAlphabet, where S is the set of states.</typeparam>
    /// <typeparam name="TAssignment">The type of the value associated with a state.</typeparam>
    public partial class Nfa<TAlphabet, TAssignment> {

        /// <summary>
        /// A State of an NFA
        /// </summary>
        public class State {
            public TAssignment Value;

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

        public static ReducedStateMap ReduceStateMap(StateMap stateMap, Nfa<TAlphabet, Configuration> subsetConstructionDfa, out Nfa<TAlphabet, int> reducedSubsetConstructionDfa) {
            //construct an elementary automata matrix (EAM) [1]
            var elementaryAutomataMatrix = MakeElementaryAutomataMatrix(stateMap);

            //determine which rows can be merged
            var rowsToMerge = new List<HashSet<int>>();
            {
                var unmergedRows = Enumerable.Range(0, stateMap.Rows.Count).ToList();
                while(unmergedRows.Count > 0) {
                    rowsToMerge.Add(new HashSet<int> {unmergedRows[0]});
                    for (var rowIndex1 = 1; rowIndex1 < unmergedRows.Count; rowIndex1++) {
                        int columnIndex;
                        for (columnIndex = 0; columnIndex < stateMap.Columns.Count; columnIndex++) {
                            if (elementaryAutomataMatrix[unmergedRows[0], columnIndex] != elementaryAutomataMatrix[unmergedRows[rowIndex1], columnIndex]) break;
                        }
                        if (columnIndex != stateMap.Columns.Count) {
                            continue;
                        }
                        rowsToMerge[rowsToMerge.Count - 1].Add(unmergedRows[rowIndex1]);
                        unmergedRows.RemoveAt(rowIndex1);
                        rowIndex1--;
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
                        rowsToMerge[rowsToMerge.Count - 1].Add(unmergedColumns[columnIndex]);
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
            reducedSubsetConstructionDfa = GenerateEquivalenceClassReducedDfa(subsetConstructionDfa, configurationToEquivalenceClassRowIndex);

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

            public bool IsProperSupersetOf(Grid other) {
                return Rows.IsProperSupersetOf(other.Rows) && Columns.IsProperSupersetOf(other.Columns);
            }
        }

        public static Grid[] ComputePrimeGrids(bool[,] reducedAutomataMatrix) {
            var gridsToProcess = new ConcurrentQueue<Grid>();
            var gridsThatHaveBeenQueued = new ConcurrentSet<Grid>();
            int rowCount = reducedAutomataMatrix.GetUpperBound(0) + 1;
            int columnCount = reducedAutomataMatrix.GetUpperBound(1) + 1;

            Action<Grid> tryQueueGrid = grid => {
                if (gridsThatHaveBeenQueued.TryAdd(grid)) {
                    gridsToProcess.Enqueue(grid);
                }
            };

            //make initial grids which contain only one element
            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++) {
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++) {
                    if (reducedAutomataMatrix[rowIndex, columnIndex]) {
                        var grid = new Grid(new[] {rowIndex}, new[] {columnIndex});
                        tryQueueGrid(grid);
                    }
                }
            }

            //then, grow them incrementally, adding them back into the queue
            //or saving them if they cannot be grown
            var results = new ConcurrentSet<Grid>();
            while(gridsToProcess.Count > 0) {
                Parallel.ForEach(Enumerable.Range(0, gridsToProcess.Count), index => {
                    Grid grid;
                    if (gridsToProcess.TryDequeue(out grid)) {
                        Debug.Assert(grid != null);
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
                                tryQueueGrid(newGrid);
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
                                tryQueueGrid(newGrid);
                                isPrime = false;
                            }
                        }
                        //if it's prime, then save it to the results
                        if (isPrime) {
                            results.TryAdd(grid);
                        }
                    } else {
                        throw new ApplicationException();
                    }
                });
            }
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

        public static Nfa<TAlphabet, int> FromIntersectionRule(Nfa<TAlphabet, int> reducedDfa, Cover cover) {
            var orderedReducedDfaStates = reducedDfa.States.OrderBy(x => x.Value).ToList();
            var subsetAssignmentFunction = MakeSubsetAssignmentFunction(cover);
            var counter = 0;
            var orderedGrids = cover.ToBimap(x => counter++, x => x);
            var result = new Nfa<TAlphabet, int>();
            var intToResultState = new AutoDictionary<int, Nfa<TAlphabet, int>.State>(i => new Nfa<TAlphabet, int>.State(i));
            for (var resultStateIndex = 0; resultStateIndex < orderedGrids.Count; resultStateIndex++) {
                var grid = orderedGrids.Left[resultStateIndex];
                var resultState = intToResultState[resultStateIndex];
                var resultTransitionPartialLambda = result.TransitionFunction[resultState];
                var rows = grid.Rows.Select(rowIndex => orderedReducedDfaStates[rowIndex]);
                var symbols = ReadOnlyHashSet<TAlphabet>.MultiIntersect(rows.Select(row => reducedDfa.TransitionFunction[row].Keys));
                foreach (var symbol in symbols) {
                    var gridSets = rows.Select(row => subsetAssignmentFunction[reducedDfa.TransitionFunction[row][symbol].First().Value]);
                    var nextGrids = ReadOnlyHashSet<Grid>.MultiIntersect(gridSets);
                    var nextIndices = nextGrids.Select(nextGrid => orderedGrids.Right[nextGrid]);
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
            return result;
        }

        bool CoverIsLegitimate(JaggedAutoDictionary<int, int, HashSet<State>> reducedStateMap, Cover cover) {
            throw new NotImplementedException();
        }
    }
}

/*
 * References:
 * [1] Kameda, T. ; IEEE ; Weiner, Peter
 *      "On the State Minimization of Nondeterministic Finite Automata"
 *      Computers, IEEE Transactions on  (Volume:C-19 ,  Issue: 7 )
 */
