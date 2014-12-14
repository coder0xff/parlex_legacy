using System;

namespace Synchronox {
    public class NodeConstructionNotCompletedException : Exception {
        public NodeConstructionNotCompletedException() : base("The node's constructor did not call ConstructionComplete()") {}
    }
}