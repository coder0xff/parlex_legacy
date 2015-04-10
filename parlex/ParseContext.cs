using System.Collections.Generic;

namespace Parlex {
    public class ParseContext {
        public ParseEngine Engine { get; set; }
        public int Position { get; set; }
        internal ParseEngine.Dispatcher Dispatcher { get; set; }
        internal DependencyCounter DependencyCounter;
        internal List<MatchClass> ParseChain { get; set; }
    }
}
