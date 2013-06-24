using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace parlex
{
    class SymbolClass
    {
        public String Title;
        public override bool Equals(object obj)
        {
            SymbolClass castObj = obj as SymbolClass;
            if ((Object)castObj == null) return false;
            return castObj.Title == this.Title;
        }
    }
}
