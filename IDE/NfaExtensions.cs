using System;
using System.Collections.Concurrent;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Collections.Generic.More;
using System.Linq;
using System.Linq.More;
using System.Text;
using System.Threading.Tasks;
using parlex;
 
using Nfa = IDE.Nfa<parlex.Product, int>;
using State = IDE.Nfa<parlex.Product, int>.State;
using ProductSpan = parlex.GrammarDocument.ProductSpanSource;

namespace IDE {
    public static class NfaExtensions {
        private static List<HashSet<State>> ComputeMacroCycles(Nfa productNfa, HashSet<State> ignoredStates) {
            ignoredStates = ignoredStates ?? new HashSet<State>();
            var reachability = new AutoDictionary<State, HashSet<State>>(s => new HashSet<State>());
            foreach (var fromStateAndInputSymbols in productNfa.TransitionFunction.Where(keyValuePair => !ignoredStates.Contains(keyValuePair.Key))) {
                var fromState = fromStateAndInputSymbols.Key;
                var inputSymbols = fromStateAndInputSymbols.Value;
                foreach (var inputSymbolAndToStates in inputSymbols) {
                    var toStates = inputSymbolAndToStates.Value;
                    reachability[fromState].UnionWith(toStates.Where(toState => !ignoredStates.Contains(toState)));
                }
            }
            var donePropogating = false;
            while(!donePropogating) {
                donePropogating = true;
                foreach (var fromStateAndReachableStates in reachability) {
                    var reachableStates = fromStateAndReachableStates.Value;
                    var newReachableStates = new HashSet<State>();
                    foreach (var state in reachableStates) {
                        newReachableStates.UnionWith(reachability[state]);
                    }
                    var oldReachableStatesCount = reachableStates.Count;
                    reachableStates.UnionWith(newReachableStates);
                    if (reachableStates.Count > oldReachableStatesCount) {
                        donePropogating = false;
                    }
                }
            }
            var unGroupedStates = new HashSet<State>(productNfa.States);
            var result = new List<HashSet<State>>();
            while(unGroupedStates.Count > 0) {
                var referenceState = unGroupedStates.First();
                var reachables = reachability[referenceState];
                var macroCycle = new HashSet<State>(reachables.Where(s => reachability[s].Contains(referenceState)));
                if (macroCycle.Count > 0) {
                    result.Add(macroCycle);
                }
                unGroupedStates.ExceptWith(macroCycle);
                unGroupedStates.Remove(referenceState);
            }
            return result;
        }

        //private static List<State> getLongestPath(Nfa productNfa, State headState, State tailState, HashSet<State> excludedStates) {
        //    excludedStates.Add(headState);
        //    var longestPath = new List<State>();
        //    foreach (var nextState in productNfa.TransitionFunction[headState].SelectMany(x => x.Value).Where(y => !excludedStates.Contains(y)).Distinct()) {
        //        var subsequencePath = getLongestPath(productNfa, nextState, tailState, excludedStates);
        //        if (subsequencePath.Count > longestPath.Count) {
        //            longestPath = subsequencePath;
        //        }
        //    }
        //    longestPath.Insert(0, headState);
        //    excludedStates.Remove(headState);
        //    return longestPath;
        //}

        private static HashSet<State> SelectMacroCycle(State state, List<HashSet<State>> macroCycles) {
            return macroCycles.FirstOrDefault(x => x.Contains(state));
        }

        private static int GetCurrentSpanIndex(List<ProductSpan> existingSpans) {
            return existingSpans.Select(x => x.StartPosition + x.Length).Concat(new[] { 0 }).Max();
        }

