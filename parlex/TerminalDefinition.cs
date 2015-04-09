using System;

namespace Parlex {
    public abstract class TerminalDefinition : RecognizerDefinition {
        public abstract int Length { get; }
        public abstract bool Matches(Int32[] documentUtf32CodePoints, int documentIndex);
    }
}