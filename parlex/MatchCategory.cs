namespace Parlex {
    public class MatchCategory {
        public int Position { get; private set; }
        public Recognizer Recognizer { get; private set; }
        internal MatchCategory(int position, Recognizer recognizer) {
            Position = position;
            Recognizer = recognizer;
        }

        public override bool Equals(object obj) {
            var castObj = obj as MatchCategory;
            if (ReferenceEquals(null, castObj)) {
                return false;
            }
            return castObj.Position.Equals(Position) && castObj.Recognizer.Equals(Recognizer);
        }

        public override int GetHashCode() {
            unchecked {
                int hash = 17;
                hash = hash * 31 + Position.GetHashCode();
                hash = hash * 31 + Recognizer.GetHashCode();
                return hash;
            }
        }

        public override string ToString() {
            return "{" + Position + ":" + Recognizer.Name + "}";
        }

    }
}