using System;
using System.Linq;
using Automata;

namespace Parlex {
    public class NfaProduction : Recognizer {
        public override String Name {
            get { return _name; }
        }

        public override bool IsGreedy {
            get { return _greedy; }
        }

        public Nfa<Recognizer> Nfa {
            get { return _nfa; }
        }

        public NfaProduction(String name, bool greedy) {
            _name = name;
            _greedy = greedy;
        }

        public NfaProduction(String name, bool greedy, Nfa<Recognizer> source) {
            _nfa = new Nfa<Recognizer>(source);
            _name = name;
            _greedy = greedy;
        }

        public override void Start() {
            EnterConfiguration(_nfa.StartStates.ToArray());
        }

        public override string ToString() {
            return Name;
        }
        private void EnterConfiguration(Nfa<Recognizer>.State[] states) {
            foreach (var state in states) {
                foreach (var keyValuePair in _nfa.TransitionFunction[state]) {
                    var pair = keyValuePair;
                    Transition(keyValuePair.Key, () => EnterConfiguration(pair.Value.ToArray()));
                }
            }
            if (states.Any(state => _nfa.AcceptStates.Contains(state))) {
                Accept();
            }
        }

        private Nfa<Recognizer> _nfa = new Nfa<Recognizer>();
        private readonly bool _greedy;
        private readonly String _name;

    }
}