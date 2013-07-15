using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parlex {
    class CharacterClassCharacterProduct : Product, IBuiltInCharacterProduct {
        public readonly HashSet<int> CodePoints;
        public CharacterClassCharacterProduct(String title, IEnumerable<int> codePoints) : base (title) {
            CodePoints = new HashSet<int>(codePoints);
        }

        public bool Match(int codePoint) {
            return CodePoints.Contains(codePoint);
        }
    }
}
