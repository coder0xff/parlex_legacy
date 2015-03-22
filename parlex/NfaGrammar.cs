using System;
using System.Collections.Generic;
using System.Collections.Generic.More;
using System.Linq;
using System.Linq.More;
using System.Reflection;
using System.Text;

namespace Parlex {
    public class NfaGrammar {
        public readonly List<NfaProduction> Productions = new List<NfaProduction>();
        public NfaProduction Main;


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

        public Grammar ToGrammar() {
            var result = new Grammar();
            foreach (var nfaProduction in Productions) {
                var production = new Production();
                production.Behavior = new BehaviorTree(nfaProduction);
                production.Name = nfaProduction.Name;
                result.Productions.Add(production);
                if (nfaProduction == Main) {
                    result.Main = production;
                }
            }
            return result;
        }
    }
}