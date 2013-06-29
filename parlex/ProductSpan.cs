using System;

namespace parlex {
    class ProductSpan : ICloneable {
        public Product Product;
        public int SpanStart;
        public int SpanLength;

        public ProductSpan(Product product, int spanStart, int spanLength) {
            Product = product;
            SpanStart = spanStart;
            SpanLength = spanLength;
        }

        public object Clone() {
            return new ProductSpan(Product, SpanStart, SpanLength);
        }
    }
}
