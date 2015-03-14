using System;

namespace Parlex {
    public interface ITerminal : ISymbol {
        int Length { get; }
        bool Matches(Int32[] documentUtf32CodePoints, int documentIndex);
    }
}