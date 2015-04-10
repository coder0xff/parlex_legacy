namespace Parlex {
    public class MatchClass {
        public ParseEngine Engine { get; private set; }
        public int Position { get; private set; }
        public Recognizer Recognizer { get; private set; }
        public int Length { get; private set; }

        public MatchCategory Category {
            get {
                return new MatchCategory(Position, Recognizer);
            }
        }

        public override bool Equals(object obj) {
            var castObj = obj as MatchClass;
            if (ReferenceEquals(null, castObj)) {
                return false;
            }
            return Category.Equals(castObj.Category) && Length.Equals(castObj.Length);
        }

        public override int GetHashCode() {
            unchecked {
                int hash = 17;
                hash = hash * 31 + Category.GetHashCode();
                hash = hash * 32 + Length.GetHashCode();
                return hash;
            }
        }

        public override string ToString() {
            return "{" + Position + ":" + Length + ":" + Recognizer.Name + "}" ;
        }

        internal MatchClass(ParseEngine engine, int position, Recognizer recognizer, int length) {
            Engine = engine;
            Position = position;
            Recognizer = recognizer;
            Length = length;
        }

    }
}