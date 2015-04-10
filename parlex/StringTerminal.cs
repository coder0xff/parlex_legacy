using System;
using System.Collections.Generic;

namespace Parlex {
    public sealed class StringTerminal : Terminal {
        public override int Length {
            get { return _unicodeCodePoints.Length; }
        }

        public override bool IsGreedy {
            get { return false; }
        }

        public string Text {
            get { return _text; }
        }

        public StringTerminal(String text) : base("String Terminal: " + text) {
            if (text == null) {
                throw new ArgumentNullException("text");
            }
            _text = text;
            _unicodeCodePoints = text.GetUtf32CodePoints();
        }

        public override bool Matches(IReadOnlyList<Int32> documentUtf32CodePoints, int documentIndex) {
            if (documentUtf32CodePoints == null) {
                throw new ArgumentNullException("documentUtf32CodePoints");
            }
            if (documentIndex < 0) {
                throw new ArgumentOutOfRangeException("documentIndex", "documentIndex must be non-negative");
            }
            foreach (int codePoint in _unicodeCodePoints) {
                if (documentIndex >= documentUtf32CodePoints.Count) {
                    return false;
                }
                if (documentUtf32CodePoints[documentIndex++] != codePoint) {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode() {
            return (Text != null ? Text.GetHashCode() : 0);
        }

        public override string ToString() {
            return Text;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != GetType()) {
                return false;
            }
            return Equals((StringTerminal)obj);
        }
        private bool Equals(StringTerminal other) {
            return String.Equals(Text, other.Text);
        }

        private readonly String _text;
        private readonly Int32[] _unicodeCodePoints;

    }
}