using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using parlex;

namespace IDE {
    public static class NfaSequenceExtensions {
        public static Nfa<Product, int> ToNfa(this CompiledGrammar.NfaSequence sequence) {
            var positionToState = new AutoDictionary<int /*position*/, Nfa<Product, int>.State>(position => new Nfa<Product, int>.State(position));
            var result = new Nfa<Product, int>();

            for (int position = 0; position < sequence.RelationBranches.Length; position++) {
                foreach (var productReference in sequence.RelationBranches[position]) {
                    var fromState = positionToState[position];
                    Nfa<Product, int>.State toState = productReference.IsRepetitious ? fromState : positionToState[productReference.ExitSequenceCounter - sequence.SpanStart];
                    var transitionProduct = productReference.Product;
                    result.TransitionFunction[fromState][transitionProduct].Add(toState);
                }
            }

            result.StartStates.Add(positionToState[0]);
            result.AcceptStates.Add(positionToState[sequence.RelationBranches.Length]);

            return result;
        }
    }
}
