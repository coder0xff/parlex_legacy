using System.Collections.Generic;

namespace Parlex {
    internal class ParseContext {
        internal int Position { get; set; }
        internal List<MatchClass> ParseChain { get; set; }
    }
}
