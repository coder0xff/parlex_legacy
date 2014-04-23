using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using parlex;

namespace IDE {
    public static class NfaSequenceExtensions {
        public static Nfa<Product, int> ToNfa(this CompiledGrammar.NfaSequence sequence) {
            var positionToState = new AutoDictionary<int /*position*/, Nfa<Product, int>.State>(position => new Nfa<Product, int>.State(position));
            var result = new Nfa<Product, int>();
            var skipAheadTable = new AutoDictionary<int, HashSet<int>>(i => new HashSet<int>());
            for (int position = 0; position < sequence.RelationBranches.Length; position++) {
                foreach (var productReference in sequence.RelationBranches[position]) {
                    var fromState = positionToState[position];
                    Nfa<Product, int>.State toState = productReference.IsRepetitious ? fromState : positionToState[productReference.ExitSequenceCounter - sequence.SpanStart];
                    if (productReference.IsRepetitious) {
                        skipAheadTable[position].Add(productReference.ExitSequenceCounter - sequence.SpanStart);
                    }
                    var transitionProduct = productReference.Product;
                    if (transitionProduct.Title.StartsWith("anon-")) { //expand anonymous products in line
                        var subNfa = transitionProduct.ToNfa();
                        if (subNfa.StartStates.Count > 1) { // should typically not, but this algorithm only works with one start state on the subNfa, so force it if necessary
                            subNfa = subNfa.MinimizedDfa();
                        }
                        var newStates = subNfa.States.Except(subNfa.StartStates).Except(subNfa.AcceptStates);
                        result.States.UnionWith(newStates);
                        foreach (var subFromStateAndInputSymbols in subNfa.TransitionFunction) {
                            var subFromState = subFromStateAndInputSymbols.Key;
                            subFromState = subNfa.StartStates.Contains(subFromState) ? fromState : subNfa.AcceptStates.Contains(subFromState) ? toState : subFromState;
                            var subInputSymbols = subFromStateAndInputSymbols.Value;
                            foreach (var subInputSymbolAndToStates in subInputSymbols) {
                                var subInputSymbol = subInputSymbolAndToStates.Key;
                                var subToStates = subInputSymbolAndToStates.Value;
                                foreach (var subToState in subToStates) {
                                    var usedSubToState = subNfa.AcceptStates.Contains(subToState) ? toState : subNfa.StartStates.Contains(subToState) ? fromState : subToState;
                                    result.TransitionFunction[subFromState][subInputSymbol].Add(usedSubToState);
                                }
                            }
                        }
                    } else {
                        result.TransitionFunction[fromState][transitionProduct].Add(toState);
                    }
                }
            }

            foreach (var skipAheadPosition in skipAheadTable.Select(keyValuePair => keyValuePair.Key).OrderByDescending(i => i)) {
                var fromState = positionToState[skipAheadPosition];
                var skipReferences = skipAheadTable[skipAheadPosition];
                foreach (var skipReference in skipReferences) {
                    if (skipReference == sequence.RelationBranches.Length) {
                        result.AcceptStates.Add(fromState);
                    } else {
                        foreach (var inputSymbolAndToStates in result.TransitionFunction[positionToState[skipReference]]) {
                            result.TransitionFunction[fromState][inputSymbolAndToStates.Key].UnionWith(inputSymbolAndToStates.Value);
                        }
                    }
                }
            }

            result.StartStates.Add(positionToState[0]);
            result.AcceptStates.Add(positionToState[sequence.RelationBranches.Length]);
            result.States.UnionWith(positionToState.Values);

            return result;
        }
    }
}