        private static void UnfoldMacroCycle(Nfa productNfa, List<ProductSpan> existingSpans, State currentState, State tailState, HashSet<State> macroCycle, Action followUpAction) {
            var ignoredStates = new HashSet<State>(productNfa.States);
            ignoredStates.ExceptWith(macroCycle);
            ignoredStates.Add(tailState);
            var currentSpanIndex = GetCurrentSpanIndex(existingSpans);
            var macroCycleSpans = new List<ProductSpan>();
            var subSpans = new List<ProductSpan>();
            var cycleCount = 0;
            Process(productNfa, subSpans, currentState, tailState, ignoredStates, () => {
                cycleCount++;
                var startSpanIndex = GetCurrentSpanIndex(existingSpans);
                foreach (var productSpan in subSpans) {
                    productSpan.StartPosition += startSpanIndex;
                    existingSpans.Add(productSpan);
                    macroCycleSpans.Add(productSpan);
                }
                var endSpanIndex = GetCurrentSpanIndex(existingSpans);
                var cycleSpan = new ProductSpan(null, startSpanIndex, endSpanIndex - startSpanIndex);
                existingSpans.Add(cycleSpan);
                macroCycleSpans.Add(cycleSpan);
            }, true);
            if (cycleCount > 1) {
                var nextSpanIndex = GetCurrentSpanIndex(existingSpans);
                var macroCycleSpan = new ProductSpan(null, currentSpanIndex, nextSpanIndex - currentSpanIndex);
                existingSpans.Add(macroCycleSpan);
                macroCycleSpans.Add(macroCycleSpan);
            }
            followUpAction();
            foreach (var productSpan in macroCycleSpans) {
                existingSpans.Remove(productSpan);
            }
        }

        private static void Process(Nfa productNfa, List<ProductSpan> existingSpans, State currentState, State tailState, HashSet<State> ignoredToStates, Action followUpAction, bool forceIteration = false) {
            if (currentState == tailState && !forceIteration) {
                followUpAction();
            } else {
                var macroCycles = ComputeMacroCycles(productNfa, ignoredToStates);
                var ignoredToStatesContainsCurrent = ignoredToStates.Contains(currentState);
                ignoredToStates.Add(currentState);
                var currentSpanIndex = GetCurrentSpanIndex(existingSpans);
                var currentMacroCycle = SelectMacroCycle(currentState, macroCycles);
                var isMacroCycleEntrance = currentMacroCycle != null;
                var branches = productNfa.TransitionFunction[currentState].SelectMany(x => x.Value.Where(s => !ignoredToStates.Contains(s) || s == tailState).Select(y => new KeyValuePair<Product, State>(x.Key, y))).ToList(); //list of key-value pairs of transition product and to state
                var isBranch = branches.Count > 1;
                if (isMacroCycleEntrance && isBranch) {
                    Action<int> followUpLambda = null;
                    followUpLambda = nextBranchIndex => {
                        var nextSpanIndex = GetCurrentSpanIndex(existingSpans);
                        if (nextBranchIndex < branches.Count) {
                            existingSpans.Add(new ProductSpan(branches[nextBranchIndex].Key.Title, nextSpanIndex, 1));
                            UnfoldMacroCycle(productNfa, existingSpans, branches[nextBranchIndex].Value, currentState, currentMacroCycle, () => followUpLambda(nextBranchIndex + 1));
                            existingSpans.RemoveAt(existingSpans.Count - 1);
                        } else {
                            existingSpans.Add(new ProductSpan(null, currentSpanIndex, nextBranchIndex - currentSpanIndex));
                            Process(productNfa, existingSpans, currentState, tailState, ignoredToStates, followUpAction);
                            existingSpans.RemoveAt(existingSpans.Count - 1);
                        }
                    };
                } else if (isMacroCycleEntrance) {
                    UnfoldMacroCycle(productNfa, existingSpans, currentState, currentState, currentMacroCycle, () => Process(productNfa, existingSpans, currentState, tailState, ignoredToStates, followUpAction));
                } else {
                    foreach (var branch in branches) {
                        existingSpans.Add(new ProductSpan(branch.Key.Title, currentSpanIndex, 1));
                        Process(productNfa, existingSpans, branch.Value, tailState, ignoredToStates, followUpAction);
                        existingSpans.RemoveAt(existingSpans.Count - 1);
                    }
                }
                if (!ignoredToStatesContainsCurrent) {
                    ignoredToStates.Remove(currentState);
                }
            }
        }

        public static GrammarDocument.ExemplarSource[] ToExemplarSources(this Nfa productNfa) {
            var resultList = new List<GrammarDocument.ExemplarSource>();
            foreach (var startState in productNfa.StartStates) {
                foreach (var acceptState in productNfa.AcceptStates) {
                    var productSpans = new List<ProductSpan>();
                    Process(productNfa, productSpans, startState, acceptState, new HashSet<State>(), () => {
                        var item = new GrammarDocument.ExemplarSource("");
                        foreach (var productSpanSource in productSpans) {
                            item.Add(productSpanSource);
                        }
                        resultList.Add(item);
                    });
                }
            }
            return resultList.ToArray();
        }
    }
}
