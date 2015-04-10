using System;
using System.Collections.Generic;

namespace Parlex {
    public abstract class Terminal : Recognizer {
        public override string Name { get { return _name; } }
        public abstract int Length { get; }
        public override bool IsGreedy { get { return false; } }
        public abstract bool Matches(IReadOnlyList<Int32> documentUtf32CodePoints, int documentIndex);
        public override void Start() {
            if (!Matches(ParseContext.Engine.CodePoints, ParseContext.Position)) {
                return;
            }
            ParseContext.Position += Length;
            Accept();
        }

        protected Terminal(String name) {
            _name = name;
        }

        private readonly String _name;

    }
}