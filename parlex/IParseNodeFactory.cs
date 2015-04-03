using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    public interface IParseNodeFactory {
        String Name { get; }
        Boolean IsGreedy { get; }
        ParseNode Create();
        bool Is(ITerminal terminal);
        bool Is(NfaProduction production);
    }
}
