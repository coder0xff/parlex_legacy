using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parlex
{
    class Exemplar
    {
        public String Text;
        public List<ProductSpan> ProductSpans = new List<ProductSpan>();

        public Exemplar(String text)
        {
            Text = text;
        }
    }
}
