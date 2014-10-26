using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Automata;

namespace Parlex {
    public static class NfaExtensions {
        /// <summary>
        ///     Duplicates the specified state, leaving the recognized language unchanged
        /// </summary>
        /// <typeparam name="TAlphabet"></typeparam>
        /// <param name="nfa"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static Nfa<TAlphabet>.State Cleave<TAlphabet>(this Nfa<TAlphabet> nfa, Nfa<TAlphabet>.State state) {
            if (!nfa.States.Contains(state)) {
                throw new InvalidNfaOperationException("The specified State is not in the Nfa");
            }

            var newState = new Nfa<TAlphabet>.State();
            if (nfa.StartStates.Contains(state)) {
                nfa.StartStates.Add(newState);
            }
            if (nfa.AcceptStates.Contains(state)) {
                nfa.AcceptStates.Add(newState);
            }
            nfa.States.Add(newState);
            var newTransitions =
                new JaggedAutoDictionary<Nfa<TAlphabet>.State, TAlphabet, List<Nfa<TAlphabet>.State>>(
                    (x, y) => new List<Nfa<TAlphabet>.State>());
            foreach (Nfa<TAlphabet>.Transition transition in nfa.GetTransitions()) {
                if (transition.ToState == state) {
                    newTransitions[transition.FromState][transition.Symbol].Add(newState);
                }
                if (transition.FromState == state) {
                    newTransitions[newState][transition.Symbol].Add(transition.ToState);
                }
            }
            foreach (Nfa<TAlphabet>.Transition transition in nfa.GetTransitions()) {
                nfa.TransitionFunction[transition.FromState][transition.Symbol].Add(transition.ToState);
            }
            return newState;
        }

        /// <summary>
        ///     The specified state is replaced by new states which each have transitions going to only the states as grouped by
        ///     toGroups.
        ///     Any state, for which a transition exists to that state from the specified state, that is not specified in toGroups
        ///     will be placed in its own group.
        ///     This is a behavior preserving transformation
        ///     This function is robust and will throw an InvalidNfaOperationException if the inputs are not sane.
        /// </summary>
        /// <typeparam name="TAlphabet"></typeparam>
        /// <param name="nfa"></param>
        /// <param name="state"></param>
        /// <param name="toGroups"></param>
        /// <returns></returns>
        public static IEnumerable<Nfa<TAlphabet>.State> SplitFroms<TAlphabet>(this Nfa<TAlphabet> nfa, Nfa<TAlphabet>.State state,
                                                                              IEnumerable<IEnumerable<Nfa<TAlphabet>.State>> toGroups = null) {
            //do some input sanity checks
            if (state == null) {
                throw new ArgumentNullException("state");
            }
            if (toGroups == null) {
                toGroups = new List<List<Nfa<TAlphabet>.State>>();
            }
            if (!nfa.States.Contains(state)) {
                throw new InvalidNfaOperationException("The specified State is not in the Nfa");
            }

            //cache some lists
            List<List<Nfa<TAlphabet>.State>> cachedTransitionGroups = toGroups.Select(x => x.Select(y => y).ToList()).ToList();
            List<Nfa<TAlphabet>.State> allNonSelfToStates = nfa.GetTransitions().Where(x => x.FromState == state).Select(x => x.ToState).Where(x => x != state).ToList();

            //more input sanity checks
            if (cachedTransitionGroups.Any(cachedTransitionGroup => cachedTransitionGroup.Any(toState => !allNonSelfToStates.Contains(toState)))) {
                throw new InvalidNfaOperationException("One or more of the states specified in toGroups is not a state that has a transition from the specified State");
            }

            if (cachedTransitionGroups.SelectMany(x => x).Count() != cachedTransitionGroups.SelectMany(x => x).Distinct().Count()) {
                throw new InvalidNfaOperationException("One or more states specified in toGroups occurs more than once");
            }

            if (cachedTransitionGroups.SelectMany(x => x).Contains(state)) {
                throw new InvalidNfaOperationException("The specified state may not be included in the specified toGroups");
            }

            //Put each unspecified to-state in its own group
            foreach (Nfa<TAlphabet>.State ungrouped in allNonSelfToStates.Except(cachedTransitionGroups.SelectMany(x => x))) {
                cachedTransitionGroups.Add(new List<Nfa<TAlphabet>.State>(new[] {ungrouped}));
            }

            bool isStart = nfa.StartStates.Contains(state);
            bool isAccept = nfa.AcceptStates.Contains(state);

            var results = new List<Nfa<TAlphabet>.State>();
            //create the new states
            foreach (var cachedTransitionGroup in cachedTransitionGroups) {
                var newState = new Nfa<TAlphabet>.State();
                results.Add(newState);
                nfa.States.Add(newState);
                if (isStart) {
                    nfa.StartStates.Add(newState);
                }
                if (isAccept) {
                    nfa.AcceptStates.Add(newState);
                }
                //hook the new state up to its toGroup by adding the appropriate transitions

                foreach (Nfa<TAlphabet>.State toState in cachedTransitionGroup) {
                    Nfa<TAlphabet>.State state1 = toState;
                    foreach (Nfa<TAlphabet>.Transition transition in nfa.GetTransitions().Where(x => x.ToState == state1)) {
                        nfa.TransitionFunction[newState][transition.Symbol].Add(toState);
                    }
                }
            }

            //Recreate any incoming transitions to the specified state, excluding self transitions, to each new state
            foreach (Nfa<TAlphabet>.Transition incomingTransition in nfa.GetTransitions().Where(x => x.ToState == state && x.FromState != state)) {
                foreach (Nfa<TAlphabet>.State result in results) {
                    nfa.TransitionFunction[incomingTransition.FromState][incomingTransition.Symbol].Add(result);
                }
            }

            //Recreate self transitions on each new state
            foreach (Nfa<TAlphabet>.Transition selfTransition in nfa.GetTransitions().Where(x => x.ToState == state && x.FromState == state)) {
                foreach (Nfa<TAlphabet>.State result in results) {
                    nfa.TransitionFunction[result][selfTransition.Symbol].Add(result);
                }
            }

            //Remove transitions from state
            nfa.TransitionFunction.TryRemove(state);
            //Remove transitions to state
            foreach (var transitionAndTos in nfa.TransitionFunction.SelectMany(x => x.Value)) {
                transitionAndTos.Value.RemoveWhere(x => x == state);
            }

            nfa.States.Remove(state);
            if (isStart) {
                nfa.StartStates.Remove(state);
            }
            if (isAccept) {
                nfa.AcceptStates.Remove(state);
            }
            return results;
        }

