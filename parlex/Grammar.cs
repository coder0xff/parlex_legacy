using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace Parlex {
    public class Grammar {
        public List<Production> Productions = new List<Production>();

        public Production Main;

        public Production GetProduction(String name) {
            return Productions.FirstOrDefault(production => production.Name == name);
        }

        public void ResolveNode(BehaviorTree.Choice choice) {
            foreach (var child in choice.Children) {
                ResolveNode(child);
            }
        }

        public void ResolveNode(BehaviorTree.Leaf leaf) {
            var placeHolder = leaf.RecognizerDefinition as PlaceholderIRecognizerDefinition;
            if (placeHolder != null) {
                var resolved = GetProduction(placeHolder.Name);
                if (resolved != null) {
                    leaf.RecognizerDefinition = resolved;
                } else {
                    throw new UndefinedProductionException(placeHolder.Name);
                }
            }
        }

        public void ResolveNode(BehaviorTree.Optional optional) {
            ResolveNode(optional.Child);
        }

        public void ResolveNode(BehaviorTree.Repetition repetition) {
            ResolveNode(repetition.Child);
        }

        public void ResolveNode(BehaviorTree.Sequence sequence) {
            foreach (var child in sequence.Children) {
                ResolveNode(child);
            }
        }

        public static DynamicDispatcher _resolveNodeDispatcher;
        void ResolveNode(BehaviorTree.Node node) {
            if (_resolveNodeDispatcher == null) {
                _resolveNodeDispatcher = new DynamicDispatcher();
            }
            _resolveNodeDispatcher.Dispatch<Object>(this, node);
        }

        internal void Resolve() {
            foreach (var production in Productions) {
                ResolveNode(production.Behavior.Root);
            }
        }

        public NfaGrammar ToNfaGrammar() {
            var result = new NfaGrammar();
            Dictionary<Production, NfaProduction> map = new Dictionary<Production, NfaProduction>();
            foreach (var production in Productions) {
                var nfa = production.Behavior.ToNfa();
                var nfaProduction = new NfaProduction(production.Name, production.Greedy, nfa);
                result.Productions.Add(nfaProduction);
                if (production == Main) {
                    result.Main = nfaProduction;
                }
                map[production] = nfaProduction;
            }
            foreach (var nfaProduction in result.Productions) {
                foreach (var from in nfaProduction.Nfa.TransitionFunction) {
                    foreach (var transition in from.Value.ToArray()) {
                        var asProduction = transition.Key as Production;
                        NfaProduction transitionNfa;
                        if (asProduction != null && map.TryGetValue(asProduction, out transitionNfa)) {
                            foreach (var to in transition.Value) {
                                from.Value[transitionNfa].Add(to);
                            }
                            from.Value.TryRemove(asProduction);
                        }
                    }
                }
            }
            return result;
        }
    }
}
