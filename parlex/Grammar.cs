using System;
using System.Collections.Generic;
using System.Collections.Generic.More;
using System.Linq;
using System.Linq.More;
using System.Reflection;
using System.Text;

namespace Parlex {
    public class Grammar {
        public readonly List<NfaProduction> Productions = new List<NfaProduction>();
        public NfaProduction MainProduction;


        public NfaProduction GetRecognizerByName(String name) {
            return Productions.FirstOrDefault(x => x.Name == name);
        }

        public ISymbol GetSymbol(String name) {
            ISymbol result;
            if (!StandardSymbols.TryGetBuiltinISymbolByName(name, out result)) {
                result = GetRecognizerByName(name);
            }
            return result;
        }
    }
}