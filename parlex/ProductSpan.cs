using System;

namespace parlex {
    class ProductSpan : ICloneable {
        internal readonly OldProduction Product;
        internal readonly int SpanStart;
        internal readonly int SpanLength;
        internal readonly bool IsRepititious;

        internal ProductSpan(OldProduction product, int spanStart, int spanLength, bool isRepititious) {
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
