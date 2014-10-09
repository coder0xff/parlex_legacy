using System.Collections.Concurrent.More;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Generic.More;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.More;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.More;

namespace Automata {
    /// <summary>
    /// A Nondeterministic Finite Automaton (∪-Nfa) with the ability to store an additional value with each State
    /// </summary>
    /// <typeparam name="TAlphabet">The domain of the transition function is S x TAlphabet, where S is the set of states.</typeparam>
    /// <typeparam name="TAssignment">The type of the value associated with a state.</typeparam>
    public class Nfa<TAlphabet, TAssignment> {

        /// <summary>
        /// A State of an Nfa
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public class State {
            private readonly TAssignment _value;

            public TAssignment Value {
                get { return _value; }
            }

            public override string ToString()
            {
                return _value.ToString();
            }

            public State(TAssignment value) {
                _value = value;
            }
        }

        private readonly HashSet<State> _states = new HashSet<State>();

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public HashSet<State> States {
            get { return _states; }
        }

        private readonly JaggedAutoDictionary<State, TAlphabet, HashSet<State>> _transitionFunction = new JaggedAutoDictionary<State, TAlphabet, HashSet<State>>((dontCare0, dontCare1) => new HashSet<State>());

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public JaggedAutoDictionary<State, TAlphabet, HashSet<State>> TransitionFunction {
            get { return _transitionFunction; }
        }

        private readonly HashSet<State> _startStates = new HashSet<State>();

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public HashSet<State> StartStates {
            get { return _startStates; }
        }

        private readonly HashSet<State> _acceptStates = new HashSet<State>();

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public HashSet<State> AcceptStates {
            get { return _acceptStates; }
        }

        public class Transition
        {
            public State FromState;
            public TAlphabet Symbol;
            public State ToState;
        }

        public IEnumerable<Transition> GetTransitions() {
            return
                _transitionFunction.SelectMany(
                    x => x.Value.SelectMany(y => y.Value.Select(z => new Transition{FromState = x.Key, Symbol = y.Key, ToState = z})));
        }

