using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GNfa = Automata.Nfa<Parlex.Recognizer>;
using GState = Automata.Nfa<Parlex.Recognizer>.State;
using Nfa = Automata.Nfa<Parlex.BehaviorNode>;
using State = Automata.Nfa<Parlex.BehaviorNode>.State;
using Transition = Automata.Nfa<Parlex.BehaviorNode>.Transition;

namespace Parlex {
    /// <summary>
    ///     Convert an NFA into a behavioral tree.
    ///     The algorithm is similar to converting an NFA to a regex,
    ///     except instead of transitions becoming regular expressions
    ///     they become trees.
    /// </summary>
    public class BehaviorTree {
        public BehaviorNode Root { get; set; }

        public BehaviorTree(GNfa nfa) {
            if (nfa == null) {
                throw new ArgumentNullException("nfa");
            }
            Root = Treeify(nfa.Minimized());
        }

        public BehaviorTree() {}

        public GNfa ToNfa() {
            return Root.ToNfa().Minimized();
        }
        private static Nfa Preprocess(GNfa gnfa) {
            var nfa = new Nfa();
            var stateMap = new AutoDictionary<GState, State>(x => new State());
            foreach (GState state in gnfa.States) {
                nfa.States.Add(stateMap[state]);
            }

            var startState = new State();
            nfa.States.Add(startState);
            nfa.StartStates.Add(startState);
            foreach (GState state in gnfa.StartStates) {
                nfa.TransitionFunction[startState][new NullBehavior()].Add(stateMap[state]);
            }

            var acceptState = new State();
            nfa.States.Add(acceptState);
            nfa.AcceptStates.Add(acceptState);
            foreach (GState state in gnfa.AcceptStates) {
                nfa.TransitionFunction[stateMap[state]][new NullBehavior()].Add(acceptState);
            }

            foreach (GNfa.Transition transition in gnfa.GetTransitions()) {
                nfa.TransitionFunction[stateMap[transition.FromState]][new BehaviorLeaf(transition.Symbol)].Add(
                    stateMap[transition.ToState]);
            }

            MergeAdjecentTransitions(nfa);
            return nfa;
        }

        private static State[] ComputePredecessors(Nfa nfa, State state) { //states that precede state
            State[] predecessors =
                nfa.TransitionFunction.Where(kvp => kvp.Value.SelectMany(kvp2 => kvp2.Value).Contains(state)).Select(kvp => kvp.Key).Distinct().Where(x => x != state).ToArray();
            return predecessors;
        }

        private static State[] ComputeSucessors(Nfa nfa, State state) { //state that succeed state
            State[] sucessors = nfa.TransitionFunction[state].SelectMany(kvp => kvp.Value).Distinct().Where(x => x != state).ToArray();
            return sucessors;
        }

        private static void BuildRippedTransitions(Nfa nfa, State state, State[] predecessors, State[] sucessors, RepetitionBehavior selfCycleNode) {
            foreach (State predecessor in predecessors) {
                foreach (State sucessor in sucessors) {
                    var newTransition = new SequenceBehavior();
                    newTransition.Children.Add(nfa.TransitionFunction[predecessor].Where(kvp => kvp.Value.Contains(state)).Select(kvp => kvp.Key).First());
                    if (selfCycleNode != null) {
                        newTransition.Children.Add(selfCycleNode);
                    }
                    newTransition.Children.Add(nfa.TransitionFunction[state].Where(kvp => kvp.Value.Contains(sucessor)).Select(kvp => kvp.Key).First());
                    nfa.TransitionFunction[predecessor][newTransition].Add(sucessor);
                }
            }

            Transition[] incomingTransitions =
                predecessors.SelectMany(
                    predecessor =>
                        nfa.TransitionFunction[predecessor].Where(kvp => kvp.Value.Contains(state))
                            .Select(kvp => new Transition(predecessor, kvp.Key, state))).ToArray();

            foreach (Transition incomingTransition in incomingTransitions) {
                nfa.TransitionFunction[incomingTransition.FromState][incomingTransition.Symbol].Remove(
                    incomingTransition.ToState);
                if (!nfa.TransitionFunction[incomingTransition.FromState][incomingTransition.Symbol].Any()) {
                    nfa.TransitionFunction[incomingTransition.FromState].TryRemove(incomingTransition.Symbol);
                }
            }
        }

