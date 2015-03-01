using System;
using System.Linq;
using System.Text;

namespace Parlex {
    internal class Match {
        internal MatchClass MatchClass {
            get {
                return new MatchClass(Position, Symbol, Length);
            }
        }
        internal int Position { get; set; }
        internal ISyntaxNodeFactory Symbol { get; set; }
        internal int Length { get; set; }
        internal MatchClass[] Children { get; set; }
        internal ParserEngine Engine { private get; set; }

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append("{" + Position + ":" + Length + ":" + Symbol.Name + ":\"");
            for (int i = 0; i < Length; ++i) {
                sb.Append(Char.ConvertFromUtf32(Engine.CodePoints[Position + i]));
            }
            sb.Append("\"}");
            return sb.ToString();
        }

        internal void StripWhiteSpaceEaters() {
            Children = Children.Where(x => !x.Symbol.Is(Grammar.WhiteSpaces)).ToArray();
        }

    }
}