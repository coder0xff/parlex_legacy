using System;
using System.Collections.Concurrent;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Collections.Generic.More;
using System.Linq;
using System.Linq.More;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
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

        public static List<State> GetLongestPath(this Nfa productNfa, State headState, State tailState, HashSet<State> excludedStates) {
            excludedStates.Add(headState);
            var longestPath = new List<State>();
            foreach (var nextState in productNfa.TransitionFunction[headState].SelectMany(x => x.Value).Where(y => !excludedStates.Contains(y)).Distinct()) {
                var subsequencePath = GetLongestPath(productNfa, nextState, tailState, excludedStates);
                if (subsequencePath.Count > longestPath.Count) {
                    longestPath = subsequencePath;
                }
            }
            longestPath.Insert(0, headState);
            excludedStates.Remove(headState);
            return longestPath;
        }

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
                if (subSpans.Count > 1) {
                    foreach (var productSpan in subSpans) {
                        productSpan.StartPosition += startSpanIndex;
                        existingSpans.Add(productSpan);
                        macroCycleSpans.Add(productSpan);
                    }
                    var endSpanIndex = GetCurrentSpanIndex(existingSpans);
                    var cycleSpan = new ProductSpan("anon-" + Guid.NewGuid() + "*", startSpanIndex, endSpanIndex - startSpanIndex);
                    existingSpans.Add(cycleSpan);
                    macroCycleSpans.Add(cycleSpan);
                } else {
                    var productSpan = subSpans[0];
                    productSpan.StartPosition += startSpanIndex;
                    if (!productSpan.Name.EndsWith("*")) productSpan.Name += "*";
                    existingSpans.Add(productSpan);
                    macroCycleSpans.Add(productSpan);
                }
            }, true);
            if (cycleCount > 1) {
                var nextSpanIndex = GetCurrentSpanIndex(existingSpans);
                var macroCycleSpan = new ProductSpan("anon-" + Guid.NewGuid() + "*", currentSpanIndex, nextSpanIndex - currentSpanIndex);
                existingSpans.Add(macroCycleSpan);
                macroCycleSpans.Add(macroCycleSpan);
            }
            followUpAction();
            foreach (var productSpan in macroCycleSpans) {
                existingSpans.Remove(productSpan);
            }
        }

        private static void Process(Nfa productNfa, List<ProductSpan> existingSpans, State currentState, State tailState, HashSet<State> ignoredToStates, Action followUpAction, bool forceIteration = false) {
            bool isTailState = currentState == tailState;
            var macroCycles = ComputeMacroCycles(productNfa, ignoredToStates);
            var ignoredToStatesContainsCurrent = ignoredToStates.Contains(currentState);
            ignoredToStates.Add(currentState);
            var currentSpanIndex = GetCurrentSpanIndex(existingSpans);
            var currentMacroCycle = SelectMacroCycle(currentState, macroCycles);
            var isMacroCycleEntrance = currentMacroCycle != null;
            var branches = productNfa.TransitionFunction[currentState].SelectMany(x => x.Value.Where(s => !ignoredToStates.Contains(s) || s == tailState).Select(y => new KeyValuePair<Product, State>(x.Key, y))).ToList(); //list of key-value pairs of transition product and to state
            if (isMacroCycleEntrance) {
                if (isTailState && !forceIteration) {
                    UnfoldMacroCycle(productNfa, existingSpans, currentState, currentState, currentMacroCycle, followUpAction);
                } else {
                    UnfoldMacroCycle(productNfa, existingSpans, currentState, currentState, currentMacroCycle, () => Process(productNfa, existingSpans, currentState, tailState, ignoredToStates, followUpAction));                    
                }
            } else {
                if (isTailState && !forceIteration) {
                    followUpAction();
                } else {
                    foreach (var branch in branches) {
                        existingSpans.Add(new ProductSpan(branch.Key.Title, currentSpanIndex, 1));
                        Process(productNfa, existingSpans, branch.Value, tailState, ignoredToStates, followUpAction);
                        existingSpans.RemoveAt(existingSpans.Count - 1);
                    }
                }
            }
            if (!ignoredToStatesContainsCurrent) {
                ignoredToStates.Remove(currentState);
            }
        }

        public static GrammarDocument ToGrammarDocument(this Nfa productNfa, String name, Dictionary<String, Product> products) {
            var result = new GrammarDocument();
            foreach (var startState in productNfa.StartStates) {
                foreach (var acceptState in productNfa.AcceptStates) {
                    var productSpans = new List<ProductSpan>();
                    Process(productNfa, productSpans, startState, acceptState, new HashSet<State>(), () => {
                        if (productSpans.Count == 1) {
                            result.IsASources.Add(new GrammarDocument.IsA(productSpans[0].Name, name));
                        } else {
                            var item = new GrammarDocument.ExemplarSource("");
                            foreach (var productSpanSource in productSpans) {
                                item.Add(productSpanSource);
                            }
                            var length = GetCurrentSpanIndex(productSpans);
                            item.Add(new ProductSpan(name, 0, length));
                            result.ExemplarSources.Add(item);
                        }
                    });
                }
            }
            foreach (var exemplar in result.ExemplarSources) {
                exemplar.TryExemplify(products);
            }
            return result;
        }

        public static void SaveToGraphMLFile(this Nfa productNfa, String path) {
            const string header = 
@"<?xml version=""1.0"" encoding=""utf-8""?>
<graphml xmlns=""http://graphml.graphdrawing.org/xmlns"">
    <graph id=""G"" edgedefault=""directed"">
";

            var stateCount = 0;
            productNfa = productNfa.Reassign(x => Interlocked.Increment(ref stateCount)); //Make sure each has a unique index
            var b = new StringBuilder(header);
            foreach (var state in productNfa.States) {
                b.Append("<node id=\"");
                b.Append(state.Value);
                b.AppendLine("\" />");
            }
            foreach (var fromStateAndInputSymbolsAndToStates in productNfa.TransitionFunction) {
                var fromState = fromStateAndInputSymbolsAndToStates.Key;
                var inputSymbolsAndToStates = fromStateAndInputSymbolsAndToStates.Value;
                foreach (var inputSymbolAndToStates in inputSymbolsAndToStates) {
                    var inputSymbol = inputSymbolAndToStates.Key;
                    var toStates = inputSymbolAndToStates.Value;
                    foreach (var toState in toStates) {
                        b.Append("<edge id=\"");
                        b.Append(inputSymbol.Title);
                        b.Append("\" source=\"");
                        b.Append(fromState.Value);
                        b.Append("\" target=\"");
                        b.Append(toState.Value);
                        b.AppendLine("\" />");
                    }
                }
            }

            const string footer = 
@"    </graph>
</graphml>";

            b.Append(footer);

            System.IO.File.WriteAllText(path, b.ToString());
        }

        struct LayerAssignment {
            public readonly State State;
            public readonly int Layer;
            public readonly ReadOnlyHashSet<State> Predecessors;

            public LayerAssignment(State state, int layer, ReadOnlyHashSet<State> predecessors) : this() {
                State = state;
                Layer = layer;
                Predecessors = predecessors;
            }

            public bool Equals(LayerAssignment other) {
                return Equals(State, other.State) && Layer == other.Layer && Equals(Predecessors, other.Predecessors);
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) {
                    return false;
                }
                return obj is LayerAssignment && Equals((LayerAssignment) obj);
            }

            public override int GetHashCode() {
                unchecked {
                    int hashCode = (State != null ? State.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ Layer;
                    hashCode = (hashCode * 397) ^ Predecessors.GetHashCode();
                    return hashCode;
                }
            }

            public static bool operator ==(LayerAssignment left, LayerAssignment right) {
                return left.Equals(right);
            }

            public static bool operator !=(LayerAssignment left, LayerAssignment right) {
                return !left.Equals(right);
            }
        }

        public static AutoDictionary<State, int> GetLayerAssignments(this Nfa productNfa) {
            var processor = new DistinctRecursiveAlgorithmProcessor<LayerAssignment>();
            var results = new AutoDictionary<State, int>(state => 0);
            foreach (var startState in productNfa.StartStates) {
                processor.Add(new LayerAssignment(startState, 0, new ReadOnlyHashSet<State>(productNfa.StartStates)));
                results[startState] = 0;
            }
            bool firstLoop = true;
            var unreachedStates = productNfa.States.Except(results.Keys).ToList();
            while(unreachedStates.Count > 0) {
                if (!firstLoop) {
                    var state = unreachedStates.OrderBy(s => unreachedStates.Count(s1 => productNfa.GetRoutes(s, s1, new HashSet<State>()).Any())).First();
                    processor.Add(new LayerAssignment(state, 0, new ReadOnlyHashSet<State>(productNfa.StartStates.Concat(new[] {state}))));
                }
                firstLoop = false;
                processor.Run(layerAssignment => {
                    if (layerAssignment.Layer > results[layerAssignment.State]) {
                        results[layerAssignment.State] = layerAssignment.Layer;
                    }
                    foreach (var toState in productNfa.TransitionFunction[layerAssignment.State].SelectMany(inputSymbolAndToStates => inputSymbolAndToStates.Value).Distinct().Where(s => !layerAssignment.Predecessors.Contains(s))) {
                        var nextPredecessors = new ReadOnlyHashSet<State>(layerAssignment.Predecessors.Concat(new[] {toState}));
                        processor.Add(new LayerAssignment(toState, layerAssignment.Layer + 1, nextPredecessors));
                    }
                });
                unreachedStates = productNfa.States.Except(results.Keys).ToList();
            }

            var remap = results.Values.Distinct().OrderBy(i => i).Select((p, i) => new {p = p, i = i}).ToDictionary(pi => pi.p, pi => pi.i);
            foreach (var keyValuePair in results) {
                results[keyValuePair.Key] = remap[keyValuePair.Value];
            }
            return results;
        }
    }
}
