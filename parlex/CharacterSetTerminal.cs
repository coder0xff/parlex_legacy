using System;
using System.Collections.Generic;

namespace Parlex {
    public class CharacterSetTerminal : Terminal {
        public override int Length {
            get { return 1; }
        }

        public override string Name {
            get { return _shortName; }
        }

        public CharacterSetTerminal(String name, IEnumerable<Int32> unicodeCodePoints, String shortName = null) : base(name) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }
            if (unicodeCodePoints == null) {
                throw new ArgumentNullException("unicodeCodePoints");
            }
            _name = name;
            _shortName = shortName ?? _name;
            _unicodeCodePoints = new HashSet<Int32>(unicodeCodePoints);
        }

        public override bool Matches(IReadOnlyList<Int32> documentUtf32CodePoints, int documentIndex) {
            if (documentUtf32CodePoints == null) {
                throw new ArgumentNullException("documentUtf32CodePoints");
            }
            if (documentIndex < 0) {
                throw new ArgumentOutOfRangeException("documentIndex", "documentIndex must be non-negative");
            }
            if (documentIndex >= documentUtf32CodePoints.Count) {
                return false;
            }
            return _unicodeCodePoints.Contains(documentUtf32CodePoints[documentIndex]);
        }

        public override string ToString() {
            return _name;
        }
        private readonly String _name;
        private readonly HashSet<Int32> _unicodeCodePoints;
        private readonly String _shortName;

    }
}