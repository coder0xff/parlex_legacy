using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    public class NfaSymbolNode : SyntaxNode {
        private readonly Grammar.Production _production;

        public NfaSymbolNode(Grammar.Production production) {
            _production = production;
        }

        public override void Start() {
            foreach (var state in _production.StartStates) {
                ProcessState(state);
            }
        }

        private void ProcessState(Grammar.Production.State state) {
            if (_production.AcceptStates.Contains(state)) {
                Accept();
            }
            foreach (var transition in _production.GetTransitions().Where(transition => transition.FromState == state)) {
                var transition1 = transition;
                Transition(new SymbolNodeFactory(transition.Symbol), () => ProcessState(transition1.ToState));
            }
        }
    }
}
