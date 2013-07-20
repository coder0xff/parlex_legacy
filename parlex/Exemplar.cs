using System;
using System.Collections.Generic;

namespace parlex {
    class Exemplar : ICloneable {
        internal readonly String Text;
        internal readonly List<ProductSpan> ProductSpans = new List<ProductSpan>();

        internal Exemplar(String text) {
            Text = text;
        }

        public object Clone() {
            var result = new Exemplar(Text);
            foreach (ProductSpan productSpan in ProductSpans) {
                result.ProductSpans.Add((ProductSpan)productSpan.Clone());
            }
            return result;
        }
    }
}
