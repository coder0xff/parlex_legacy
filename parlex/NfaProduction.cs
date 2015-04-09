using System;
using System.Collections.Generic;
using System.Linq;
using Automata;

namespace Parlex {
    public class NfaProduction : Recognizer {
        public Nfa<Recognizer> Nfa = new Nfa<Recognizer>();
        private readonly bool _eatTrailingWhitespace;
        private readonly bool _greedy;
        private String _name;

        public NfaProduction(String name, bool greedy, bool eatTrailingWhitespace) {
            _name = name;
            _greedy = greedy;
            _eatTrailingWhitespace = eatTrailingWhitespace;
        }

        public NfaProduction(String name, bool greedy, Nfa<Recognizer> source) {
            Nfa = new Nfa<Recognizer>(source);
            _name = name;
            _greedy = greedy;
        }

        public bool EatWhiteSpace {
            get { return _eatTrailingWhitespace; }
        }

        public override String Name {
            get { return _name; }
        }

        public override bool IsGreedy {
            get { return _greedy; }
        }

        public override void Start() {
            EnterConfiguration(Nfa.StartStates.ToArray());
        }

        private void EnterConfiguration(Nfa<Recognizer>.State[] states) {
            foreach (var state in states) {
                foreach (var keyValuePair in Nfa.TransitionFunction[state]) {
                    var pair = keyValuePair;
                    Transition(keyValuePair.Key, () => EnterConfiguration(pair.Value.ToArray()));
                }
            }
            if (states.Any(state => Nfa.AcceptStates.Contains(state))) {
                Accept();
            }
        }

        public override string ToString() {
            return Name;
        }
    }
}