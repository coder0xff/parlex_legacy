using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    class PlaceholderISymbol : ISymbol{
        public PlaceholderISymbol(String name) {
            Name = name;
        }

        public string Name { get; private set; }
    }
}