        private static void MergeAdjecentTransitions(Nfa nfa) {
            foreach (State fromState in nfa.States) {
                foreach (State toState in nfa.States) {
                    BehaviorNode[] transitionSymbols =
                        nfa.TransitionFunction[fromState].Where(kvp => kvp.Value.Contains(toState))
                            .Select(kvp => kvp.Key)
                            .ToArray();
                    if (transitionSymbols.Length > 1) {
                        var newChoice = new ChoiceBehavior();
                        newChoice.Children.AddRange(transitionSymbols);
                        foreach (BehaviorNode transitionSymbol in transitionSymbols) {
                            nfa.TransitionFunction[fromState][transitionSymbol].Remove(toState);
                            if (!nfa.TransitionFunction[fromState][transitionSymbol].Any()) {
                                nfa.TransitionFunction[fromState].TryRemove(transitionSymbol);
                            }
                        }
                        nfa.TransitionFunction[fromState][newChoice].Add(toState);
                    }
                }
            }
        }

        /// <summary>
        /// Remove a state from an nfa, and merge the connecting transitions
        /// </summary>
        /// <param name="nfa"></param>
        /// <param name="state"></param>
        private static void RipState(Nfa nfa, State state) {
            BehaviorNode[] selfCycles = nfa.TransitionFunction[state].Where(kvp => kvp.Value.Contains(state)).Select(kvp => kvp.Key).Distinct().ToArray();
            RepetitionBehavior selfCycleNode = null;
            Debug.Assert(selfCycles.Length < 2); //should have a max of one, because adjacent transitions should have been merged into an option already
            if (selfCycles.Length > 0) {
                selfCycleNode = new RepetitionBehavior {Child = selfCycles.First()};
            }
            var predecessors = ComputePredecessors(nfa, state);
            var sucessors = ComputeSucessors(nfa, state);

            BuildRippedTransitions(nfa, state, predecessors, sucessors, selfCycleNode);

            nfa.TransitionFunction.TryRemove(state);

            MergeAdjecentTransitions(nfa);
        }

        private static bool CollapseRedundantNodes(ref BehaviorNode root) {
            if (root is SequenceBehavior) {
                var t = root as SequenceBehavior;
                if (t.Children.Count == 1) {
                    root = t.Children[0];
                    CollapseRedundantNodes(ref root);
                    return true;
                }
                bool result = false;
                for (int i = 0; i < t.Children.Count; i++) {
                    BehaviorNode c = t.Children[i];
                    result |= CollapseRedundantNodes(ref c);
                    t.Children[i] = c;
                }
                return result;
            }
            if (root is ChoiceBehavior) {
                var t = root as ChoiceBehavior;
                if (t.Children.Count == 1) {
                    root = t.Children[0];
                    CollapseRedundantNodes(ref root);
                    return true;
                }
                bool result = false;
                for (int i = 0; i < t.Children.Count; i++) {
                    BehaviorNode c = t.Children[i];
                    result |= CollapseRedundantNodes(ref c);
                    t.Children[i] = c;
                }
                return result;
            }
            if (root is RepetitionBehavior) {
                var t = root as RepetitionBehavior;
                return CollapseRedundantNodes(ref t.Child);
            }
            if (root is Optional) {
                var t = root as Optional;
                return CollapseRedundantNodes(ref t.Child);
            }
            return false;
        }

        private static BehaviorNode Treeify(GNfa gnfa) {
            Nfa nfa = Preprocess(gnfa);
            IEnumerable<State> ripableStates = nfa.States.Where(x => !nfa.StartStates.Contains(x) && !nfa.AcceptStates.Contains(x));
            foreach (State state in ripableStates) {
                RipState(nfa, state);
            }
            JaggedAutoDictionary<BehaviorNode, HashSet<State>> possibleResults = nfa.TransitionFunction[nfa.StartStates.First()];
            if (possibleResults.Count() == 0) {
                return new NullBehavior();
            }
            BehaviorNode result = possibleResults.First().Key;
            result = result.Optimize();
            while (CollapseRedundantNodes(ref result)) {
                result = result.Optimize();
            }
            return result;
        }

    }

    public abstract class BehaviorNode {
        internal abstract BehaviorNode Optimize();
        internal abstract GNfa ToNfa();
    }

    public class BehaviorLeaf : BehaviorNode {
        public Recognizer Recognizer;

        public BehaviorLeaf(Recognizer recognizer) {
            Recognizer = recognizer;
        }

        internal override BehaviorNode Optimize() {
            return this;
        }

        internal override GNfa ToNfa() {
            var result = new GNfa();
            var startState = new GState();
            var acceptState = new GState();
            result.StartStates.Add(startState);
            result.States.Add(startState);
            result.AcceptStates.Add(acceptState);
            result.States.Add(acceptState);
            result.TransitionFunction[startState][Recognizer].Add(acceptState);
            return result;
        }
    }

    internal class NullBehavior : BehaviorNode {
        internal override BehaviorNode Optimize() {
            return this;
        }

        internal override GNfa ToNfa() {
            throw new Exception("A Null node cannot be converted to an Nfa.");
        }
    }

