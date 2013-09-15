﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using parlex;

namespace IDE {
    public static class ProductExtensions {
        public static Nfa<Product, int> ToNfa(this Product product) {
            return Nfa<Product, int>.Union(product.Sequences.Select(sequence => sequence.ToNfa())).Minimized();
        }

        public static GrammarDocument.ExemplarSource[] GenerateExemplarSources(this Product product) {
            throw new NotImplementedException();
        }
    }
}