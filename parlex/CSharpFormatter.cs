﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Automata;

namespace Parlex {
    public class CSharpFormatter : IMetaSyntax {
        public Grammar Parse(Stream s) {
            throw new NotImplementedException();
        }
        public void Generate(Stream s, Grammar grammar) {
            if (s == null) {
                throw new ArgumentNullException("s");
            }
            if (grammar == null) {
                throw new ArgumentNullException("grammar");
            }
            var nfaGrammar = grammar.ToNfaGrammar();
            var sw = new StreamWriter(s);
            sw.WriteLine("//This file was generated by the Parlex C# exporter.");
            sw.WriteLine("using Parlex;");
            sw.WriteLine();
            sw.WriteLine("public static class GeneratedGrammar {");
            sw.WriteLine("\tpublic static NfaGrammar NfaGrammar = new NfaGrammar();");
            var stringTerminalNames = SerializeStringTerminals(sw, nfaGrammar);
            foreach (var production in nfaGrammar.Productions) {
                DefineProduction(sw, production);
            }
            sw.WriteLine("\tstatic GeneratedGrammar() {");
            foreach (var production in nfaGrammar.Productions) {
                SerializeProduction(sw, production, stringTerminalNames);
            }
            sw.WriteLine("\t\tNfaGrammar.Main = Syntax;");
            sw.WriteLine("\t}");
            sw.WriteLine("}");
            sw.Close();
        }

        private static Dictionary<Recognizer, String> SerializeStringTerminals(StreamWriter s, NfaGrammar grammar) {
            var results = new Dictionary<Recognizer, String>();
            var stringTerminals = grammar.Productions.SelectMany(production =>
                production.Nfa.TransitionFunction.SelectMany(x => x.Value).Select(x => x.Key).Distinct().Where(x => x is StringTerminal).Cast<StringTerminal>()
                ).ToArray();
            for (var i = 0; i < stringTerminals.Length; i++) {
                var name = "StringTerminal" + i;
                results[stringTerminals[i]] = name;
                s.WriteLine("\tprivate static readonly Terminal " + name + " = new StringTerminal(\"" + stringTerminals[i] + "\");");
            }
            return results;
        }

        private static String CSharpName(String productionName) {
            bool capitalized = true;
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < productionName.Length; i++) {
                var c = productionName[i];
                if (Char.IsLetter(c)) {
                    c = capitalized ? Char.ToUpper(c, CultureInfo.InvariantCulture) : Char.ToLower(c, CultureInfo.InvariantCulture);
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

        private static void DefineProduction(StreamWriter s, NfaProduction production) {
            var cSharpName = CSharpName(production.Name);
            s.WriteLine("\tprivate static readonly NfaProduction " + cSharpName + " = new NfaProduction(\"" + cSharpName + "\", " + (production.IsGreedy ? "true" : "false") + ");");
        }

        private static void SerializeProduction(StreamWriter s, NfaProduction production, Dictionary<Recognizer, string> stringTerminalNames) {
            var cSharpName = CSharpName(production.Name);
            var lowerCSharpName = Char.ToLower(cSharpName[0], CultureInfo.InvariantCulture) + cSharpName.Substring(1);
            var names = new Dictionary<Nfa<Recognizer>.State, string>();
            int counter = 0;
            foreach (var state in production.Nfa.States) {
                names[state] = lowerCSharpName + "State" + counter;
                counter++;
                s.WriteLine("\t\tvar " + names[state] + " = new Nfa<Production>.State();");
                s.WriteLine("\t\t" + cSharpName + ".States.Add(" + names[state] + ");");
            }
            foreach (var startState in production.Nfa.StartStates) {
                s.WriteLine("\t\t" + cSharpName + ".StartStates.Add(" + names[startState] +");");
            }
            foreach (var acceptState in production.Nfa.AcceptStates) {
                s.WriteLine("\t\t" + cSharpName + ".AcceptStates.Add(" + names[acceptState] + ");");
            }
            foreach (var transition in production.Nfa.GetTransitions()) {
                var symbolName = transition.Symbol.Name;
                if (stringTerminalNames.ContainsKey(transition.Symbol)) {
                    symbolName = stringTerminalNames[transition.Symbol];
                } else {
                    FieldInfo field;
                    if (StandardSymbols.TryGetBuiltInFieldByName(symbolName, out field)) {
                        symbolName = field.DeclaringType.FullName + "." + field.Name;
                        if (symbolName.StartsWith("Parlex.", StringComparison.InvariantCulture)) {
                            symbolName = symbolName.Substring("Parlex.".Length);
                        }
                    } else {
                        symbolName = CSharpName(symbolName);
                    }
                }
                s.WriteLine("\t\t" + cSharpName + ".TransitionFunction[" + names[transition.FromState] + "][" + symbolName + "].Add(" + names[transition.ToState] + ");");
            }
            s.WriteLine("\t\tNfaGrammar.Productions.Add(" + cSharpName + ");");
            s.WriteLine();
        }

    }
}
