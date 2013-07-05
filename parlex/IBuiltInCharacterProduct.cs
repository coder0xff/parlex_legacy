using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parlex {
    interface IBuiltInCharacterProduct {
        bool Match(Int32 codePoint);
    }
}
