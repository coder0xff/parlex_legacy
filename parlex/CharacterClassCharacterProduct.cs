using System;
using System.Collections.Generic;
using System.Linq;

namespace parlex {
    class CharacterClassCharacterProduct : Product, IBuiltInCharacterProduct {
        internal readonly HashSet<int> CodePoints;

        internal CharacterClassCharacterProduct(String title, IEnumerable<int> codePoints) : base (title) {
            CodePoints = new HashSet<int>(codePoints);
        }

        public bool Match(int codePoint) {
            return CodePoints.Contains(codePoint);
        }

        public Int32 GetExample() {
            return CodePoints.First();
        }
    }
}
