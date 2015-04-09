using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Automata;

namespace Parlex {
    public class NfaSymbolNode : Recognizer {
        private readonly NfaProduction _production;

        public NfaSymbolNode(NfaProduction production) {
            _production = production;
        }

        public override void Start() {
            foreach (var state in _production.Nfa.StartStates) {
                ProcessState(state);
            }
        }

        private void ProcessState(Nfa<RecognizerDefinition>.State state) {
            if (_production.Nfa.AcceptStates.Contains(state)) {
                Accept();
            }
            foreach (var transition in _production.Nfa.GetTransitions().Where(transition => transition.FromState == state)) {
                var transition1 = transition;
                Transition(new SymbolNodeFactory(transition.Symbol), () => ProcessState(transition1.ToState));
            }
        }
    }
}
