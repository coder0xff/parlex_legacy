using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    public interface IParseNodeFactory {
        String Name { get; }
        Boolean IsGreedy { get; }
        Recognizer Create();
        bool Is(TerminalDefinition terminalDefinition);
        bool Is(NfaProduction production);
    }
}
