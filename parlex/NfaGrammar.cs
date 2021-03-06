﻿using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Automata;

namespace Parlex {
    public class NfaGrammar {
        public Collection<NfaProduction> Productions {
            get { return _productions; }
        }


        public NfaProduction Main { get; set; }

        public NfaProduction GetProduction(String name) {
            return _productions.FirstOrDefault(x => x.Name == name);
        }

        public Grammar ToGrammar() {
            var result = new Grammar();
            var map = new AutoDictionary<NfaProduction, Production>(nfaProduction => new Production(nfaProduction.Name));
            //Convert NfaProductions to Productions
            foreach (var nfaProduction in _productions) {
                var resultProduction = map[nfaProduction];
                var clone = new Nfa<Recognizer>(nfaProduction.Nfa);
                //And convert the transition that are NfaProductions to Productions
                foreach (var from in clone.TransitionFunction) {
                    foreach (var transition in from.Value.ToArray()) {
                        var asNfaProduction = transition.Key as NfaProduction;
                        if (asNfaProduction != null) {
                            var production = map[asNfaProduction];
                            foreach (var to in transition.Value) {
                                from.Value[production].Add(to);
                            }
                            from.Value.TryRemove(asNfaProduction);
                        }
                    }
                }
                resultProduction.Behavior = new BehaviorTree(nfaProduction.Nfa);
                result.Productions.Add(resultProduction);
                if (nfaProduction == Main) {
                    result.Main = resultProduction;
                }
            }
            return result;
        }
        private Collection<NfaProduction> _productions = new Collection<NfaProduction>();
    }
}