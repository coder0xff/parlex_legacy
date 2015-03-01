namespace Parlex {
    internal class MatchClass {
        private int Position { get; set; }
        internal ISyntaxNodeFactory Symbol { get; private set; }
        internal int Length { get; private set; }

        private MatchCategory Category {
            get {
                return new MatchCategory(Position, Symbol);
            }
        }

        internal MatchClass(int position, ISyntaxNodeFactory symbol, int length) {
            Position = position;
            Symbol = symbol;
            Length = length;
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
            return "{" + Position + ":" + Length + ":" + Symbol.Name + "}" ;
        }

    }
}