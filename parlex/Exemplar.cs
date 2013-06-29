using System;
using System.Collections.Generic;

namespace parlex {
    class Exemplar : ICloneable {
        public String Text;
        public List<ProductSpan> ProductSpans = new List<ProductSpan>();

        public Exemplar(String text) {
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
