using System.Collections;
using System.Collections.Generic;

namespace Synchronox {
    internal interface IInput {
        IEnumerable<IOutput> GetConnectedOutputs();
        Node Owner { get; }
        void CheckWillHalt();
        bool IsBlocked { get; }
        void SignalHalt();
        void Lock();
        void Unlock();
    }
}
