using System;
using Automata;

namespace Parlex {
    public class NfaProduction : RecognizerDefinition {
        public Nfa<RecognizerDefinition> Nfa = new Nfa<RecognizerDefinition>();
        private readonly bool _eatTrailingWhitespace;
        private readonly bool _greedy;
        private String _name;

        public NfaProduction(String name, bool greedy, bool eatTrailingWhitespace) {
            _name = name;
            _greedy = greedy;
            _eatTrailingWhitespace = eatTrailingWhitespace;
        }

        public NfaProduction(String name, bool greedy, Nfa<RecognizerDefinition> source) {
            Nfa = new Nfa<RecognizerDefinition>(source);
            _name = name;
            _greedy = greedy;
        }

        public bool Greedy {
            get { return _greedy; }
        }

        public bool EatWhiteSpace {
            get { return _eatTrailingWhitespace; }
        }

        public override String Name {
            get { return _name; }
        }

        public override string ToString() {
            return Name;
        }
    }
}