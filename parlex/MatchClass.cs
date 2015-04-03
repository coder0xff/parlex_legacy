using System;

namespace Parlex {
    public class MatchClass {
        public ParseEngine Engine { get; private set; }
        public int Position { get; private set; }
        public IParseNodeFactory Symbol { get; private set; }
        public int Length { get; private set; }

        public String Text {
            get { return Engine.Document.Utf32Substring(Position, Length); }
        }

        private MatchCategory Category {
            get {
                return new MatchCategory(Position, Symbol);
            }
        }

        internal MatchClass(ParseEngine engine, int position, IParseNodeFactory symbol, int length) {
            Engine = engine;
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