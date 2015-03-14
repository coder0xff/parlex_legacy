using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Automata;

namespace Parlex {
    public class CSharpFormatter : IGrammarFormatter {
        private Dictionary<ISymbol, String> SerializeStringTerminals(StreamWriter s, Grammar grammar) {
            var results = new Dictionary<ISymbol, String>();
            var stringTerminals = grammar.Productions.SelectMany(production =>
                production.TransitionFunction.SelectMany(x => x.Value).Select(x => x.Key).Distinct().Where(x => x is StringTerminal).Cast<StringTerminal>()
                ).ToArray();
            for (var i = 0; i < stringTerminals.Length; i++) {
                var name = "StringTerminal" + i;
                results[stringTerminals[i]] = name;
                s.WriteLine("\tprivate static readonly Grammar.ITerminal " + name + " = new Grammar.StringTerminal(\"" + stringTerminals[i] + "\");");
            }
            return results;
        }

        private static String CSharpName(String productionName) {
            bool capitalized = true;
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < productionName.Length; i++) {
                var c = productionName[i];
                if (Char.IsLetter(c)) {
                    c = capitalized ? Char.ToUpper(c) : Char.ToLower(c);
                    result.Append(c);
                    capitalized = false;
                    continue;
                }
                capitalized = false;
                if (c == '_') {
                    capitalized = true;
                    continue;
                }
                result.Append(c);
            }
            return result.ToString();
        }

        private void DefineProduction(StreamWriter s, NfaProduction production) {
            var cSharpName = CSharpName(production.Name);
            s.WriteLine("\tprivate static readonly Grammar.Production " + cSharpName + " = new Grammar.Production(\"" + cSharpName + "\", " + (production.Greedy ? "true" : "false") + ", " + (production.EatWhiteSpace ? "true" : "false") + ");");
        }

        private void SerializeProduction(StreamWriter s, NfaProduction production, Dictionary<ISymbol, string> stringTerminalNames) {
            var cSharpName = CSharpName(production.Name);
            var lowerCSharpName = Char.ToLower(cSharpName[0]) + cSharpName.Substring(1);
            var names = new Dictionary<Nfa<ISymbol>.State, string>();
            int counter = 0;
            foreach (var state in production.States) {
                names[state] = lowerCSharpName + "State" + counter;
                counter++;
                s.WriteLine("\t\tvar " + names[state] + " = new Grammar.Production.State();");
                s.WriteLine("\t\t" + cSharpName + ".States.Add(" + names[state] + ");");
            }
            foreach (var startState in production.StartStates) {
                s.WriteLine("\t\t" + cSharpName + ".StartStates.Add(" + names[startState] +");");
            }
            foreach (var acceptState in production.AcceptStates) {
                s.WriteLine("\t\t" + cSharpName + ".AcceptStates.Add(" + names[acceptState] + ");");
            }
            foreach (var transition in production.GetTransitions()) {
                var symbolName = transition.Symbol.Name;
                if (stringTerminalNames.ContainsKey(transition.Symbol)) {
                    symbolName = stringTerminalNames[transition.Symbol];
                } else {
                    FieldInfo field;
                    if (StandardSymbols.TryGetBuiltinFieldByName(symbolName, out field)) {
                        symbolName = field.DeclaringType.FullName + "." + field.Name;
                        if (symbolName.StartsWith("Parlex.")) {
                            symbolName = symbolName.Substring("Parlex.".Length);
                        }
                    } else {
                        symbolName = CSharpName(symbolName);
                    }
                }
                s.WriteLine("\t\t" + cSharpName + ".TransitionFunction[" + names[transition.FromState] + "][" + symbolName + "].Add(" + names[transition.ToState] + ");");
            }
            s.WriteLine("\t\tGrammar.Productions.Add(" + cSharpName + ");");
            s.WriteLine();
        }

        public void Serialize(Stream s, Grammar grammar) {
            var sw = new StreamWriter(s);
            sw.WriteLine("using Parlex;");
            sw.WriteLine();
            sw.WriteLine("public static class GeneratedGrammar {");
            sw.WriteLine("\tpublic static Grammar Grammar = new Grammar();");
            var stringTerminalNames = SerializeStringTerminals(sw, grammar);
            foreach (var production in grammar.Productions) {
                DefineProduction(sw, production);
            }
            sw.WriteLine("\tstatic GeneratedGrammar() {");
            foreach (var production in grammar.Productions) {
                SerializeProduction(sw, production, stringTerminalNames);
            }
            sw.WriteLine("\t\tGrammar.MainProduction = Syntax;");
            sw.WriteLine("\t}");
            sw.WriteLine("}");
            sw.Close();
        }

        public Grammar Deserialize(Stream s) {
            throw new NotImplementedException();
        }
    }
}