        /// <summary>
        ///     The specified state is replaced by new states which each have transitions coming from only the states as grouped by
        ///     fromGroups.
        ///     Any state, for which a transition exists from that state to the specified state, that is not specified in
        ///     fromGroups will be placed in its own group.
        ///     This is a behavior preserving transformation.
        ///     This function is robust and will throw an InvalidNfaOperationException if the inputs are not sane.
        /// </summary>
        /// <typeparam name="TAlphabet"></typeparam>
        /// <param name="nfa"></param>
        /// <param name="state"></param>
        /// <param name="fromGroups"></param>
        /// <returns></returns>
        public static IEnumerable<Nfa<TAlphabet>.State> SplitTos<TAlphabet>(this Nfa<TAlphabet> nfa,
                                                                            Nfa<TAlphabet>.State state,
                                                                            IEnumerable<IEnumerable<Nfa<TAlphabet>.State>> fromGroups = null) {
            //do some input sanity checks
            if (state == null) {
                throw new ArgumentNullException("state");
            }
            if (fromGroups == null) {
                fromGroups = new List<List<Nfa<TAlphabet>.State>>();
            }
            if (!nfa.States.Contains(state)) {
                throw new InvalidNfaOperationException("The specified State is not in the Nfa");
            }

            //cache some lists
            List<List<Nfa<TAlphabet>.State>> cachedTransitionGroups = fromGroups.Select(x => x.Select(y => y).ToList()).ToList();
            List<Nfa<TAlphabet>.State> allNonSelfFromStates = nfa.GetTransitions().Where(x => x.ToState == state).Select(x => x.FromState).Where(x => x != state).ToList();

            //more input sanity checks
            if (cachedTransitionGroups.Any(cachedTransitionGroup => cachedTransitionGroup.Any(fromState => !allNonSelfFromStates.Contains(fromState)))) {
                throw new InvalidNfaOperationException("One or more of the states specified in fromGroups is not a state that has a transition to the specified State");
            }

            if (cachedTransitionGroups.SelectMany(x => x).Count() != cachedTransitionGroups.SelectMany(x => x).Distinct().Count()) {
                throw new InvalidNfaOperationException("One or more states specified in fromGroups occurs more than once");
            }

            if (cachedTransitionGroups.SelectMany(x => x).Contains(state)) {
                throw new InvalidNfaOperationException("The specified state may not be included in the specified fromGroups");
            }

            //Put each unspecified from-state in its own group
            foreach (Nfa<TAlphabet>.State ungrouped in allNonSelfFromStates.Except(cachedTransitionGroups.SelectMany(x => x))) {
                cachedTransitionGroups.Add(new List<Nfa<TAlphabet>.State>(new[] {ungrouped}));
            }

            bool isStart = nfa.StartStates.Contains(state);
            bool isAccept = nfa.AcceptStates.Contains(state);

            var results = new List<Nfa<TAlphabet>.State>();
            //create the new states
            foreach (var cachedTransitionGroup in cachedTransitionGroups) {
                var newState = new Nfa<TAlphabet>.State();
                results.Add(newState);
                nfa.States.Add(newState);
                if (isStart) {
                    nfa.StartStates.Add(newState);
                }
                if (isAccept) {
                    nfa.AcceptStates.Add(newState);
                }

                //hook the new state up to its group by adding the appropriate transitions
                foreach (Nfa<TAlphabet>.State fromState in cachedTransitionGroup) {
                    Nfa<TAlphabet>.State state1 = fromState;
                    foreach (Nfa<TAlphabet>.Transition transition in nfa.GetTransitions().Where(x => x.FromState == state1)) {
                        nfa.TransitionFunction[fromState][transition.Symbol].Add(newState);
                    }
                }
            }

            //Recreate any outgoing transitions from the specified state, excluding self transitions, from each new state
            foreach (Nfa<TAlphabet>.Transition outgoingTransition in nfa.GetTransitions().Where(x => x.FromState == state && x.ToState != state)) {
                foreach (Nfa<TAlphabet>.State result in results) {
                    nfa.TransitionFunction[result][outgoingTransition.Symbol].Add(outgoingTransition.ToState);
                }
            }
            //Recreate self transitions on each new state
            foreach (Nfa<TAlphabet>.Transition selfTransition in nfa.GetTransitions().Where(x => x.ToState == state && x.FromState == state)) {
                foreach (Nfa<TAlphabet>.State result in results) {
                    nfa.TransitionFunction[result][selfTransition.Symbol].Add(result);
                }
            }

            //Remove transitions from state
            nfa.TransitionFunction.TryRemove(state);
            //Remove transitions to state
            foreach (var transitionAndTos in nfa.TransitionFunction.SelectMany(x => x.Value)) {
                transitionAndTos.Value.RemoveWhere(x => x == state);
            }

            nfa.States.Remove(state);
            if (isStart) {
                nfa.StartStates.Remove(state);
            }
            if (isAccept) {
                nfa.AcceptStates.Remove(state);
            }
            return results;
        }

