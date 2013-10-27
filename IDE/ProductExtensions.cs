using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using parlex;

namespace IDE {
    public static class ProductExtensions {
        public static Nfa<OldProduction, int> ToNfa(this OldProduction product) {
            return Nfa<OldProduction, int>.Union(product.Sequences.Select(sequence => sequence.ToNfa())).Minimized();
        }

        public static GrammarDocument ToGrammarDocument(this OldProduction product, Dictionary<String, OldProduction> allProducts) {
            return product.ToNfa().ToGrammarDocument(product.Title, allProducts);
        }
    }
}
