﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using parlex;

namespace IDE {
    public static class ProductExtensions {
        public static Nfa<Product, int> ToNfa(this Product product) {
            return Nfa<Product, int>.Union(product.Sequences.Select(sequence => sequence.ToNfa()));
        }

        public static GrammarDocument ToGrammarDocument(this Product product, Dictionary<String, Product> allProducts) {
            return product.ToNfa().ToGrammarDocument(product.Title, allProducts);
        }
    }
}
