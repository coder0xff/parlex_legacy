using System;
using System.Collections.Generic;
using System.Linq;
using Common;

namespace parlex {
    public class CharacterClassCharacterProduct : Product, ICharacterProduct {
        internal readonly HashSet<int> CodePoints;
        private readonly GrammarDocument.CharacterSetEntry _source;

        internal CharacterClassCharacterProduct(String title, IEnumerable<int> codePoints, GrammarDocument.CharacterSetEntry source) : base (title) {
            CodePoints = new HashSet<int>(codePoints);
            _source = source;
        }

        public bool Match(int codePoint) {
            return CodePoints.Contains(codePoint);
        }

        public Int32 GetExampleCodePoint() {
            return CodePoints.Where(c => c < 128).OrderBy(x => Rng.Next()).First();
        }

        public override string GetExample() {
            return char.ConvertFromUtf32(GetExampleCodePoint());
        }

        public GrammarDocument.CharacterSetEntry Source {get { return _source; }}
    }
}
