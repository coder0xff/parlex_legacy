using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Parlex {
    public static class WirthSyntaxNotation {
       private static string ProcessIdentifierClause(Parser.Job job, Match identifier) {
            return job.Document.Utf32Substring(identifier.Position, identifier.Length).Trim();
        }

        private static string ProcessLiteralClause(Parser.Job job, Match literal) {
            var result = Util.ProcessStringLiteral(job.CodePoints, literal.Position, literal.Length);
            if (result == null) throw new ApplicationException();
            return result;
        }

        private static BehaviorTree.Node ProcessFactorClause(Parser.Job job, Match factor) {
            Match firstChild = job.AbstractSyntaxGraph.NodeTable[factor.Children[0]].First();

            if (firstChild.Recognizer == WirthSyntaxNotationGrammar.Identifier) {
                var name = ProcessIdentifierClause(job, firstChild);
                Recognizer builtIn;
                return new BehaviorTree.Leaf(StandardSymbols.TryGetBuiltinISymbolByName(name, out builtIn) ? builtIn : new PlaceholderRecognizer(name));
            }

            if (firstChild.Recognizer == WirthSyntaxNotationGrammar.Literal) {
                return new BehaviorTree.Leaf(new StringTerminal(ProcessLiteralClause(job, firstChild)));
            }

            if (firstChild.Recognizer == WirthSyntaxNotationGrammar.OpenSquareTerminalDefinition) {
                Match expression = job.AbstractSyntaxGraph.NodeTable[factor.Children[1]].First();
                return new BehaviorTree.Optional {Child = ProcessExpressionClause(job, expression)};
            }

            if (firstChild.Recognizer == WirthSyntaxNotationGrammar.OpenParenthesisTerminalDefinition) {
                Match expression = job.AbstractSyntaxGraph.NodeTable[factor.Children[1]].First();
                return ProcessExpressionClause(job, expression);
            }

            if (firstChild.Recognizer == WirthSyntaxNotationGrammar.OpenCurlyTerminalDefinition) {
                Match expression = job.AbstractSyntaxGraph.NodeTable[factor.Children[1]].First();
                return new BehaviorTree.Repetition {Child = ProcessExpressionClause(job, expression)};
            }

            throw new InvalidOperationException();
        }

        private static BehaviorTree.Node ProcessTermClause(Parser.Job job, Match term) {
            var result = new BehaviorTree.Sequence();
            foreach (MatchClass matchClass in term.Children) {
                if (matchClass.Recognizer == StandardSymbols.WhiteSpaceTerminalDefinition) {
                    continue;
                }
                Match factor = job.AbstractSyntaxGraph.NodeTable[matchClass].First();
                var child = ProcessFactorClause(job, factor);
                result.Children.Add(child);
            }
            return result;
        }

        private static BehaviorTree.Node ProcessExpressionClause(Parser.Job job, Match expression) {
            var result = new BehaviorTree.Choice();

            for (var index = 0; index < expression.Children.Length; index += 2) {
                var child = ProcessTermClause(job, job.AbstractSyntaxGraph.NodeTable[expression.Children[index]].First());
                result.Children.Add(child);
            }

            return result;
        }

        private static Production ProcessProductionClause(Parser.Job job, Match production) {
            string name = ProcessIdentifierClause(job, job.AbstractSyntaxGraph.NodeTable[production.Children[0]].First()).Trim();
            var root = ProcessExpressionClause(job, job.AbstractSyntaxGraph.NodeTable[production.Children[2]].First());
            root.Optimize();
            return new Production(name) {Behavior = new BehaviorTree {Root = root}};
        }

        private static List<Production> ProcessSyntaxClause(Parser.Job job, Match syntax) {
            var results = new List<Production>();
            foreach (MatchClass matchClass in syntax.Children) {
                if (matchClass.Recognizer == StandardSymbols.WhiteSpaceTerminalDefinition) {
                    continue;
                }
                results.Add(ProcessProductionClause(job, job.AbstractSyntaxGraph.NodeTable[matchClass].First()));
            }
            return results;
        }

        public static Grammar GrammarFromString(String text) {
            var parser = new Parser(WirthSyntaxNotationGrammar.NfaGrammar);
            var j = parser.Parse(text);
            j.Join();
            var asg = j.AbstractSyntaxGraph;
            asg.StripWhiteSpaceEaters();
            if (asg.IsAmbiguous) {
                throw new ParseException("The given metagrammar resulted in an ambiguous parse tree");
            }
            if (j.AbstractSyntaxGraph.IsEmpty) {
                throw new ParseException("The document's syntax was incorrect.");
            }
            var result = new Grammar();
            foreach (var production in ProcessSyntaxClause(j, j.AbstractSyntaxGraph.NodeTable[j.AbstractSyntaxGraph.Root].First())) {
                result.Productions.Add(production);
            }
            result.Resolve();
            result.Main = result.GetProduction("SYNTAX");
            return result;
        }

        private static String BehaviorTreeSequenceToString(BehaviorTree.Sequence sequence) {
            var sb = new StringBuilder();
            BehaviorTree.Node[] temp = sequence.Children.ToArray();
            for (int i = 0; i < temp.Length; ++i) {
                bool parenthesetize = !(temp[i] is BehaviorTree.Leaf || temp[i] is BehaviorTree.Repetition);
                if (parenthesetize) {
                    sb.Append("(");
                }
                sb.Append(BehaviorTreeNodeToString(temp[i]));
                if (parenthesetize) {
                    sb.Append(")");
                }
                if (i != temp.Length - 1) {
                    sb.Append(" ");
                }
            }
            return sb.ToString();
        }

        private static String BehaviorTreeChoiceToString(BehaviorTree.Choice choice) {
            var sb = new StringBuilder();
            BehaviorTree.Node[] temp = choice.Children.ToArray();
            for (int i = 0; i < temp.Length; ++i) {
                bool parenthesetize = !(temp[i] is BehaviorTree.Leaf || temp[i] is BehaviorTree.Repetition);
                if (parenthesetize) {
                    sb.Append("(");
                }
                sb.Append(BehaviorTreeNodeToString(temp[i]));
                if (parenthesetize) {
                    sb.Append(")");
                }
                if (i < temp.Length - 1) {
                    sb.Append("|");
                }
            }
            return sb.ToString();
        }

        private static String BehaviorTreeOptionalToString(BehaviorTree.Optional repetition) {
            var sb = new StringBuilder();
            sb.Append("[");
            sb.Append(BehaviorTreeNodeToString(repetition.Child));
            sb.Append("]");
            return sb.ToString();
        }

        private static String BehaviorTreeRepetitionToString(BehaviorTree.Repetition repetition) {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append(BehaviorTreeNodeToString(repetition.Child));
            sb.Append("}");
            return sb.ToString();
        }

        private static String BehaviorTreeTerminalToString(BehaviorTree.Leaf leaf) {
            string temp = leaf.Recognizer.Name;
            var asStringTerminal = leaf.Recognizer as StringTerminal;
            if (asStringTerminal != null) {
                temp = Util.QuoteStringLiteral(asStringTerminal.Text);
            }
            String temp2 = StandardSymbols.TryGetBuiltInNameBySymbol(leaf.Recognizer);
            if (temp2 != null) temp = temp2;
            return temp;
        }

        private static String BehaviorTreeNodeToString(BehaviorTree.Node node) {
            if (node is BehaviorTree.Sequence) {
                return BehaviorTreeSequenceToString(node as BehaviorTree.Sequence);
            }
            if (node is BehaviorTree.Choice) {
                return BehaviorTreeChoiceToString(node as BehaviorTree.Choice);
            }
            if (node is BehaviorTree.Repetition) {
                return BehaviorTreeRepetitionToString(node as BehaviorTree.Repetition);
            }
            if (node is BehaviorTree.Leaf) {
                return BehaviorTreeTerminalToString(node as BehaviorTree.Leaf);
            }
            if (node is BehaviorTree.Optional) {
                return BehaviorTreeOptionalToString(node as BehaviorTree.Optional);
            }
            throw new InvalidOperationException();
        }

        private static void AppendRecognizer(StringBuilder builder, Production production) {
            builder.Append(production.Name);
            builder.Append("=");
            builder.Append(BehaviorTreeNodeToString(production.Behavior.Root));
            builder.AppendLine(".");
        }

        public static String GrammarToString(Grammar grammar) {
            var builder = new StringBuilder();
            foreach (var recognizer in grammar.Productions) {
                AppendRecognizer(builder, recognizer);
            }
            return builder.ToString();
        }

        public class Formatter : IMetaSyntax {
            public void Generate(Stream s, Grammar grammar) {
                string text = GrammarToString(grammar);
                using (var sw = new StreamWriter(s, new UTF8Encoding(true))) {
                    sw.Write(text);
                }
            }

            public Grammar Parse(Stream s) {
                var sr = new StreamReader(s, Encoding.UTF8);
                string text = sr.ReadToEnd();
                sr.Dispose();
                return GrammarFromString(text);
            }
        }
    }
}