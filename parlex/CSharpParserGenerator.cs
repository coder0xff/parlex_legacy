using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Automata;

namespace Parlex {
    public class CSharpParserGenerator : IParserGenerator {
        private static String CSharpName(String name) {
            bool capitalized = true;
            var result = new StringBuilder();
            foreach (var t in name) {
                var c = t;
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

        private String _namespace;

        public CSharpParserGenerator(String @namespace) {
            _namespace = @namespace;
        }

        private void OutputTransitions(StringBuilder builder, String _namespace, AutoDictionary<Grammar.ISymbol, String> nameMap, Nfa<Grammar.ISymbol, int> nfa, Nfa<Grammar.ISymbol, int>.State fromState) {
            foreach (var transitionAndToStates in nfa.TransitionFunction[fromState]) {
                var transition = transitionAndToStates.Key;
                foreach (var toState in transitionAndToStates.Value) {
                    var asStringTerminal = transition as Grammar.StringTerminal;
                    if (asStringTerminal != null) {
                        builder.AppendLine("\t\t\tTransition(" + Grammar.QuoteStringLiteral(asStringTerminal.ToString()) + ", State" + toState.Value + ");");                        
                    } else if (Grammar.IsBuiltIn(transition)) {
                        builder.AppendLine("\t\t\tTransition(Grammar." + Grammar.TryGetBuiltInFieldBySymbol(transition).Name + ", State" + toState.Value + ");");
                    } else {
                        builder.AppendLine("\t\t\tTransition<"+ _namespace + "." + nameMap[transition] + ">(State" + toState.Value + ");");
                    }
                }
            }
        }

        public void Generate(string destinationDirectory, Grammar grammar, String parserName) {
            var nameMap = new AutoDictionary<Grammar.ISymbol, String>(x => CSharpName(x.Name));
            foreach (var production in grammar.Productions) {
                var nfa = production.Reassign();
                var cSharpName = nameMap[production];
                var fileName = destinationDirectory + "/" + cSharpName + ".cs";
                var builder = new StringBuilder();
                builder.AppendLine("using Parlex;");
                builder.AppendLine();
                builder.AppendLine("namespace " + _namespace + " {");
                builder.AppendLine("\tpartial class " + cSharpName + " : SyntaxNode {");
                builder.AppendLine("\t\tpublic override void Start() {");
                foreach (var startState in nfa.StartStates) {
                    builder.AppendLine("\t\t\tState" + startState.Value + "();");
                }
                builder.AppendLine("\t\t}");
                builder.AppendLine();
                foreach (var state in nfa.States) {
                    builder.AppendLine("\t\tvoid State" + state.Value + "() {");
                    if (nfa.AcceptStates.Contains(state)) {
                        builder.AppendLine("\t\t\tAccept();");
                    }
                    OutputTransitions(builder, _namespace, nameMap, nfa, state);
                    builder.AppendLine("\t\t}");
                    builder.AppendLine();
                }
                builder.AppendLine("\t}");
                builder.AppendLine("}");
                System.IO.File.WriteAllText(fileName, builder.ToString());
            }
            var topBuilder = new StringBuilder();
            topBuilder.AppendLine("using Parlex;");
            topBuilder.AppendLine("");
            topBuilder.AppendLine("namespace " + _namespace + "{");
            topBuilder.AppendLine("\tclass " + parserName + " {");

        }
    }
}
