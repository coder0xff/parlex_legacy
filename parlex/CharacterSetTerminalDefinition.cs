using System;
using System.Collections.Generic;

namespace Parlex {
    public class CharacterSetTerminalDefinition : TerminalDefinition {
        private readonly String _name;
        private readonly HashSet<Int32> _unicodeCodePoints;
        private readonly String _shortName;

        public CharacterSetTerminalDefinition(String name, IEnumerable<Int32> unicodeCodePoints, String shortName = null) {
            _name = name;
            _shortName = shortName ?? _name;
            _unicodeCodePoints = new HashSet<Int32>(unicodeCodePoints);
        }

        public override bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
            if (documentIndex >= documentUtf32CodePoints.Length) {
                return false;
            }
            return _unicodeCodePoints.Contains(documentUtf32CodePoints[documentIndex]);
        }

        public override int Length {
            get { return 1; }
        }

        public override string Name {
            get { return _shortName; }
        }

        public override string ToString() {
            return _name;
        }
    }
}