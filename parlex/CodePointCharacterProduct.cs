using System;

namespace parlex {
    class CodePointCharacterProduct : Product, IBuiltInCharacterProduct {
        private readonly Int32 CodePoint;

        internal CodePointCharacterProduct(Int32 codePoint) : base("codePoint" + codePoint.ToString("X6")) {
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