        public HashSet<State> TransitionFunctionExtended(IEnumerable<State> fromStates, TAlphabet input) {
            if (fromStates == null) throw new ArgumentNullException("fromStates");
            var result = new HashSet<State>();
            foreach (var state in fromStates) {
                result.UnionWith(_transitionFunction[state][input]);
            }
            return result;
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public HashSet<State> TransitionFunctionExtended(State fromState, IEnumerable<TAlphabet> inputString) {
            var currentStateSet = new HashSet<State> { fromState };
            return inputString.Aggregate(currentStateSet, TransitionFunctionExtended);
        }

        public HashSet<State> TransitionFunctionExtended(IEnumerable<State> fromStates, IEnumerable<TAlphabet> inputString) {
            var currentStateSet = new HashSet<State>(fromStates);
            return inputString.Aggregate(currentStateSet, TransitionFunctionExtended);
        }

        /// <summary>
        /// An immutable set of States that can be quickly tested for inequality
        /// </summary>
        public class StateSet : ReadOnlyHashSet<State> {
            public StateSet(IEnumerable<State> items)
                : base(items) {
            }
        }

        /// <summary>
        /// Creates a new Nfa that has only one transition for each input symbol for each state - i.e. it is deterministic
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
                    var isAcceptState = stateSetProxy.Any(x => AcceptStates.Contains(x));
                    if (isAcceptState) {
                        resultAcceptStates.Add(newState);
                    }
                    var transitions = stateSetProxy.Select(x => TransitionFunction[x]).SelectMany(y => y.Keys).Distinct();
                    Parallel.ForEach(transitions, transition => {
                        var nextStateSet = TransitionFunctionExtended(stateSetProxy, transition);
                        if (nextStateSet.Count > 0) {
                            var nextState = adder(new StateSet(nextStateSet));
                            result.TransitionFunction[newState][transition].Add(nextState);
                        }
                    });
                }, TaskCreationOptions.AttachedToParent);
                return newState;
            });

            var startStateSet = new StateSet(StartStates);
            Task.Factory.StartNew(() => {
                result.StartStates.Add(adder(startStateSet));
            }).Wait();
            result.States.UnionWith(stateSetToDeterminizedState.Values);
            foreach (var acceptState in resultAcceptStates) {
                result.AcceptStates.Add(acceptState);
            }
            return result;
        }

        /// <summary>
        /// Creates a new Nfa that recognizes the reversed language
        /// </summary>
        /// <returns>The new Nfa</returns>
        public Nfa<TAlphabet, TAssignment> Dual() {
            var result = new Nfa<TAlphabet, TAssignment>();
            result._startStates.UnionWith(_acceptStates);
            result._states.UnionWith(_states);
            foreach (var keyValuePair in _transitionFunction) {
                foreach (var valuePair in keyValuePair.Value) {
                    foreach (var state in valuePair.Value) {
                        result._transitionFunction[state][valuePair.Key].Add(keyValuePair.Key);
                    }
                }
            }
            result._acceptStates.UnionWith(_startStates);
            return result;
        }

        class StateMap {
            [SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")]
            public readonly HashSet<State>[,] Map;
            public readonly Bimap<StateSet, int> Rows = new Bimap<StateSet, int>();
            public readonly Bimap<StateSet, int> Columns = new Bimap<StateSet, int>();

            public StateMap(int rowCount, int columnCount) {
                Map = new HashSet<State>[rowCount, columnCount];
            }
        }

        class ReducedStateMap {
            [SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")]
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
        StateMap MakeStateMap(out Nfa<TAlphabet, StateSet> determinized) {
            determinized = Determinize();
            var determinizedDual = Dual().Determinize();

            var orderedRows = determinized.States.ToList();
            orderedRows.Remove(determinized.StartStates.First());
            orderedRows.Insert(0, determinized.StartStates.First());

            var orderedColumns = determinizedDual.States.ToList();
            orderedColumns.Remove(determinizedDual.StartStates.First());
            orderedColumns.Insert(0, determinizedDual.StartStates.First());

            var result = new StateMap(orderedRows.Count, orderedColumns.Count);
            for (var rowIndex = 0; rowIndex < orderedRows.Count; rowIndex++) {
                var rowState = orderedRows[rowIndex];
                var rowStateSet = new StateSet(rowState.Value);
                result.Rows.Left.Add(rowStateSet, rowIndex);
                for (var columnIndex = 0; columnIndex < orderedColumns.Count; columnIndex++) {
                    var columnState = orderedColumns[columnIndex];
                    var columnStateSet = new StateSet(columnState.Value);
                    result.Map[rowIndex, columnIndex] = new HashSet<State>(rowStateSet.Intersect(columnStateSet));
                }
            }
            for (var columnIndex = 0; columnIndex < orderedColumns.Count; columnIndex++) {
                var columnState = orderedColumns[columnIndex];
                var columnStateSet = new StateSet(columnState.Value);
                result.Columns.Left.Add(columnStateSet, columnIndex);
            }
            return result;
        }

        static bool[,] MakeElementaryAutomatonMatrix(StateMap stateMap) {
            var result = new bool[stateMap.Rows.Count, stateMap.Columns.Count];
            for (var rowIndex = 0; rowIndex < stateMap.Rows.Count; rowIndex++) {
                for (var columnIndex = 0; columnIndex < stateMap.Columns.Count; columnIndex++) {
                    result[rowIndex, columnIndex] = stateMap.Map[rowIndex, columnIndex].Count > 0;
                }
            }
            return result;
        }

        static Nfa<TAlphabet, int> GenerateEquivalenceClassReducedDfa(Nfa<TAlphabet, StateSet> subsetConstructionDfa, Dictionary<StateSet, int> equivalenceClassLookup) {
            var result = new Nfa<TAlphabet, int>();
            var intToResultState = new AutoDictionary<int, Nfa<TAlphabet, int>.State>(i => new Nfa<TAlphabet, int>.State(i));
            result.StartStates.Add(intToResultState[equivalenceClassLookup[subsetConstructionDfa.StartStates.First().Value]]);
            foreach (var acceptState in subsetConstructionDfa.AcceptStates) {
                result.AcceptStates.Add(intToResultState[equivalenceClassLookup[acceptState.Value]]);
            }
            foreach (var keyValuePair in subsetConstructionDfa.TransitionFunction) {
                var fromState = intToResultState[equivalenceClassLookup[keyValuePair.Key.Value]];
                foreach (var valuePair in keyValuePair.Value) {
                    var inputSymbol = valuePair.Key;
                    foreach (var state in valuePair.Value) {
                        var toState = intToResultState[equivalenceClassLookup[state.Value]];
                        result.TransitionFunction[fromState][inputSymbol].Add(toState);
                    }
                }
            }
            result.States.UnionWith(intToResultState.Values);
            return result;
        }

        static ReducedStateMap ReduceStateMap(StateMap stateMap, Nfa<TAlphabet, StateSet> subsetConstructionDfa, out Nfa<TAlphabet, int> minimizedSubsetConstructionDfa) {
            //construct an elementary automata matrix (EAM) [1]
            var elementaryAutomataMatrix = MakeElementaryAutomatonMatrix(stateMap);

            //determine which rows can be merged
            var rowsToMerge = new List<HashSet<int>>();
            {
                var unmergedRows = Enumerable.Range(0, stateMap.Rows.Count).ToList();
                while (unmergedRows.Count > 0) {
                    rowsToMerge.Add(new HashSet<int> { unmergedRows[0] });
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
                while (unmergedColumns.Count > 0) {
                    columnsToMerge.Add(new HashSet<int> { unmergedColumns[0] });
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
            var stateSetToEquivalenceClassRowIndex = new Dictionary<StateSet, int>();
            for (var equivalenceClassRowIndex = 0; equivalenceClassRowIndex < rowsToMerge.Count; equivalenceClassRowIndex++) {
                foreach (var row in rowsToMerge[equivalenceClassRowIndex]) {
                    stateSetToEquivalenceClassRowIndex[stateMap.Rows.Right[row]] = equivalenceClassRowIndex;
                }
                var rowName = new ReadOnlyHashSet<int>(rowsToMerge[equivalenceClassRowIndex]);
                result.Rows.Left.Add(rowName, equivalenceClassRowIndex);
            }
            minimizedSubsetConstructionDfa = GenerateEquivalenceClassReducedDfa(subsetConstructionDfa, stateSetToEquivalenceClassRowIndex);

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

        static bool[,] MakeReducedAutomataMatrix(ReducedStateMap reducedStateMap) {
            var result = new bool[reducedStateMap.Rows.Count, reducedStateMap.Columns.Count];
            for (var rowIndex = 0; rowIndex < reducedStateMap.Rows.Count; rowIndex++) {
                for (var columnIndex = 0; columnIndex < reducedStateMap.Columns.Count; columnIndex++) {
                    result[rowIndex, columnIndex] = reducedStateMap.Map[rowIndex, columnIndex].Count > 0;
                }
            }
            return result;
        }

        class Grid {
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

        static Grid[] ComputePrimeGrids(bool[,] reducedAutomataMatrix) {
            var gridsToProcess = new DistinctRecursiveAlgorithmProcessor<Grid>();
            var rowCount = reducedAutomataMatrix.GetUpperBound(0) + 1;
            var columnCount = reducedAutomataMatrix.GetUpperBound(1) + 1;

            //make initial grids which contain only one element
            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++) {
                for (var columnIndex = 0; columnIndex < columnCount; columnIndex++) {
                    if (reducedAutomataMatrix[rowIndex, columnIndex]) {
                        var grid = new Grid(new[] { rowIndex }, new[] { columnIndex });
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
                    var comparisonRow = grid.Rows.First();
                    foreach (var testRow in Enumerable.Range(0, rowCount).Except(grid.Rows)) {
                        var canExpand = grid.Columns.All(columnIndex => reducedAutomataMatrix[testRow, columnIndex] == reducedAutomataMatrix[comparisonRow, columnIndex]);
                        if (!canExpand) {
                            continue;
                        }
                        var newGrid = new Grid(grid.Rows.Concat(new[] { testRow }), grid.Columns);
                        gridsToProcess.Add(newGrid);
                        isPrime = false;
                    }
                }
                //try expanding to other columns
                {
                    var comparisonColumn = grid.Columns.First();
                    foreach (var testColumn in Enumerable.Range(0, columnCount).Except(grid.Columns)) {
                        var canExpand = grid.Rows.All(rowIndex => reducedAutomataMatrix[rowIndex, testColumn] == reducedAutomataMatrix[rowIndex, comparisonColumn]);
                        if (!canExpand) {
                            continue;
                        }
                        var newGrid = new Grid(grid.Rows, grid.Columns.Concat(new[] { testColumn }));
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

        class Cover : ReadOnlyHashSet<Grid> {
            public Cover(IEnumerable<Grid> items)
                : base(items) {
            }
        }

        static IEnumerable<Cover> EnumerateCovers(Grid[] primeGrids, int firstGridIndex, Dictionary<Grid, HashSet<int>> gridToFlattenedIndicesSet, HashSet<int> flattenedIndicesWithTrue, int gridCount) {
            if (gridCount > primeGrids.Length - firstGridIndex) { //can't reach gridCount == 0 before the recursion runs out of grids
                yield break;
            }
            for (var gridIndex = firstGridIndex; gridIndex < primeGrids.Length; gridIndex++) {
                var primeGrid = primeGrids[gridIndex];
                var primeGridAsEnumerable = new[] { primeGrid };
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

        static IEnumerable<Cover> EnumerateCovers(bool[,] reducedAutomataMatrix, Grid[] primeGrids) {
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

        static Nfa<TAlphabet, int> FromIntersectionRule(Nfa<TAlphabet, int> reducedDfa, Cover cover, out Bimap<int, Grid> orderedGrids) {
            var orderedReducedDfaStates = reducedDfa._states.OrderBy(x => x.Value).ToList();
            var subsetAssignmentFunction = MakeSubsetAssignmentFunction(cover);
            var counter = 0;
            var orderedGridsTemp = cover.ToBimap(x => counter++, x => x);
            var result = new Nfa<TAlphabet, int>();
            var intToResultState = new AutoDictionary<int, Nfa<TAlphabet, int>.State>(i => new Nfa<TAlphabet, int>.State(i));
            for (var resultStateIndex = 0; resultStateIndex < orderedGridsTemp.Count; resultStateIndex++) {
                var grid = orderedGridsTemp.Left[resultStateIndex];
                var resultState = intToResultState[resultStateIndex];
                var resultTransitionPartialLambda = result._transitionFunction[resultState];
                var rows = grid.Rows.Select(rowIndex => orderedReducedDfaStates[rowIndex]);
                var symbols = ReadOnlyHashSet<TAlphabet>.IntersectMany(rows.Select(row => reducedDfa._transitionFunction[row].Keys));
                foreach (var symbol in symbols) {
                    var symbol1 = symbol;
                    var gridSets = rows.Select(row => subsetAssignmentFunction[reducedDfa._transitionFunction[row][symbol1].First().Value]);
                    var nextGrids = ReadOnlyHashSet<Grid>.IntersectMany(gridSets);
                    var nextIndices = nextGrids.Select(nextGrid => orderedGridsTemp.Right[nextGrid]);
                    var nextStates = nextIndices.Select(gridIndex => intToResultState[gridIndex]);
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

        static bool GridSetSpansRow(Bimap<int, Grid> orderedGrids, IEnumerable<int> gridIndices, bool[,] reducedAutomataMatrix, int rowIndex) {
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

        static bool SubsetAssignmentIsLegitimate(Nfa<TAlphabet, int> intersectionRuleNFA, Nfa<TAlphabet, int> minimizedDfa, bool[,] reducedAutomataMatrix, Bimap<int, Grid> orderedGrids) {
            var intersectionRuleDfa = intersectionRuleNFA.Determinize();
            var intersectionRuleDfaOrderedStates = intersectionRuleDfa.States.ToList();
            intersectionRuleDfaOrderedStates.Remove(intersectionRuleDfa.StartStates.First());
            intersectionRuleDfaOrderedStates.Insert(0, intersectionRuleDfa.StartStates.First());

            var processor = new DistinctRecursiveAlgorithmProcessor<KeyValuePair<Nfa<TAlphabet, int>.State /*minimized*/, Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet>.State /*intersection rule*/>>();
            processor.Add(new KeyValuePair<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet>.State>(minimizedDfa.StartStates.First(), intersectionRuleDfa.StartStates.First()));
            var isLegitimate = true;
            processor.Run(pair => {
                if (isLegitimate) {
                    var minimizedDfaState = pair.Key;
                    var intersectionRuleDfaState = pair.Value;
                    var inputSymbols = minimizedDfa.TransitionFunction[minimizedDfaState].Keys;
                    foreach (var inputSymbol in inputSymbols) {
                        if (!intersectionRuleDfa.TransitionFunction[intersectionRuleDfaState][inputSymbol].Any()) {
                            isLegitimate = false;
                            continue;
                        }
                        var nextIntersectionRuleDfaState = intersectionRuleDfa.TransitionFunction[intersectionRuleDfaState][inputSymbol].First();
                        var nextMinimizedDfaState = minimizedDfa.TransitionFunction[minimizedDfaState][inputSymbol].First();
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
            foreach (var state in _states) {
                stateMapper.EnsureCreated(state);
            }
            foreach (var state in _transitionFunction.Keys) {
                var sourcePartialEvaluation0 = _transitionFunction[state];
                var targetPartialEvaluation0 = result._transitionFunction[stateMapper[state]];
                foreach (var inputSymbol in _transitionFunction[state].Keys) {
                    var sourcePartialEvaluation1 = sourcePartialEvaluation0[inputSymbol];
                    var targetPartialEvaluation1 = targetPartialEvaluation0[inputSymbol];
                    foreach (var state1 in sourcePartialEvaluation1) {
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
            foreach (var state in _states) {
                stateMapper.EnsureCreated(state);
            }
            foreach (var fromStateKeyValuePair in TransitionFunction)
            {
                foreach (var transitionKeyValuePair in fromStateKeyValuePair.Value)
                {
                    foreach (var toState in transitionKeyValuePair.Value)
                    {
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
        /// If there are any nodes that cannot be reached
        /// or cannot reach an accept state then remove them
        /// </summary>
        public void RemoveRedundancies() {
            start:
            foreach (var state in States.ToArray()) {
                if (StartStates.Select(x => GetRoutes(x, state).Any()).All(x => x != true) &&
                    AcceptStates.Select(x => GetRoutes(state, x).Any()).All(x => x != true)) {
                    StartStates.Remove(state);
                    AcceptStates.Remove(state);
                    StartStates.Remove(state);
                    _transitionFunction.TryRemove(state);
                    State state1 = state;
                    foreach (var transition in GetTransitions().Where(x => x.ToState == state1)) {
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
        /// Minimize this Nfa using the Kameda-Weiner algorithm [1]
        /// </summary>
        /// <returns>A minimal-state Nfa accepting the same language</returns>
        public Nfa<TAlphabet, int> Minimized() {
            Nfa<TAlphabet, StateSet> determinized;
            var sm = MakeStateMap(out determinized);
            Nfa<TAlphabet, int> minimizedSubsetConstructionDfa;
            var rsm = ReduceStateMap(sm, determinized, out minimizedSubsetConstructionDfa);
            var ram = MakeReducedAutomataMatrix(rsm);
            var primeGrids = ComputePrimeGrids(ram);
            var covers = EnumerateCovers(ram, primeGrids);
            foreach (var cover in covers) {
                if (cover.Count == _states.Count) {
                    break;
                }
                Bimap<int, Grid> orderedGrids;
                var minNFA = FromIntersectionRule(minimizedSubsetConstructionDfa, cover, out orderedGrids);
                var isLegitimate = SubsetAssignmentIsLegitimate(minNFA, minimizedSubsetConstructionDfa, ram, orderedGrids);
                if (isLegitimate) {
                    minNFA.RemoveRedundancies();
                    return minNFA;
                }
            }
            var stateCount = 0;
            return Reassign(x => Interlocked.Increment(ref stateCount)); //did not find a smaller Nfa. Return this;
        }

        public static Nfa<TAlphabet, TAssignment> Union(IEnumerable<Nfa<TAlphabet, TAssignment>> nfas) {
            var result = new Nfa<TAlphabet, TAssignment>();
            foreach (var nfa in nfas) {
                //don't need to clone the states because they are immutable
                result._startStates.UnionWith(nfa._startStates);
                result._acceptStates.UnionWith(nfa._acceptStates);
                foreach (var fromState in nfa._transitionFunction.Keys) {
                    foreach (var inputSymbol in nfa._transitionFunction[fromState].Keys) {
                        result._transitionFunction[fromState][inputSymbol].UnionWith(nfa._transitionFunction[fromState][inputSymbol]);
                    }
                }
                result._states.UnionWith(nfa._states);
            }
            return result;
        }

        public Nfa<TAlphabet, int> MinimizedDfa() {
            Nfa<TAlphabet, StateSet> determinized;
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
            processor.Add(new KeyValuePair<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, int>.State>(thisMinDfa._startStates.First(), thatMinDfa._startStates.First())); //only one start state since it's a min dfa
            processor.Run(pair => {
                if (!equivalent) {
                    return;
                }
                foreach (var inputSymbolAndStates in thisMinDfa._transitionFunction[pair.Key]) {
                    var thisMinDfaInputSymbol = inputSymbolAndStates.Key;
                    var thisMinDfaNextState = inputSymbolAndStates.Value.First(); //deterministic, so only one state
                    var thatMinDfaNextStates = thatMinDfa._transitionFunction[pair.Value][thisMinDfaInputSymbol];
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
            var singleTransitionNFA = Nfa<TAlphabet, int>.Union(minDets);
            var singleTransitionFunction = singleTransitionNFA._transitionFunction;
            var singleAcceptStates = singleTransitionNFA._acceptStates;
            var stateCount = 0;
            var resultStates = new AutoDictionary<ReadOnlyHashSet<Nfa<TAlphabet, int>.State>, Nfa<TAlphabet, int>.State>(x => new Nfa<TAlphabet, int>.State(Interlocked.Increment(ref stateCount)));
            var processor = new DistinctRecursiveAlgorithmProcessor<ReadOnlyHashSet<Nfa<TAlphabet, int>.State>>();
            var startStateSet = new ReadOnlyHashSet<Nfa<TAlphabet, int>.State>(minDets.Select(x => x._startStates.First()));
            var result = new Nfa<TAlphabet, int>();
            var acceptStates = new ConcurrentSet<Nfa<TAlphabet, int>.State>();
            processor.Add(startStateSet);
            processor.Run(stateSet => {
                var fromState = resultStates[stateSet];
                if (singleAcceptStates.IsSupersetOf(stateSet)) {
                    acceptStates.TryAdd(fromState);
                }
                var fromSymbols = ReadOnlyHashSet<TAlphabet>.IntersectMany(stateSet.Select(state => singleTransitionFunction[state].Keys));
                foreach (var fromSymbol in fromSymbols) {
                    var symbol = fromSymbol;
                    var nextStateSet = new ReadOnlyHashSet<Nfa<TAlphabet, int>.State>(stateSet.Select(state => singleTransitionFunction[state][symbol].First()));
                    var toState = resultStates[nextStateSet];
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
            return Intersect(new[] { this, that }).IsEquivalent(that);
        }

        IEnumerable<IEnumerable<State>> GetRoutes(State fromState, State toState, HashSet<State> ignoredStates = null) {
            if (ignoredStates == null) ignoredStates = new HashSet<State>();
            var subsequentStates = _transitionFunction[fromState].SelectMany(inputSymbolAndToStates => inputSymbolAndToStates.Value).Distinct().Where(s => !ignoredStates.Contains(s)).ToList();
            foreach (var subsequentState in subsequentStates) {
                if (subsequentState == toState) {
                    yield return new[] { toState };
                }
                ignoredStates.Add(subsequentState);
                foreach (var route in GetRoutes(subsequentState, toState, ignoredStates)) {
                    yield return new[] { subsequentState }.Concat(route);
                }
                ignoredStates.Remove(subsequentState);
            }
        }

        public IEnumerable<IEnumerable<State>> GetCycles() {
            return _states.SelectMany(state => GetRoutes(state, state, new HashSet<State>()));
        }

        public string ToString(Func<TAlphabet, String> transitionStringifier)
        {
            var result = new StringBuilder();
            var nodeCounter = 0;
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
                var fromStateBuilder = labels[fromStateKeyValuePair.Key].ToString();
                foreach (var transitionKeyValuePair in fromStateKeyValuePair.Value) {
                    var transitionBuilder = transitionStringifier(transitionKeyValuePair.Key);
                    foreach (var toState in transitionKeyValuePair.Value) {
                        result.Append(fromStateBuilder);
                        result.Append(" -> ");
                        result.Append(transitionBuilder);
                        result.Append(" -> ");
                        result.Append(labels[toState]);
                        result.Append(System.Environment.NewLine);
                    }
                }
            }
            return result.ToString();

        }

        public override string ToString()
        {
            return ToString(x => x.ToString());
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
                foreach (var acceptState in AcceptStates) {
                    result.Append(" ");
                    result.Append(nodeNames[acceptState]);
                }
                result.AppendLine(";");
                result.AppendLine("\tnode [shape = circle];");
                foreach (var startState in StartStates) {
                    result.Append("\tstart -> ");
                    result.Append(nodeNames[startState]);
                    result.AppendLine(";");
                }
                foreach (var transition in GetTransitions()) {
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

    }

    /// <summary>
    /// A Nondeterministic Finite Automaton (∪-Nfa)
    /// </summary>
    /// <typeparam name="TAlphabet">The domain of the transition function is S x TAlphabet, where S is the set of states.</typeparam>
    public class Nfa<TAlphabet> {

        /// <summary>
        /// A State of an Nfa
        /// </summary>
        public class State
        {
            public override string ToString()
            {
                return "State";
            }
        }

        public readonly HashSet<State> States = new HashSet<State>();
        public readonly JaggedAutoDictionary<State, TAlphabet, HashSet<State>> TransitionFunction = new JaggedAutoDictionary<State, TAlphabet, HashSet<State>>((dontCare0, dontCare1) => new HashSet<State>());
        public readonly HashSet<State> StartStates = new HashSet<State>();
        public readonly HashSet<State> AcceptStates = new HashSet<State>();

        public Nfa() { }

        public class Transition
        {
            public State FromState;
            public TAlphabet Symbol;
            public State ToState;

            public Transition(State fromState, TAlphabet symbol, State toState) {
                FromState = fromState;
                Symbol = symbol;
                ToState = toState;
            }

            public Transition() {
            }
        }

        public IEnumerable<Transition> GetTransitions()
        {
            return from fromStateKeyValuePair in TransitionFunction from transitionKeyValuePair in fromStateKeyValuePair.Value from toState in transitionKeyValuePair.Value select new Transition
            {
                FromState = fromStateKeyValuePair.Key,
                Symbol = transitionKeyValuePair.Key,
                ToState = toState
            };
        }

        public Nfa(Nfa<TAlphabet> other) {
            foreach (var state in other.StartStates) {
                StartStates.Add(state);
            }
            foreach (var state in other.States) {
                States.Add(state);
            }
            foreach (var state in other.AcceptStates) {
                AcceptStates.Add(state);
            }
            foreach (var transition in other.GetTransitions())
            {
                TransitionFunction[transition.FromState][transition.Symbol].Add(transition.ToState);
            }
        }

        public HashSet<State> TransitionFunctionEx(IEnumerable<State> states, TAlphabet input) {
            var result = new HashSet<State>();
            foreach (var state in states) {
                result.UnionWith(TransitionFunction[state][input]);
            }
            return result;
        }

        public HashSet<State> TransitionFunctionEx(State state, IEnumerable<TAlphabet> inputString) {
            var currentStateSet = new HashSet<State> { state };
            return inputString.Aggregate(currentStateSet, TransitionFunctionEx);
        }

        public HashSet<State> TransitionFunctionEx(IEnumerable<State> states, IEnumerable<TAlphabet> inputString) {
            var currentStateSet = new HashSet<State>(states);
            return inputString.Aggregate(currentStateSet, TransitionFunctionEx);
        }

        /// <summary>
        /// An immutable set of States that can be quickly tested for inequality
        /// </summary>
        public class StateSet : ReadOnlyHashSet<State> {
            public StateSet(IEnumerable<State> items)
                : base(items) {
            }
        }

        /// <summary>
        /// Creates a new Nfa that has only one transition for each input symbol for each state - i.e. it is deterministic
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
                    var isAcceptState = stateSetProxy.Any(x => AcceptStates.Contains(x));
                    if (isAcceptState) {
                        resultAcceptStates.Add(newState);
                    }
                    var awayInputs = stateSetProxy.Select(x => TransitionFunction[x]).SelectMany(y => y.Keys).Distinct();
                    Parallel.ForEach(awayInputs, input => {
                        var nextStateSet = TransitionFunctionEx(stateSetProxy, input);
                        if (nextStateSet.Count > 0) {
                            var nextState = adder(new StateSet(nextStateSet));
                            result.TransitionFunction[newState][input].Add(nextState);
                        }
                    });
                }, TaskCreationOptions.AttachedToParent);
                return newState;
            });

            var startStateSet = new StateSet(StartStates);
            Task.Factory.StartNew(() => {
                result.StartStates.Add(adder(startStateSet));
            }).Wait();
            result.States.UnionWith(stateSetToDState.Values);
            foreach (var acceptState in resultAcceptStates) {
                result.AcceptStates.Add(acceptState);
            }
            return result;
        }

        /// <summary>
        /// Creates a new Nfa that recognizes the reversed language
        /// </summary>
        /// <returns>The new Nfa</returns>
        public Nfa<TAlphabet> Dual() {
            var result = new Nfa<TAlphabet>();
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

        class StateMap {
            public readonly HashSet<State>[,] Map;
            public readonly Bimap<StateSet, int> Rows = new Bimap<StateSet, int>();
            public readonly Bimap<StateSet, int> Columns = new Bimap<StateSet, int>();

            public StateMap(int rowCount, int columnCount) {
                Map = new HashSet<State>[rowCount, columnCount];
            }
        }

        class ReducedStateMap {
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
        StateMap MakeStateMap(out Nfa<TAlphabet, StateSet> determinized) {
            determinized = Determinize();
            var determinizedDual = Dual().Determinize();

            var orderedRows = determinized.States.ToList();
            orderedRows.Remove(determinized.StartStates.First());
            orderedRows.Insert(0, determinized.StartStates.First());

            var orderedColumns = determinizedDual.States.ToList();
            orderedColumns.Remove(determinizedDual.StartStates.First());
            orderedColumns.Insert(0, determinizedDual.StartStates.First());

            var result = new StateMap(orderedRows.Count, orderedColumns.Count);
            for (var rowIndex = 0; rowIndex < orderedRows.Count; rowIndex++) {
                var rowState = orderedRows[rowIndex];
                var rowStateSet = new StateSet(rowState.Value);
                result.Rows.Left.Add(rowStateSet, rowIndex);
                for (var columnIndex = 0; columnIndex < orderedColumns.Count; columnIndex++) {
                    var columnState = orderedColumns[columnIndex];
                    var columnStateSet = new StateSet(columnState.Value);
                    result.Map[rowIndex, columnIndex] = new HashSet<State>(rowStateSet.Intersect(columnStateSet));
                }
            }
            for (var columnIndex = 0; columnIndex < orderedColumns.Count; columnIndex++) {
                var columnState = orderedColumns[columnIndex];
                var columnStateSet = new StateSet(columnState.Value);
                result.Columns.Left.Add(columnStateSet, columnIndex);
            }
            return result;
        }

        static bool[,] MakeElementaryAutomatonMatrix(StateMap stateMap) {
            var result = new bool[stateMap.Rows.Count, stateMap.Columns.Count];
            for (var rowIndex = 0; rowIndex < stateMap.Rows.Count; rowIndex++) {
                for (var columnIndex = 0; columnIndex < stateMap.Columns.Count; columnIndex++) {
                    result[rowIndex, columnIndex] = stateMap.Map[rowIndex, columnIndex].Count > 0;
                }
            }
            return result;
        }

        static Nfa<TAlphabet, int> GenerateEquivalenceClassReducedDfa(Nfa<TAlphabet, StateSet> subsetConstructionDfa, Dictionary<StateSet, int> equivalenceClassLookup) {
            var result = new Nfa<TAlphabet, int>();
            var intToResultState = new AutoDictionary<int, Nfa<TAlphabet, int>.State>(i => new Nfa<TAlphabet, int>.State(i));
            result.StartStates.Add(intToResultState[equivalenceClassLookup[subsetConstructionDfa.StartStates.First().Value]]);
            foreach (var acceptState in subsetConstructionDfa.AcceptStates) {
                result.AcceptStates.Add(intToResultState[equivalenceClassLookup[acceptState.Value]]);
            }
            foreach (var keyValuePair in subsetConstructionDfa.TransitionFunction) {
                var fromState = intToResultState[equivalenceClassLookup[keyValuePair.Key.Value]];
                foreach (var valuePair in keyValuePair.Value) {
                    var inputSymbol = valuePair.Key;
                    foreach (var state in valuePair.Value) {
                        var toState = intToResultState[equivalenceClassLookup[state.Value]];
                        result.TransitionFunction[fromState][inputSymbol].Add(toState);
                    }
                }
            }
            result.States.UnionWith(intToResultState.Values);
            return result;
        }

        static ReducedStateMap ReduceStateMap(StateMap stateMap, Nfa<TAlphabet, StateSet> subsetConstructionDfa, out Nfa<TAlphabet, int> minimizedSubsetConstructionDfa) {
            //construct an elementary automata matrix (EAM) [1]
            var elementaryAutomataMatrix = MakeElementaryAutomatonMatrix(stateMap);

            //determine which rows can be merged
            var rowsToMerge = new List<HashSet<int>>();
            {
                var unmergedRows = Enumerable.Range(0, stateMap.Rows.Count).ToList();
                while (unmergedRows.Count > 0) {
                    rowsToMerge.Add(new HashSet<int> { unmergedRows[0] });
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
                while (unmergedColumns.Count > 0) {
                    columnsToMerge.Add(new HashSet<int> { unmergedColumns[0] });
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
            var stateSetToEquivalenceClassRowIndex = new Dictionary<StateSet, int>();
            for (var equivalenceClassRowIndex = 0; equivalenceClassRowIndex < rowsToMerge.Count; equivalenceClassRowIndex++) {
                foreach (var row in rowsToMerge[equivalenceClassRowIndex]) {
                    stateSetToEquivalenceClassRowIndex[stateMap.Rows.Right[row]] = equivalenceClassRowIndex;
                }
                var rowName = new ReadOnlyHashSet<int>(rowsToMerge[equivalenceClassRowIndex]);
                result.Rows.Left.Add(rowName, equivalenceClassRowIndex);
            }
            minimizedSubsetConstructionDfa = GenerateEquivalenceClassReducedDfa(subsetConstructionDfa, stateSetToEquivalenceClassRowIndex);

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

        static bool[,] MakeReducedAutomataMatrix(ReducedStateMap reducedStateMap) {
            var result = new bool[reducedStateMap.Rows.Count, reducedStateMap.Columns.Count];
            for (var rowIndex = 0; rowIndex < reducedStateMap.Rows.Count; rowIndex++) {
                for (var columnIndex = 0; columnIndex < reducedStateMap.Columns.Count; columnIndex++) {
                    result[rowIndex, columnIndex] = reducedStateMap.Map[rowIndex, columnIndex].Count > 0;
                }
            }
            return result;
        }

        class Grid {
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

        static Grid[] ComputePrimeGrids(bool[,] reducedAutomataMatrix) {
            var gridsToProcess = new DistinctRecursiveAlgorithmProcessor<Grid>();
            var rowCount = reducedAutomataMatrix.GetUpperBound(0) + 1;
            var columnCount = reducedAutomataMatrix.GetUpperBound(1) + 1;

            //make initial grids which contain only one element
            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++) {
                for (var columnIndex = 0; columnIndex < columnCount; columnIndex++) {
                    if (reducedAutomataMatrix[rowIndex, columnIndex]) {
                        var grid = new Grid(new[] { rowIndex }, new[] { columnIndex });
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
                    var comparisonRow = grid.Rows.First();
                    foreach (var testRow in Enumerable.Range(0, rowCount).Except(grid.Rows)) {
                        var canExpand = grid.Columns.All(columnIndex => reducedAutomataMatrix[testRow, columnIndex] == reducedAutomataMatrix[comparisonRow, columnIndex]);
                        if (!canExpand) {
                            continue;
                        }
                        var newGrid = new Grid(grid.Rows.Concat(new[] { testRow }), grid.Columns);
                        gridsToProcess.Add(newGrid);
                        isPrime = false;
                    }
                }
                //try expanding to other columns
                {
                    var comparisonColumn = grid.Columns.First();
                    foreach (var testColumn in Enumerable.Range(0, columnCount).Except(grid.Columns)) {
                        var canExpand = grid.Rows.All(rowIndex => reducedAutomataMatrix[rowIndex, testColumn] == reducedAutomataMatrix[rowIndex, comparisonColumn]);
                        if (!canExpand) {
                            continue;
                        }
                        var newGrid = new Grid(grid.Rows, grid.Columns.Concat(new[] { testColumn }));
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

        class Cover : ReadOnlyHashSet<Grid> {
            public Cover(IEnumerable<Grid> items)
                : base(items) {
            }
        }

        static IEnumerable<Cover> EnumerateCovers(Grid[] primeGrids, int firstGridIndex, Dictionary<Grid, HashSet<int>> gridToFlattenedIndicesSet, HashSet<int> flattenedIndicesWithTrue, int gridCount) {
            if (gridCount > primeGrids.Length - firstGridIndex) { //can't reach gridCount == 0 before the recursion runs out of grids
                yield break;
            }
            for (var gridIndex = firstGridIndex; gridIndex < primeGrids.Length; gridIndex++) {
                var primeGrid = primeGrids[gridIndex];
                var primeGridAsEnumerable = new[] { primeGrid };
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

        static IEnumerable<Cover> EnumerateCovers(bool[,] reducedAutomataMatrix, Grid[] primeGrids) {
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

        static Nfa<TAlphabet, int> FromIntersectionRule(Nfa<TAlphabet, int> reducedDfa, Cover cover, out Bimap<int, Grid> orderedGrids) {
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
                var symbols = ReadOnlyHashSet<TAlphabet>.IntersectMany(rows.Select(row => reducedDfa.TransitionFunction[row].Keys));
                foreach (var symbol in symbols) {
                    var symbol1 = symbol;
                    var gridSets = rows.Select(row => subsetAssignmentFunction[reducedDfa.TransitionFunction[row][symbol1].First().Value]);
                    var nextGrids = ReadOnlyHashSet<Grid>.IntersectMany(gridSets);
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

        static bool GridSetSpansRow(Bimap<int, Grid> orderedGrids, IEnumerable<int> gridIndices, bool[,] reducedAutomataMatrix, int rowIndex) {
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

        static bool SubsetAssignmentIsLegitimate(Nfa<TAlphabet, int> intersectionRuleNFA, Nfa<TAlphabet, int> minimizedDfa, bool[,] reducedAutomataMatrix, Bimap<int, Grid> orderedGrids) {
            var intersectionRuleDfa = intersectionRuleNFA.Determinize();
            var intersectionRuleDfaOrderedStates = intersectionRuleDfa.States.ToList();
            intersectionRuleDfaOrderedStates.Remove(intersectionRuleDfa.StartStates.First());
            intersectionRuleDfaOrderedStates.Insert(0, intersectionRuleDfa.StartStates.First());

            var processor = new DistinctRecursiveAlgorithmProcessor<KeyValuePair<Nfa<TAlphabet, int>.State /*minimized*/, Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet>.State /*intersection rule*/>>();
            processor.Add(new KeyValuePair<Nfa<TAlphabet, int>.State, Nfa<TAlphabet, Nfa<TAlphabet, int>.StateSet>.State>(minimizedDfa.StartStates.First(), intersectionRuleDfa.StartStates.First()));
            var isLegitimate = true;
            processor.Run(pair => {
                if (isLegitimate) {
                    var minimizedDfaState = pair.Key;
                    var intersectionRuleDfaState = pair.Value;
                    var inputSymbols = minimizedDfa.TransitionFunction[minimizedDfaState].Keys;
                    foreach (var inputSymbol in inputSymbols) {
                        var nextIntersectionRuleDfaState = intersectionRuleDfa.TransitionFunction[intersectionRuleDfaState][inputSymbol].First();
                        var nextMinimizedDfaState = minimizedDfa.TransitionFunction[minimizedDfaState][inputSymbol].First();
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

        public Nfa<TAlphabet, int> Reassign()
        {
            int counter = 0;
            return Reassign(_ => Interlocked.Increment(ref counter));
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

        public Nfa<TAlphabet> Clone() {
            var result = new Nfa<TAlphabet>();
            var stateMapper = new AutoDictionary<State, State>(state => new State());
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
        public Nfa<TAlphabet> Minimized()
        {
            return Reassign().Minimized().Reassign();
        }

        public static Nfa<TAlphabet> Union(IEnumerable<Nfa<TAlphabet>> nfas) {
            var result = new Nfa<TAlphabet>();
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
            Nfa<TAlphabet, StateSet> determinized;
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

        public static Nfa<TAlphabet, int> Intersect(IEnumerable<Nfa<TAlphabet>> nfas) {
            var minDets = nfas.Select(nfa => nfa.MinimizedDfa());
            var singleTransitionNFA = Nfa<TAlphabet, int>.Union(minDets);
            var singleTransitionFunction = singleTransitionNFA.TransitionFunction;
            var singleAcceptStates = singleTransitionNFA.AcceptStates;
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
                var fromSymbols = ReadOnlyHashSet<TAlphabet>.IntersectMany(stateSet.Select(state => singleTransitionFunction[state].Keys));
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

        //public bool Contains(Nfa<TAlphabet> that)
        //{
        //    return Intersect(new[] { this, that }).IsEquivalent(that);
        //}

        public IEnumerable<State> GetReachables(State fromState, HashSet<State> ignoredStates = null) {
            if (ignoredStates == null) ignoredStates = new HashSet<State>();
            DistinctRecursiveAlgorithmProcessor<State> processor = new DistinctRecursiveAlgorithmProcessor<State>();
            processor.Add(fromState);
            List<State> results = new List<State>();
            processor.Run(state => {
                foreach (var reachable in TransitionFunction[state].SelectMany(kvp => kvp.Value)) {
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
            if (ignoredStates == null) ignoredStates = new HashSet<State>();
            var subsequentStates = TransitionFunction[fromState].SelectMany(inputSymbolAndToStates => inputSymbolAndToStates.Value).Distinct().Where(s => !ignoredStates.Contains(s)).ToList();
            foreach (var subsequentState in subsequentStates) {
                if (subsequentState == toState) {
                    yield return new[] { toState };
                }
                ignoredStates.Add(subsequentState);
                foreach (var route in GetRoutes(subsequentState, toState, ignoredStates)) {
                    yield return new[] { subsequentState }.Concat(route);
                }
                ignoredStates.Remove(subsequentState);
            }
        }

        public IEnumerable<IEnumerable<State>> GetCycles() {
            return States.SelectMany(state => GetRoutes(state, state, new HashSet<State>()));
        }

        /// <summary>
        /// Inserts an Nfa 'require' at the 'at' state
        /// Any transitions leaving 'at' are removed and stored in outgoingTransitions
        /// Any start states of 'require' become synonymous with at
        /// Any accept states of 'require' have outgoingTransitions added to them
        /// </summary>
        /// <param name="at">The state to insert the Nfa at</param>
        /// <param name="require">The Nfa to insert</param>
        public void Insert(State at, Nfa<TAlphabet> require) {
            var atIsAcceptState = AcceptStates.Contains(at);
            //Store copies of all the transitions leaving 'at'
            var outgoingTransitions = new JaggedAutoDictionary<TAlphabet, List<State>>(_ => new List<State>());
            foreach (var symbol in TransitionFunction[at].Keys) {
                foreach (var toState in TransitionFunction[at][symbol]) {
                    if (!ReferenceEquals(at, toState))
                        outgoingTransitions[symbol].Add(toState);
                }
            }

            foreach (TAlphabet outGoingSymbol in outgoingTransitions.Keys)
            {
                foreach (State toState in outgoingTransitions[outGoingSymbol])
                {
                    TransitionFunction[at][outGoingSymbol].Remove(toState);
                }
                if (TransitionFunction[at][outGoingSymbol].Count == 0)
                {
                    TransitionFunction[at].TryRemove(outGoingSymbol);
                }
            }

            if (!TransitionFunction[at].Any()) TransitionFunction.TryRemove(at);

            //Add all of 'require's states to storage, except start and accept states
            //Simultaneously, create a map from 'require's states to storage's states
            //which for the most part is identity, but start states map to 'at'
            //Also, make a list of the mapped accept states, which we'll use later
            var stateMap = new Dictionary<State, State>();
            var mappedAcceptStates = new List<State>();
            foreach (var state in require.States) {
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
            foreach (var state in require.States) {
                foreach (var symbol in require.TransitionFunction[state].Keys) {
                    foreach (var toState in require.TransitionFunction[state][symbol]) {
                        TransitionFunction[stateMap[state]][symbol].Add(stateMap[toState]);
                    }
                }
            }

            //lastly, hook up the mappedAcceptStates using the saved outgoingTransitions
            foreach (var mappedAcceptState in mappedAcceptStates) {
                foreach (var symbol in outgoingTransitions.Keys) {
                    foreach (var toState in outgoingTransitions[symbol]) {
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

        public string ToString(Func<TAlphabet, String> transitionStringifier)
        {
            int counter = 0;
            return Reassign(_ => Interlocked.Increment(ref counter)).ToString(transitionStringifier);
        }

        public override string ToString()
        {
            return ToString(x => x.ToString());
        }

        public String GraphvizDotLanguage {
            get
            {
                var result = new StringBuilder();
                int counter = 0;
                AutoDictionary<State, String> nodeNames =
                    new AutoDictionary<State, string>(_ => "N" + Interlocked.Increment(ref counter).ToString());
                result.AppendLine("digraph nfa {");
                result.AppendLine("\trankdir=LR;");
                result.AppendLine("\tsize=\"8,5\"");
                result.AppendLine("\tnode [shape = point]; start;");

                result.Append("\tnode [shape = doublecircle];");
                foreach (var acceptState in AcceptStates) {
                    result.Append(" ");
                    result.Append(nodeNames[acceptState]);
                }
                result.AppendLine(";");
                result.AppendLine("\tnode [shape = circle];");
                foreach (var startState in StartStates) {
                    result.Append("\tstart -> ");
                    result.Append(nodeNames[startState]);
                    result.AppendLine(";");
                }
                foreach (var transition in GetTransitions()) {
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
    }
}
/*
 * References:
 * [1] Kameda, T. ; IEEE ; Weiner, Peter
 *      "On the State Minimization of Nondeterministic Finite Automata"
 *      Computers, IEEE Transactions on  (Volume:C-19 ,  Issue: 7 )
 */
