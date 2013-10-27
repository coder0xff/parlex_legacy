using System;
using System.Linq;

namespace parlex {
    public class CodePointCharacterProduct : OldProduction, ICharacterProduct {
        private readonly Int32 CodePoint;

        internal CodePointCharacterProduct(Int32 codePoint) : base("codePoint" + codePoint.ToString("X6")) {
            CodePoint = codePoint;
        }
        
        public bool Match(int codePoint) {
            return codePoint == CodePoint;
        }

        public Int32 GetExampleCodePoint() {
            return CodePoint;
        }

        public override string GetExample() {
            if (Unicode.LineTerminators.Contains(CodePoint)) {
                return null;
            }
            return char.ConvertFromUtf32(GetExampleCodePoint());
        }

        public override string ToString() {
            return Title + " '" + char.ConvertFromUtf32(CodePoint) + "'";
        }
    }
}
