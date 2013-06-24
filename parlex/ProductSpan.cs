using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace parlex
{
    class ProductSpan
    {
        public Product Product;
        public int SpanStart;
        public int SpanLength;

        public ProductSpan(Product product, int spanStart, int spanLength)
        {
            Product = product;
            SpanStart = spanStart;
            SpanLength = spanLength;
        }
    }
}
