using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Common;
using Parlex.Annotations;

namespace Parlex {
    public static class WirthSyntaxNotation {
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

        public static String GrammarToString(Grammar grammar) {
            if (grammar == null) {
                throw new ArgumentNullException("grammar");
            }
            var builder = new StringBuilder();
            foreach (var recognizer in grammar.Productions) {
                AppendRecognizer(builder, recognizer);
            }
            return builder.ToString();
        }

        private static void AppendRecognizer(StringBuilder builder, Production production) {
            builder.Append(production.Name);
            builder.Append("=");
            builder.Append(BehaviorTreeNodeToString(production.Behavior.Root));
            builder.AppendLine(".");
        }

       private static string ProcessIdentifierClause(Job job, Match identifier) {
            return job.Document.Utf32Substring(identifier.Position, identifier.Length).Trim();
        }

        private static string ProcessLiteralClause(Job job, Match literal) {
            var result = Utilities.ProcessStringLiteral(job.CodePoints, literal.Position, literal.Length);
            if (result == null) throw new ApplicationException();
            return result;
        }

        private static BehaviorNode ProcessFactorClause(Job job, Match factor) {
            Match firstChild = job.AbstractSyntaxGraph.NodeTable[factor.Children[0]].First();

            if (firstChild.Recognizer == WirthSyntaxNotationGrammar.Identifier) {
                var name = ProcessIdentifierClause(job, firstChild);
                Recognizer builtIn;
                return new BehaviorLeaf(StandardSymbols.TryGetBuiltInISymbolByName(name, out builtIn) ? builtIn : new PlaceholderProduction(name));
            }

            if (firstChild.Recognizer == WirthSyntaxNotationGrammar.Literal) {
                return new BehaviorLeaf(new StringTerminal(ProcessLiteralClause(job, firstChild)));
            }

            if (firstChild.Recognizer == WirthSyntaxNotationGrammar.OpenSquareTerminalDefinition) {
                Match expression = job.AbstractSyntaxGraph.NodeTable[factor.Children[1]].First();
                return new Optional {Child = ProcessExpressionClause(job, expression)};
            }

            if (firstChild.Recognizer == WirthSyntaxNotationGrammar.OpenParenthesisTerminalDefinition) {
                Match expression = job.AbstractSyntaxGraph.NodeTable[factor.Children[1]].First();
                return ProcessExpressionClause(job, expression);
            }

            if (firstChild.Recognizer == WirthSyntaxNotationGrammar.OpenCurlyTerminalDefinition) {
                Match expression = job.AbstractSyntaxGraph.NodeTable[factor.Children[1]].First();
                return new RepetitionBehavior {Child = ProcessExpressionClause(job, expression)};
            }

            throw new InvalidOperationException();
        }

        private static BehaviorNode ProcessTermClause(Job job, Match term) {
            var result = new SequenceBehavior();
            foreach (MatchClass matchClass in term.Children) {
                if (matchClass.Recognizer == StandardSymbols.WhiteSpace) {
                    continue;
                }
                Match factor = job.AbstractSyntaxGraph.NodeTable[matchClass].First();
                var child = ProcessFactorClause(job, factor);
                result.Children.Add(child);
            }
            return result;
        }

        private static BehaviorNode ProcessExpressionClause(Job job, Match expression) {
            var result = new ChoiceBehavior();

            for (var index = 0; index < expression.Children.Count; index += 2) {
                var child = ProcessTermClause(job, job.AbstractSyntaxGraph.NodeTable[expression.Children[index]].First());
                result.Children.Add(child);
            }

            return result;
        }

        private static Production ProcessProductionClause(Job job, Match production) {
            string name = ProcessIdentifierClause(job, job.AbstractSyntaxGraph.NodeTable[production.Children[0]].First()).Trim();
            var root = ProcessExpressionClause(job, job.AbstractSyntaxGraph.NodeTable[production.Children[2]].First());
            root.Optimize();
            return new Production(name) {Behavior = new BehaviorTree {Root = root}};
        }

        private static List<Production> ProcessSyntaxClause(Job job, Match syntax) {
            var results = new List<Production>();
            foreach (MatchClass matchClass in syntax.Children) {
                if (matchClass.Recognizer == StandardSymbols.WhiteSpace) {
                    continue;
                }
                results.Add(ProcessProductionClause(job, job.AbstractSyntaxGraph.NodeTable[matchClass].First()));
            }
            return results;
        }

        [UsedImplicitly]
        private static String BehaviorTreeNodeToString(SequenceBehavior sequenceBehavior) {
            var sb = new StringBuilder();
            BehaviorNode[] temp = sequenceBehavior.Children.ToArray();
            for (int i = 0; i < temp.Length; ++i) {
                bool parenthesetize = !(temp[i] is BehaviorLeaf || temp[i] is RepetitionBehavior);
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

        [UsedImplicitly]
        private static String BehaviorTreeNodeToString(ChoiceBehavior choiceBehavior) {
            var sb = new StringBuilder();
            BehaviorNode[] temp = choiceBehavior.Children.ToArray();
            for (int i = 0; i < temp.Length; ++i) {
                bool parenthesetize = !(temp[i] is BehaviorLeaf || temp[i] is RepetitionBehavior);
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

        [UsedImplicitly]
        private static String BehaviorTreeNodeToString(Optional repetition) {
            var sb = new StringBuilder();
            sb.Append("[");
            sb.Append(BehaviorTreeNodeToString(repetition.Child));
            sb.Append("]");
            return sb.ToString();
        }

        [UsedImplicitly]
        private static String BehaviorTreeNodeToString(RepetitionBehavior repetitionBehavior) {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append(BehaviorTreeNodeToString(repetitionBehavior.Child));
            sb.Append("}");
            return sb.ToString();
        }

        [UsedImplicitly]
        private static String BehaviorTreeNodeToString(BehaviorLeaf behaviorLeaf) {
            string temp = behaviorLeaf.Recognizer.Name;
            var asStringTerminal = behaviorLeaf.Recognizer as StringTerminal;
            if (asStringTerminal != null) {
                temp = Utilities.QuoteStringLiteral(asStringTerminal.Text);
            }
            String temp2 = StandardSymbols.TryGetBuiltInNameBySymbol(behaviorLeaf.Recognizer);
            if (temp2 != null) temp = temp2;
            return temp;
        }

        private static DynamicDispatcher _behaviorTreeNodeToStringDispatcher;
        private static String BehaviorTreeNodeToString(BehaviorNode behaviorNode) {
            if (_behaviorTreeNodeToStringDispatcher == null) {
                _behaviorTreeNodeToStringDispatcher = new DynamicDispatcher();
            }
            return _behaviorTreeNodeToStringDispatcher.Dispatch<String>(behaviorNode);
        }

    }
}