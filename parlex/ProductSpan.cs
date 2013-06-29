using System;

namespace parlex {
    class ProductSpan : ICloneable {
        public Product Product;
        public int SpanStart;
        public int SpanLength;
        public bool IsRepititious;

        public ProductSpan(Product product, int spanStart, int spanLength, bool isRepititious) {
            Product = product;
            SpanStart = spanStart;
            SpanLength = spanLength;
            IsRepititious = isRepititious;
        }

        public object Clone() {
            return new ProductSpan(Product, SpanStart, SpanLength, IsRepititious);
        }
    }
}
