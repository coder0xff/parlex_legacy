using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parlex {
    class CharacterClassCharacterProduct : Product, IBuiltInCharacterProduct {
        private readonly HashSet<int> _codePoints;
        public CharacterClassCharacterProduct(String title, IEnumerable<int> codePoints) : base (title) {
            _codePoints = new HashSet<int>(codePoints);
        }

        public bool Match(int codePoint) {
            return _codePoints.Contains(codePoint);
        }
    }
}
