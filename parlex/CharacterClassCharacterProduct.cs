using System;
using System.Collections.Generic;

namespace parlex {
    class CharacterClassCharacterProduct : Product, IBuiltInCharacterProduct {
        internal readonly HashSet<int> CodePoints;

        internal CharacterClassCharacterProduct(String title, IEnumerable<int> codePoints) : base (title) {
            CodePoints = new HashSet<int>(codePoints);
        }

        public bool Match(int codePoint) {
            return CodePoints.Contains(codePoint);
        }
    }
}