    public class Optional : BehaviorNode {
        public BehaviorNode Child;

        internal override BehaviorNode Optimize() {
            Child = Child.Optimize();
            return this;
        }

        internal override GNfa ToNfa() {
            if (Child == null) return new GNfa();
            var result = Child.ToNfa();
            foreach (GState startState in result.StartStates) {
                result.AcceptStates.Add(startState);
            }
            return result;
        }
    }

    public class RepetitionBehavior : BehaviorNode {
        public BehaviorNode Child;
        internal override BehaviorNode Optimize() {
            Child.Optimize();
            return this;
        }

        internal override GNfa ToNfa() {
            if (Child == null) return new GNfa();
            GNfa result = Child.ToNfa();
            foreach (GNfa.Transition transition in result.GetTransitions()) {
                if (result.AcceptStates.Contains(transition.ToState)) {
                    foreach (GState startState in result.StartStates) {
                        result.TransitionFunction[transition.FromState][transition.Symbol].Add(startState);
                    }
                }
                if (result.AcceptStates.Contains(transition.FromState)) {
                    foreach (GState startState in result.StartStates) {
                        result.TransitionFunction[startState][transition.Symbol].Add(transition.ToState);
                    }
                }
            }
            foreach (GState acceptState in result.AcceptStates.ToArray()) {
                result.States.Remove(acceptState);
                result.AcceptStates.Remove(acceptState);
                result.TransitionFunction.TryRemove(acceptState);
                foreach (GNfa.Transition transition in result.GetTransitions().Where(x => x.ToState == acceptState)) {
                    result.TransitionFunction[transition.FromState][transition.Symbol].Remove(acceptState);
                    if (result.TransitionFunction[transition.FromState][transition.Symbol].Count == 0) {
                        result.TransitionFunction[transition.FromState].TryRemove(transition.Symbol);
                    }
                }
            }
            foreach (GState startState in result.StartStates) {
                result.AcceptStates.Add(startState);
            }
            return result;
        }
    }

    public class SequenceBehavior : BehaviorNode {
        public readonly List<BehaviorNode> Children = new List<BehaviorNode>();

        private void FlattenNestedSequences() {
            BehaviorNode[] oldChildren = Children.ToArray();
            Children.Clear();
            foreach (BehaviorNode oldChild in oldChildren) {
                if (oldChild is SequenceBehavior) {
                    foreach (BehaviorNode childChild in (oldChild as SequenceBehavior).Children) {
                        Children.Add(childChild);
                    }
                } else {
                    Children.Add(oldChild);
                }
            }
        }

        internal override BehaviorNode Optimize() {
            BehaviorNode[] oldChildren = Children.ToArray();
            Children.Clear();
            foreach (var child in oldChildren) {
                var newChild = child.Optimize();
                if (!(newChild is NullBehavior)) {
                    Children.Add(newChild);
                }
            }
            FlattenNestedSequences();
            if (Children.Count == 1) {
                return Children[0];
            }
            return this;
        }

        internal override GNfa ToNfa() {
            var result = new GNfa();
            var state = new GState();
            result.StartStates.Add(state);
            result.States.Add(state);
            result.AcceptStates.Add(state);
            foreach (BehaviorNode child in Children) {
                GNfa childNfa = child.ToNfa();
                result.Append(childNfa);
            }
            return result;
        }
    }

    public class ChoiceBehavior : BehaviorNode {
        public readonly List<BehaviorNode> Children = new List<BehaviorNode>();

        private void FlattenNestedChoices() {
            BehaviorNode[] oldChildren = Children.ToArray();
            Children.Clear();
            foreach (BehaviorNode oldChild in oldChildren) {
                if (oldChild is ChoiceBehavior) {
                    foreach (BehaviorNode childChild in (oldChild as ChoiceBehavior).Children) {
                        Children.Add(childChild);
                    }
                } else {
                    Children.Add(oldChild);
                }
            }
        }

        internal override BehaviorNode Optimize() {
            BehaviorNode[] oldChildren = Children.ToArray();
            Children.Clear();
            bool isOptional = false;
            foreach (var child in oldChildren) {
                var newChild = child.Optimize();
                if (!(newChild is NullBehavior)) {
                    Children.Add(newChild);
                } else {
                    isOptional = true;
                }
            }
            FlattenNestedChoices();
            BehaviorNode result;
            if (Children.Count == 1) {
                result = Children[0];
            } else {
                result = this;
            }
            if (isOptional) {
                result = new Optional { Child = result };
            }
            return result;
            //todo: convert some choices to options
        }

        internal override GNfa ToNfa() {
            return GNfa.Union(Children.Select(x => x.ToNfa()));
        }
    }
}