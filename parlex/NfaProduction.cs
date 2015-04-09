using System;
using Automata;

namespace Parlex {
    public class NfaProduction : ISymbol {
        public Nfa<ISymbol> Nfa;
        private readonly bool _eatTrailingWhitespace;
        private readonly bool _greedy;
        private String _name;

        public NfaProduction(String name, bool greedy, bool eatTrailingWhitespace) {
            _name = name;
            _greedy = greedy;
            _eatTrailingWhitespace = eatTrailingWhitespace;
        }

        public NfaProduction(String name, bool greedy, Nfa<ISymbol> source) {
            Nfa = new Nfa<ISymbol>(source);
            _name = name;
            _greedy = greedy;
        }

        public bool Greedy {
            get { return _greedy; }
        }

        public bool EatWhiteSpace {
            get { return _eatTrailingWhitespace; }
        }

        public String Name {
            get { return _name; }
            set { _name = value; }
        }

        public override string ToString() {
            return Name;
        }
    }
}