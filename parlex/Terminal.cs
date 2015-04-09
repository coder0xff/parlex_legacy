using System;

namespace Parlex {
    public abstract class Terminal : Recognizer {
        private readonly String _name;

        protected Terminal(String name) {
            _name = name;
        }

        public override void Start() {
            if (!Matches(Context.Value.Engine.CodePoints, Position)) {
                return;
            }
            Position += Length;
            Accept();
        }

        public override string Name { get { return _name; } }
        public override bool IsGreedy { get { return false; } }
        public abstract int Length { get; }
        public abstract bool Matches(Int32[] documentUtf32CodePoints, int documentIndex);
    }
}