﻿using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Automata;

using GNfa = Automata.Nfa<Parlex.Grammar.ISymbol>;
using GState = Automata.Nfa<Parlex.Grammar.ISymbol>.State;
using Nfa = Automata.Nfa<Parlex.BehaviorTree.Node>;
using State = Automata.Nfa<Parlex.BehaviorTree.Node>.State;
using Transition = Automata.Nfa<Parlex.BehaviorTree.Node>.Transition;

namespace Parlex
{
    /// <summary>
    /// Convert an NFA into a behavioral tree.
    /// The algorithm is similar to converting an NFA to a regex,
    /// except instead of transitions becoming regular expressions
    /// they become trees.
    /// </summary>
    public class BehaviorTree
    {
        public abstract class Node {
            internal abstract void Optimize();
            internal abstract GNfa ToNfa();
        }

        public class Sequence : Node {
            public readonly List<Node> Children = new List<Node>();

            internal void expandNestedSequences() {
                var oldChildren = Children.ToArray();
                Children.Clear();
                foreach (var oldChild in oldChildren) {
                    oldChild.Optimize();
                    if (oldChild is Null) {
                        continue;
                    }
                    if (oldChild is Sequence) {
                        foreach (var childChild in (oldChild as Sequence).Children) {
                            Children.Add(childChild);
                        }
                    }
                    else {
                        Children.Add(oldChild);
                    }
                }
            }

            internal override void Optimize() {
                expandNestedSequences();
            }

            internal override GNfa ToNfa() {
                var result = new Nfa<Grammar.ISymbol>();
                var state = new Nfa<Grammar.ISymbol>.State();
                result.StartStates.Add(state);
                result.States.Add(state);
                result.AcceptStates.Add(state);
                foreach (var child in Children) {
                    var childNfa = child.ToNfa();
                    result.Insert(result.AcceptStates.First(), childNfa);
                }
                result = result.Minimized();
                return result;
            }
        }

        public class Repetition : Node {
            public Node Child;
            internal override void Optimize() {                
            }

            internal override GNfa ToNfa() {
                var result = Child.ToNfa();
                foreach (var transition in result.GetTransitions()) {
                    if (result.AcceptStates.Contains(transition.ToState))
                    {
                        foreach (var startState in result.StartStates) {
                            result.TransitionFunction[transition.FromState][transition.Symbol].Add(startState);
                        }
                    }
                    if (result.AcceptStates.Contains(transition.FromState)) {
                        foreach (var startState in result.StartStates) {
                            result.TransitionFunction[startState][transition.Symbol].Add(transition.ToState);
                        }
                    }
                }
                foreach (var acceptState in result.AcceptStates) {
                    result.States.Remove(acceptState);
                    result.AcceptStates.Remove(acceptState);
                    result.TransitionFunction.TryRemove(acceptState);
                    foreach (var transition in result.GetTransitions().Where(x => x.ToState == acceptState)) {
                        result.TransitionFunction[transition.FromState][transition.Symbol].Remove(acceptState);
                        if (result.TransitionFunction[transition.FromState][transition.Symbol].Count == 0) {
                            result.TransitionFunction[transition.FromState].TryRemove(transition.Symbol);
                        }
                    }
                }
                foreach (var startState in result.StartStates) {
                    result.AcceptStates.Add(startState);
                }
                result = result.Minimized();
                return result;
            }
        }

        public class Optional : Node {
            public Node Child;
            internal override void Optimize() {
            }

            internal override GNfa ToNfa() {
                var result = new GNfa();
                foreach (var startState in result.StartStates) {
                    result.AcceptStates.Add(startState);
                }
                result = result.Minimized();
                return result;
            }
        }

        public class Choice : Node {
            public readonly List<Node> Children = new List<Node>();

            void expandNestedChoices() {
                var oldChildren = Children.ToArray();
                Children.Clear();
                foreach (var oldChild in oldChildren) {
                    oldChild.Optimize();
                    if (oldChild is Null) {
                        continue;
                    }
                    if (oldChild is Choice) {
                        foreach (var childChild in (oldChild as Choice).Children) {
                            Children.Add(childChild);
                        }
                    } else {
                        Children.Add(oldChild);
                    }
                }
            }

            internal override void Optimize() {
                expandNestedChoices();
                //todo: convert some choices to options
            }

            internal override GNfa ToNfa() {
                return GNfa.Union(Children.Select(x => x.ToNfa())).Minimized();
            }
        }

        public class Leaf : Node {
            public Grammar.ISymbol Symbol;
            public Leaf(Grammar.ISymbol symbol) {
                Symbol = symbol;
            }

            internal override void Optimize() {                
            }

            internal override GNfa ToNfa() {
                var result = new GNfa();
                var startState = new GNfa.State();
                var acceptState = new GNfa.State();
                result.StartStates.Add(startState);
                result.States.Add(startState);
                result.AcceptStates.Add(acceptState);
                result.States.Add(acceptState);
                result.TransitionFunction[startState][Symbol].Add(acceptState);
                return result;
            }
        }

        private class Null : Node {
            internal override void Optimize() {
            }

            internal override GNfa ToNfa() {
                throw new Exception("A Null node cannot be converted to an Nfa.");
            }
        }

