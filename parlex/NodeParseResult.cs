using System;
using System.Collections.Generic;
using Parlex;

namespace Parlex {
    /// <summary>
    /// Stores information about the results of
    /// attempting to satisfy the specified transitions
    /// </summary>
    public class NodeParseResult {
        public HashSet<ISyntaxNodeFactory> FailedSymbols;
    }
}