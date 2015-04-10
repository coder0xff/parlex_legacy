using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Parlex.Annotations;

namespace Parlex {
    public class Grammar {
        public Production Main { get; set; }

        public List<Production> Productions {
            get { return _productions; }
        }

        public Production GetProduction(String name) {
            return _productions.FirstOrDefault(production => production.Name == name);
        }

        public NfaGrammar ToNfaGrammar() {
            var result = new NfaGrammar();
            var map = new Dictionary<Production, NfaProduction>();
            foreach (var production in _productions) {
                var nfa = production.Behavior.ToNfa();
                var nfaProduction = new NfaProduction(production.Name, production.IsGreedy, nfa);
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

        [UsedImplicitly]
        public void ResolveNode(ChoiceBehavior choiceBehavior) {
            foreach (var child in choiceBehavior.Children) {
                ResolveNode(child);
            }
        }

        [UsedImplicitly]
        public void ResolveNode(BehaviorLeaf behaviorLeaf) {
            var placeHolder = behaviorLeaf.Recognizer as PlaceholderProduction;
            if (placeHolder != null) {
                var resolved = GetProduction(placeHolder.Name);
                if (resolved != null) {
                    behaviorLeaf.Recognizer = resolved;
                } else {
                    throw new UndefinedProductionException(placeHolder.Name);
                }
            }
        }

        [UsedImplicitly]
        public void ResolveNode(Optional optional) {
            ResolveNode(optional.Child);
        }

        [UsedImplicitly]
        public void ResolveNode(RepetitionBehavior repetitionBehavior) {
            ResolveNode(repetitionBehavior.Child);
        }

        [UsedImplicitly]
        public void ResolveNode(SequenceBehavior sequenceBehavior) {
            foreach (var child in sequenceBehavior.Children) {
                ResolveNode(child);
            }
        }
        void ResolveNode(BehaviorNode behaviorNode) {
            if (_resolveNodeDispatcher == null) {
                _resolveNodeDispatcher = new DynamicDispatcher();
            }
            _resolveNodeDispatcher.Dispatch<Object>(this, behaviorNode);
        }

        internal void Resolve() {
            foreach (var production in _productions) {
                ResolveNode(production.Behavior.Root);
            }
        }

        private readonly List<Production> _productions = new List<Production>();

        private static DynamicDispatcher _resolveNodeDispatcher;
    }
}