        /// <summary>
        ///     SplitFroms the specified state and, recursively, all its antecedents which have exactly one antecedent themselves
        /// </summary>
        /// <typeparam name="TAlphabet"></typeparam>
        /// <param name="nfa"></param>
        /// <param name="state"></param>
        public static void PeelBack<TAlphabet>(this Nfa<TAlphabet> nfa, Nfa<TAlphabet>.State state) {
            if (!nfa.States.Contains(state)) {
                throw new InvalidNfaOperationException("The specified State is not in this Nfa");
            }
            IEnumerable<Nfa<TAlphabet>.State> splits = SplitFroms(nfa, state);
            foreach (Nfa<TAlphabet>.State state1 in splits) {
                Restart:
                if (nfa.States.Contains(state1)) {
                    foreach (Nfa<TAlphabet>.Transition transition in nfa.GetTransitions()) {
                        if (transition.ToState == state1 &&
                            nfa.TransitionFunction[transition.FromState].SelectMany(x => x.Value).Distinct().Count() > 1) {
                            if (nfa.GetTransitions().Where(x => x.ToState == transition.FromState).Select(x => x.FromState).Distinct().Count() == 1) {
                                PeelBack(nfa, transition.FromState);
                            }
                            goto Restart;
                        }
                    }
                }
            }
        }

