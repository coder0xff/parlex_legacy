using System.Collections.Generic;

namespace Parlex {
    public class ParseContext {
        public ParseEngine Engine { get; set; }
        internal ParseEngine.Dispatcher Dispatcher { get; set; }
        internal int _activeDependencyCount;
        internal int Position { get; set; }
        internal List<MatchClass> ParseChain { get; set; }
    }
}