        static Nfa Preprocess(GNfa gnfa) {
            var nfa = new Nfa();
            var stateMap = new AutoDictionary<GState, State>(x => new State());
            foreach (var state in gnfa.States) {
                nfa.States.Add(stateMap[state]);
            }

            var startState = new State();
            nfa.States.Add(startState);
            nfa.StartStates.Add(startState);
            foreach (var state in gnfa.StartStates) {
                nfa.TransitionFunction[startState][new Null()].Add(stateMap[state]);
            }

            var acceptState = new State();
            nfa.States.Add(acceptState);
            nfa.AcceptStates.Add(acceptState);
            foreach (var state in gnfa.AcceptStates) {
                nfa.TransitionFunction[stateMap[state]][new Null()].Add(acceptState);
            }

            foreach (var transition in gnfa.GetTransitions()) {
                nfa.TransitionFunction[stateMap[transition.FromState]][new Leaf(transition.Symbol)].Add(
                    stateMap[transition.ToState]);
            }

            MergeAdjecentTransitions(nfa);
            return nfa;
        }

        static void RipState(Nfa nfa, State state) {
            var selfCycles = nfa.TransitionFunction[state].Where(kvp => kvp.Value.Contains(state)).Select(kvp => kvp.Key).Distinct().ToArray();
            Repetition selfCycleNode = null;
            System.Diagnostics.Debug.Assert(selfCycles.Length < 2); //should have a max of one, because adjacent transitions should have been merged into an option already
            if (selfCycles.Length > 0) { 
                selfCycleNode = new Repetition {Child = selfCycles.First()};
            }
            var predecessors =
                nfa.TransitionFunction.Where(kvp => kvp.Value.SelectMany(kvp2 => kvp2.Value).Contains(state)).Select(kvp => kvp.Key).Distinct().Where(x => x != state).ToArray();
            var sucessors = nfa.TransitionFunction[state].SelectMany(kvp => kvp.Value).Distinct().Where(x => x != state).ToArray();

            foreach (var predecessor in predecessors) {
                foreach (var sucessor in sucessors) {
                    var newTransition = new Sequence();
                    newTransition.Children.Add(nfa.TransitionFunction[predecessor].Where(kvp => kvp.Value.Contains(state)).Select(kvp => kvp.Key).First());
                    if (selfCycleNode != null) {
                        newTransition.Children.Add(selfCycleNode);
                    }
                    newTransition.Children.Add(nfa.TransitionFunction[state].Where(kvp => kvp.Value.Contains(sucessor)).Select(kvp => kvp.Key).First());
                    nfa.TransitionFunction[predecessor][newTransition].Add(sucessor);
                }
            }

            var incomingTransitions =
                predecessors.SelectMany(
                    predecessor =>
                        nfa.TransitionFunction[predecessor].Where(kvp => kvp.Value.Contains(state))
                            .Select(kvp => new Transition(predecessor, kvp.Key, state))).ToArray();

            foreach (var incomingTransition in incomingTransitions) {
                nfa.TransitionFunction[incomingTransition.FromState][incomingTransition.Symbol].Remove(
                    incomingTransition.ToState);
                if (!nfa.TransitionFunction[incomingTransition.FromState][incomingTransition.Symbol].Any()) {
                    nfa.TransitionFunction[incomingTransition.FromState].TryRemove(incomingTransition.Symbol);
                }
            }

            nfa.TransitionFunction.TryRemove(state);

            MergeAdjecentTransitions(nfa);
        }

        static void MergeAdjecentTransitions(Nfa nfa) {
            foreach (var fromState in nfa.States) {
                foreach (var toState in nfa.States) {
                    var transitionSymbols =
                        nfa.TransitionFunction[fromState].Where(kvp => kvp.Value.Contains(toState))
                            .Select(kvp => kvp.Key)
                            .ToArray();
                    if (transitionSymbols.Length > 1) {
                        Choice newChoice = new Choice();
                        newChoice.Children.AddRange(transitionSymbols);
                        foreach (var transitionSymbol in transitionSymbols) {
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

        static bool CollapseRedundantNodes(ref Node root) {
            if (root is Sequence) {
                var t = root as Sequence;
                if (t.Children.Count == 1) {
                    root = t.Children[0];
                    CollapseRedundantNodes(ref root);
                    return true;
                } else {
                    var result = false;
                    for (var i = 0; i < t.Children.Count; i++) {
                        var c = t.Children[i];
                        result |= CollapseRedundantNodes(ref c);
                        t.Children[i] = c;
                    }
                    return result;
                }
            } else if (root is Choice) {
                var t = root as Choice;
                if (t.Children.Count == 1) {
                    root = t.Children[0];
                    CollapseRedundantNodes(ref root);
                    return true;
                } else {
                    var result = false;
                    for (var i = 0; i < t.Children.Count; i++) {
                        var c = t.Children[i];
                        result |= CollapseRedundantNodes(ref c);
                        t.Children[i] = c;
                    }
                    return result;
                }
            } else if (root is Repetition) {
                var t = root as Repetition;
                return CollapseRedundantNodes(ref t.Child);
            } else if (root is Optional) {
                var t = root as Optional;
                return CollapseRedundantNodes(ref t.Child);
            } else {
                return false;
            }

        }

        static Node Treeify(GNfa gnfa) {
            Nfa nfa = Preprocess(gnfa);
            var ripableStates = nfa.States.Where(x => !nfa.StartStates.Contains(x) && !nfa.AcceptStates.Contains(x));
            foreach (var state in ripableStates) {
                RipState(nfa, state);
            }
            var possibleResults = nfa.TransitionFunction[nfa.StartStates.First()];
            if (possibleResults.Count() == 0) return new Null();
            Node result = possibleResults.First().Key;
            result.Optimize();
            while (CollapseRedundantNodes(ref result)) {
                result.Optimize();
            }
            return result;
        }

        public Node Root;

        public BehaviorTree(GNfa gnfa) {
            Root = Treeify(gnfa.Minimized());
        }

        public BehaviorTree() {}

        public GNfa ToNfa() {
            return Root.ToNfa();
        }
    }
}
