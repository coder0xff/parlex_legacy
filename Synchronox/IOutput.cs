using System.Collections.Generic;

namespace Synchronox {
    interface IOutput {
        IEnumerable<IInput> GetConnectedInputs();
        Node Owner { get; }
    }
}
