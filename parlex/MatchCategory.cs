namespace Parlex {
    public class MatchCategory {
        public int Position { get; private set; }
        public ISyntaxNodeFactory Symbol { get; private set; }
        internal MatchCategory(int position, ISyntaxNodeFactory symbol) {
            Position = position;
            Symbol = symbol;
        }

        public override bool Equals(object obj) {
            var castObj = obj as MatchCategory;
            if (ReferenceEquals(null, castObj)) {
                return false;
            }
            return castObj.Position.Equals(Position) && castObj.Symbol.Equals(Symbol);
        }

        public override int GetHashCode() {
            unchecked {
                int hash = 17;
                hash = hash * 31 + Position.GetHashCode();
                hash = hash * 31 + Symbol.GetHashCode();
                return hash;
            }
        }

        public override string ToString() {
            return "{" + Position + ":" + Symbol.Name + "}";
        }

    }
}