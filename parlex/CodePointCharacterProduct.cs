using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parlex {
    class CodePointCharacterProduct : Product, IBuiltInCharacterProduct {
        public readonly Int32 CodePoint;

        public CodePointCharacterProduct(Int32 codePoint) : base("codePoint" + codePoint.ToString("X6")) {
            CodePoint = codePoint;
        }
        
        public bool Match(int codePoint) {
            return codePoint == CodePoint;
        }

        public override string ToString() {
            return Title + " '" + char.ConvertFromUtf32(CodePoint) + "'";
        }
    }
}
