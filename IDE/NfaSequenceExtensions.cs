using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using parlex;

namespace IDE {
    public static class NfaSequenceExtensions {
        public static Nfa<Product, int> ToNfa(this CompiledGrammar.NfaSequence sequence) {
            sequence.RelationBranches
        }
    }
}
