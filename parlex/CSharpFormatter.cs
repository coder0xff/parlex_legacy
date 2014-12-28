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
        private Dictionary<Grammar.ISymbol, String> SerializeStringTerminals(StreamWriter s, Grammar grammar) {
            var results = new Dictionary<Grammar.ISymbol, String>();
            var stringTerminals = grammar.Productions.SelectMany(production =>
                production.TransitionFunction.SelectMany(x => x.Value).Select(x => x.Key).Distinct().Where(x => x is Grammar.StringTerminal).Cast<Grammar.StringTerminal>()
                ).ToArray();
            for (var i = 0; i < stringTerminals.Length; i++) {
                results[stringTerminals[i]] = "stringTerminal" + i;
                s.WriteLine("\tprivate static readonly Grammar.ITerminal stringTerminal" + i + " = new Grammar.StringTerminal(\"" + stringTerminals[i] + "\");");
            }
            return results;
        }

        private void DefineProduction(StreamWriter s, Grammar.Production production) {
            s.WriteLine("\tprivate static readonly Grammar.Production " + production.Name + " = new Grammar.Production(" + production.Name + ", " + production.Greedy + ", " + production.EatWhiteSpace + ");");
        }

        private void SerializeProduction(StreamWriter s, Grammar.Production production, Dictionary<Grammar.ISymbol, string> stringTerminalNames) {
            var names = new Dictionary<Nfa<Grammar.ISymbol>.State, string>();
            int counter = 0;
            foreach (var state in production.States) {
                names[state] = production.Name + "_State" + counter;
                counter++;
                s.WriteLine("\t\tvar " + names[state] + " = new Grammar.Production.State();");
                s.WriteLine("\t\t" + production.Name + ".States.Add(" + names[state] + ");");
            }
            foreach (var startState in production.StartStates) {
                s.WriteLine("\t\t" + production.Name + ".StartStates.Add(" + names[startState] +");");
            }
            foreach (var acceptState in production.AcceptStates) {
                s.WriteLine("\t\t" + production.Name + ".AcceptStates.Add(" + names[acceptState] + ");");
            }
            foreach (var transition in production.GetTransitions()) {
                var symbolName = transition.Symbol.Name;
                if (stringTerminalNames.ContainsKey(transition.Symbol)) {
                    symbolName = stringTerminalNames[transition.Symbol];
                } else {
                    FieldInfo field;
                    if (Grammar.TryGetBuiltinFieldByName("symbolName", out field)) {
                        symbolName = field.DeclaringType.FullName + "." + field.Name;
                    }
                }
                s.WriteLine("\t\t" + production.Name + ".TransitionFunction[" + names[transition.FromState] + "][" + symbolName + "].Add(" + transition.ToState + ")");
            }
            s.WriteLine();
        }

        public void Serialize(Stream s, Grammar grammar) {
            var sw = new StreamWriter(s);
            sw.WriteLine("public static GeneratedGrammar {");
            sw.WriteLine("\tpublic static Grammar Grammar;");
            var stringTerminalNames = SerializeStringTerminals(sw, grammar);
            foreach (var production in grammar.Productions) {
                DefineProduction(sw, production);
            }
            sw.WriteLine("\tstatic GeneratedGrammar() {");
            foreach (var production in grammar.Productions) {
                SerializeProduction(sw, production, stringTerminalNames);
            }
            sw.WriteLine("\t}");
            sw.WriteLine("}");
            sw.Close();
        }

        public Grammar Deserialize(Stream s) {
            throw new NotImplementedException();
        }
    }
}
