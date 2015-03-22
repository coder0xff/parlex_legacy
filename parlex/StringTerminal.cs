using System;

namespace Parlex {
    public class StringTerminal : ITerminal {
        protected bool Equals(StringTerminal other) {
            return String.Equals(Text, other.Text);
        }

        public override int GetHashCode() {
            return (Text != null ? Text.GetHashCode() : 0);
        }

        private readonly String _text;
        private readonly Int32[] _unicodeCodePoints;

        public StringTerminal(String text) {
            if (text == null) {
                throw new ArgumentNullException("text");
            }
            _text = text;
            _unicodeCodePoints = text.GetUtf32CodePoints();
        }

        public bool Matches(Int32[] documentUtf32CodePoints, int documentIndex) {
            foreach (int codePoint in _unicodeCodePoints) {
                if (documentIndex >= documentUtf32CodePoints.Length) {
                    return false;
                }
                if (documentUtf32CodePoints[documentIndex++] != codePoint) {
                    return false;
                }
            }
            return true;
        }

        public int Length {
            get { return _unicodeCodePoints.Length; }
        }

        public String Name {
            get { return "String terminal: " + Text; }
        }

        public string Text {
            get { return _text; }
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
            if (obj.GetType() != this.GetType()) {
                return false;
            }
            return Equals((StringTerminal)obj);
        }
    }
}