        public static IEnumerable<IEnumerable<Nfa<TAlphabet>.State>> GetMacroCycles<TAlphabet>(this Nfa<TAlphabet> nfa) {
            Debug.Assert(nfa != null, "nfa != null");
            List<HashSet<Nfa<TAlphabet>.State>> results = nfa.GetCycles().Select(x => new HashSet<Nfa<TAlphabet>.State>(x)).ToList();
            for (int i = 0; i < results.Count; i++) {
                for (int j = i + 1; j < results.Count; j++) {
                    if (results[i].Overlaps(results[j])) {
                        results[i].UnionWith(results[j]);
                        j--;
                    }
                }
            }
            return results;
        }

        public static IEnumerable<Nfa<TAlphabet>.Transition> GetTransitionsInToSubgraph<TAlphabet>(
            this Nfa<TAlphabet> nfa, IEnumerable<Nfa<TAlphabet>.State> subgraph) {
            var subGraphHashSet = new HashSet<Nfa<TAlphabet>.State>(subgraph);
            return nfa.GetTransitions().Where(transition => subGraphHashSet.Contains(transition.ToState) && !subGraphHashSet.Contains(transition.FromState));
        }

        public static IEnumerable<Nfa<TAlphabet>.Transition> GetTransitionsOutOfSubgraph<TAlphabet>(
            this Nfa<TAlphabet> nfa, IEnumerable<Nfa<TAlphabet>.State> subgraph) {
            var subGraphHashSet = new HashSet<Nfa<TAlphabet>.State>(subgraph);
            return nfa.GetTransitions().Where(transition => subGraphHashSet.Contains(transition.FromState) && !subGraphHashSet.Contains(transition.ToState));
        }

        public static IEnumerable<Nfa<TAlphabet>.State> GetStartStatesOfSubgraph<TAlphabet>(this Nfa<TAlphabet> nfa,
                                                                                            IEnumerable<Nfa<TAlphabet>.State> subgraph) {
            return
                GetTransitionsInToSubgraph(nfa, subgraph)
                    .Select(x => x.ToState)
                    .Union(nfa.StartStates.Intersect(subgraph));
        }

        public static IEnumerable<Nfa<TAlphabet>.State> GetAcceptStatesOfSubgraph<TAlphabet>(this Nfa<TAlphabet> nfa,
                                                                                             IEnumerable<Nfa<TAlphabet>.State> subgraph) {
            return
                GetTransitionsOutOfSubgraph(nfa, subgraph)
                    .Select(x => x.FromState)
                    .Union(nfa.AcceptStates.Intersect(subgraph));
        }

        /// <summary>
        ///     Returns true if, within the specified subgraph (or the whole nfa if the subgraph is null),
        ///     there is exactly one route from "from" to "to" and each state [from, ..., to) has only
        ///     one subsequent state that it has transitions to (there is no branching).
        ///     This is used to identify subgraphs that can be SplitFroms or SplitTos without creating crisscrossing transitions
        /// </summary>
        /// <typeparam name="TAlphabet"></typeparam>
        /// <param name="nfa"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="subgraph"></param>
        /// <returns></returns>
        private static bool HasUnaryRoute<TAlphabet>(this Nfa<TAlphabet> nfa, Nfa<TAlphabet>.State from, Nfa<TAlphabet>.State to, IEnumerable<Nfa<TAlphabet>.State> subgraph = null) {
            if (subgraph == null) {
                subgraph = new List<Nfa<TAlphabet>.State>();
            }
            IEnumerable<IEnumerable<Nfa<TAlphabet>.State>> routes = nfa.GetRoutes(from, to, new HashSet<Nfa<TAlphabet>.State>(nfa.States.Except(subgraph)));
            if (routes.Count() != 1) {
                return false;
            }
            foreach (Nfa<TAlphabet>.State state in routes.First()) {
                if (state == to) {
                    continue;
                }
                if (nfa.TransitionFunction[state].SelectMany(x => x.Value).Count() != 1) {
                    return false;
                }
            }
            return true;
        }
    }
}