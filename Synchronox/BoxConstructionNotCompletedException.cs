using System;

namespace Synchronox {
    public class BoxConstructionNotCompletedException : Exception {
        public BoxConstructionNotCompletedException() : base("The box's constructor did not call ConstructionComplete()") {}
    }
}