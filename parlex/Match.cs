using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parlex {
    public class Match {
        internal MatchClass MatchClass {
            get {
                return new MatchClass(Engine, Position, Recognizer, Length);
            }
        }
        public int Position { get; internal set; }
        public Recognizer Recognizer { get; internal set; }
        public int Length { get; internal set; }
        public ParseEngine Engine { get; set; }
        public IReadOnlyList<MatchClass> Children {
            get { return ChildrenArray.ToList(); }
        }

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append("{" + Position + ":" + Length + ":" + Recognizer.Name + ":\"");
            for (int i = 0; i < Length; ++i) {
                sb.Append(Char.ConvertFromUtf32(Engine.CodePoints[Position + i]));
            }
            sb.Append("\"}");
            return sb.ToString();
        }

        internal MatchClass[] ChildrenArray;

        internal void StripWhiteSpaceEaters() {
            ChildrenArray = ChildrenArray.Where(x => x.Recognizer != StandardSymbols.WhiteSpaces).ToArray();
        }

    }
}