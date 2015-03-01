﻿using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Automata;

namespace Parlex {
    public static class WirthSyntaxNotation {
        private const String PlaceHolderMarker = "6CC3C4B8-33EC-4093-ADB4-418C2BA0E97B ";
        public static readonly Grammar WorthSyntaxNotationParserGrammar;
        private static readonly Grammar.ITerminal EqualsTerminal = new Grammar.StringTerminal("=");
        private static readonly Grammar.ITerminal PeriodTerminal = new Grammar.StringTerminal(".");
        private static readonly Grammar.ITerminal PipeTerminal = new Grammar.StringTerminal("|");
        private static readonly Grammar.ITerminal OpenParenthesisTerminal = new Grammar.StringTerminal("(");
        private static readonly Grammar.ITerminal CloseParenthesisTerminal = new Grammar.StringTerminal(")");
        private static readonly Grammar.ITerminal OpenSquareTerminal = new Grammar.StringTerminal("[");
        private static readonly Grammar.ITerminal CloseSquareTerminal = new Grammar.StringTerminal("]");
        private static readonly Grammar.ITerminal OpenCurlyTerminal = new Grammar.StringTerminal("{");
        private static readonly Grammar.ITerminal CloseCurlyTerminal = new Grammar.StringTerminal("}");
        private static readonly Grammar.ITerminal UnderscoreTerminal = new Grammar.StringTerminal("_");
        private static readonly Grammar.Production Syntax = new Grammar.Production("syntax", true, true);
        private static readonly Grammar.Production Production = new Grammar.Production("production", true, false);
        private static readonly Grammar.Production Expression = new Grammar.Production("expression", true, true);
        private static readonly Grammar.Production Term = new Grammar.Production("term", true, true);
        private static readonly Grammar.Production Factor = new Grammar.Production("factor", true, true);
        private static readonly Grammar.Production Identifier = new Grammar.Production("identifier", true, true);
        private static readonly Grammar.Production Literal = new Grammar.Production("literal", false, true);

        static WirthSyntaxNotation() {
            var syntaxState0 = new Nfa<Grammar.ISymbol>.State();
            var syntaxState1 = new Nfa<Grammar.ISymbol>.State();
            Syntax.States.Add(syntaxState0);
            Syntax.States.Add(syntaxState1);
            Syntax.StartStates.Add(syntaxState0);
            Syntax.AcceptStates.Add(syntaxState0);
            Syntax.AcceptStates.Add(syntaxState1);
            Syntax.TransitionFunction[syntaxState0][Production].Add(syntaxState0);
            Syntax.TransitionFunction[syntaxState0][Grammar.WhiteSpaces].Add(syntaxState1);

            var productionState0 = new Nfa<Grammar.ISymbol>.State();
            var productionState1 = new Nfa<Grammar.ISymbol>.State();
            var productionState2 = new Nfa<Grammar.ISymbol>.State();
            var productionState3 = new Nfa<Grammar.ISymbol>.State();
            var productionState4 = new Nfa<Grammar.ISymbol>.State();
            Production.States.Add(productionState0);
            Production.States.Add(productionState1);
            Production.States.Add(productionState2);
            Production.States.Add(productionState3);
            Production.States.Add(productionState4);
            Production.StartStates.Add(productionState0);
            Production.AcceptStates.Add(productionState4);
            Production.TransitionFunction[productionState0][Identifier].Add(productionState1);
            Production.TransitionFunction[productionState1][Grammar.WhiteSpaces].Add(productionState1);
            Production.TransitionFunction[productionState1][EqualsTerminal].Add(productionState2);
            Production.TransitionFunction[productionState2][Expression].Add(productionState3);
            Production.TransitionFunction[productionState3][Grammar.WhiteSpaces].Add(productionState3);
            Production.TransitionFunction[productionState3][PeriodTerminal].Add(productionState4);

            var expressionState0 = new Nfa<Grammar.ISymbol>.State();
            var expressionState1 = new Nfa<Grammar.ISymbol>.State();
            Expression.States.Add(expressionState0);
            Expression.States.Add(expressionState1);
            Expression.StartStates.Add(expressionState0);
            Expression.AcceptStates.Add(expressionState1);
            Expression.TransitionFunction[expressionState0][Term].Add(expressionState1);
            Expression.TransitionFunction[expressionState1][Grammar.WhiteSpaces].Add(expressionState1);
            Expression.TransitionFunction[expressionState1][PipeTerminal].Add(expressionState0);

            var termState0 = new Nfa<Grammar.ISymbol>.State();
            var termState1 = new Nfa<Grammar.ISymbol>.State();
            Term.States.Add(termState0);
            Term.States.Add(termState1);
            Term.StartStates.Add(termState0);
            Term.AcceptStates.Add(termState1);
            Term.TransitionFunction[termState0][Factor].Add(termState1);
            Term.TransitionFunction[termState1][Factor].Add(termState1);

            var factorState0 = new Nfa<Grammar.ISymbol>.State();
            var factorState1 = new Nfa<Grammar.ISymbol>.State();
            var factorState2 = new Nfa<Grammar.ISymbol>.State();
            var factorState3 = new Nfa<Grammar.ISymbol>.State();
            var factorState4 = new Nfa<Grammar.ISymbol>.State();
            var factorState5 = new Nfa<Grammar.ISymbol>.State();
            var factorState6 = new Nfa<Grammar.ISymbol>.State();
            var factorState7 = new Nfa<Grammar.ISymbol>.State();
            var factorState8 = new Grammar.Production.State();
            Factor.States.Add(factorState0);
            Factor.States.Add(factorState1);
            Factor.States.Add(factorState2);
            Factor.States.Add(factorState3);
            Factor.States.Add(factorState4);
            Factor.States.Add(factorState5);
            Factor.States.Add(factorState6);
            Factor.States.Add(factorState7);
            Factor.States.Add(factorState8);
            Factor.StartStates.Add(factorState0);
            Factor.AcceptStates.Add(factorState1);
            Factor.TransitionFunction[factorState0][Grammar.WhiteSpaces].Add(factorState8);
            Factor.TransitionFunction[factorState0][Identifier].Add(factorState1);
            Factor.TransitionFunction[factorState0][Literal].Add(factorState1);
            Factor.TransitionFunction[factorState0][OpenSquareTerminal].Add(factorState2);
            Factor.TransitionFunction[factorState0][OpenParenthesisTerminal].Add(factorState3);
            Factor.TransitionFunction[factorState0][OpenCurlyTerminal].Add(factorState4);
            Factor.TransitionFunction[factorState8][OpenSquareTerminal].Add(factorState2);
            Factor.TransitionFunction[factorState8][OpenParenthesisTerminal].Add(factorState3);
            Factor.TransitionFunction[factorState8][OpenCurlyTerminal].Add(factorState4);
            Factor.TransitionFunction[factorState2][Expression].Add(factorState5);
            Factor.TransitionFunction[factorState3][Expression].Add(factorState6);
            Factor.TransitionFunction[factorState4][Expression].Add(factorState7);
            Factor.TransitionFunction[factorState5][CloseSquareTerminal].Add(factorState1);
            Factor.TransitionFunction[factorState6][CloseParenthesisTerminal].Add(factorState1);
            Factor.TransitionFunction[factorState7][CloseCurlyTerminal].Add(factorState1);

            var identifierState0 = new Nfa<Grammar.ISymbol>.State();
            var identifierState1 = new Nfa<Grammar.ISymbol>.State();
            Identifier.States.Add(identifierState0);
            Identifier.States.Add(identifierState1);
            Identifier.StartStates.Add(identifierState0);
            Identifier.AcceptStates.Add(identifierState1);
            Identifier.TransitionFunction[identifierState0][Grammar.WhiteSpaces].Add(identifierState0);
            Identifier.TransitionFunction[identifierState0][Grammar.AlphaNumericTerminal].Add(identifierState1);
            Identifier.TransitionFunction[identifierState0][UnderscoreTerminal].Add(identifierState1);
            Identifier.TransitionFunction[identifierState1][Grammar.AlphaNumericTerminal].Add(identifierState1);
            Identifier.TransitionFunction[identifierState1][UnderscoreTerminal].Add(identifierState1);

            var literalState0 = new Nfa<Grammar.ISymbol>.State();
            var literalState1 = new Nfa<Grammar.ISymbol>.State();
            Literal.States.Add(literalState0);
            Literal.States.Add(literalState1);
            Literal.StartStates.Add(literalState0);
            Literal.AcceptStates.Add(literalState1);
            Literal.TransitionFunction[literalState0][Grammar.WhiteSpaces].Add(literalState0);
            Literal.TransitionFunction[literalState0][Grammar.StringLiteral].Add(literalState1);

            WorthSyntaxNotationParserGrammar = new Grammar();
            WorthSyntaxNotationParserGrammar.Productions.Add(Syntax);
            WorthSyntaxNotationParserGrammar.Productions.Add(Production);
            WorthSyntaxNotationParserGrammar.Productions.Add(Expression);
            WorthSyntaxNotationParserGrammar.Productions.Add(Term);
            WorthSyntaxNotationParserGrammar.Productions.Add(Factor);
            WorthSyntaxNotationParserGrammar.Productions.Add(Identifier);
            WorthSyntaxNotationParserGrammar.Productions.Add(Literal);
            WorthSyntaxNotationParserGrammar.MainProduction = Syntax;
        }

        private static string ProcessIdentifierClause(Parser.Job job, Match identifier) {            
            return job.Document.Utf32Substring(identifier.Position, identifier.Length).Trim();
        }

        private static string ProcessLiteralClause(Parser.Job job, Match literal) {
            var result = Grammar.ProcessStringLiteral(job.CodePoints, literal.Position, literal.Length);
            if (result == null) throw new ApplicationException();
            return result;
        }

        private static Grammar.Production GetRecognizerByIdentifierString(Grammar result, String identifierString) {
            foreach (Grammar.Production recognizer in result.Productions) {
                if (recognizer.Name == identifierString.Trim()) {
                    return recognizer;
                }
            }
            var newRecognizer = new Grammar.Production(identifierString.Trim(), false, false);
            result.Productions.Add(newRecognizer);
            return newRecognizer;
        }


        private static Nfa<Grammar.ISymbol> ProcessFactorClause(Parser.Job job, Match factor) {
            Match firstChild = job.AbstractSyntaxGraph.NodeTable[factor.Children[0]].First();

            if (firstChild.Symbol.Is(Identifier)) {
                Grammar.ISymbol transition = new Grammar.Production(PlaceHolderMarker + ProcessIdentifierClause(job, firstChild), false, false);
                var result = new Nfa<Grammar.ISymbol>();
                var state0 = new Nfa<Grammar.ISymbol>.State();
                result.StartStates.Add(state0);
                result.States.Add(state0);
                var state1 = new Nfa<Grammar.ISymbol>.State();
                result.AcceptStates.Add(state1);
                result.States.Add(state1);
                result.TransitionFunction[state0][transition].Add(state1);
                return result;
            }

            if (firstChild.Symbol.Is(Literal)) {
                var transition = new Grammar.StringTerminal(ProcessLiteralClause(job, firstChild));
                var result = new Nfa<Grammar.ISymbol>();
                var state0 = new Nfa<Grammar.ISymbol>.State();
                result.StartStates.Add(state0);
                result.States.Add(state0);
                var state1 = new Nfa<Grammar.ISymbol>.State();
                result.AcceptStates.Add(state1);
                result.States.Add(state1);
                result.TransitionFunction[state0][transition].Add(state1);
                return result;
            }

            if (firstChild.Symbol.Is(OpenSquareTerminal)) {
                Match expression = job.AbstractSyntaxGraph.NodeTable[factor.Children[1]].First();
                Nfa<Grammar.ISymbol> result = ProcessExpressionClause(job, expression);
                foreach (Nfa<Grammar.ISymbol>.State state in result.StartStates) {
                    result.AcceptStates.Add(state); //to make it optional
                }
                return result;
            }

            if (firstChild.Symbol.Is(OpenParenthesisTerminal)) {
                Match expression = job.AbstractSyntaxGraph.NodeTable[factor.Children[1]].First();
                Nfa<Grammar.ISymbol> result = ProcessExpressionClause(job, expression);
                return result;
            }

            if (firstChild.Symbol.Is(OpenCurlyTerminal)) {
                Match expression = job.AbstractSyntaxGraph.NodeTable[factor.Children[1]].First();
                Nfa<Grammar.ISymbol> result = ProcessExpressionClause(job, expression);
                var acceptNothingState = new Nfa<Grammar.ISymbol>.State();
                result.StartStates.Add(acceptNothingState);
                result.AcceptStates.Add(acceptNothingState);
                foreach (Nfa<Grammar.ISymbol>.State startState in result.StartStates) {
                    foreach (Grammar.ISymbol symbol in result.TransitionFunction[startState].Keys) {
                        foreach (Nfa<Grammar.ISymbol>.State toState in result.TransitionFunction[startState][symbol]) {
                            foreach (Nfa<Grammar.ISymbol>.State acceptState in result.AcceptStates) {
                                result.TransitionFunction[acceptState][symbol].Add(toState); //to make it repeatable
                            }
                        }
                    }
                }
                result = result.Minimized();
                return result;
            }

            throw new InvalidOperationException();
        }

        private static Nfa<Grammar.ISymbol> ProcessTermClause(Parser.Job job, Match term) {
            var result = new Nfa<Grammar.ISymbol>();
            var state = new Nfa<Grammar.ISymbol>.State();
            result.StartStates.Add(state);
            result.States.Add(state);
            result.AcceptStates.Add(state);
            foreach (MatchClass matchClass in term.Children) {
                if (matchClass.Symbol == Grammar.WhiteSpaceTerminal) {
                    continue;
                }
                Match factor = job.AbstractSyntaxGraph.NodeTable[matchClass].First();
                Nfa<Grammar.ISymbol> factorNfa = ProcessFactorClause(job, factor);
                result.Append(factorNfa);
                var temp = result.Minimized();
                result = temp;
            }
            return result;
        }

        private static Nfa<Grammar.ISymbol> ProcessExpressionClause(Parser.Job job, Match expression) {
            var result = new Nfa<Grammar.ISymbol>();

            for (int index = 0; index < expression.Children.Length; index += 2) {
                Nfa<Grammar.ISymbol> termNfa = ProcessTermClause(job, job.AbstractSyntaxGraph.NodeTable[expression.Children[index]].First());
                result = Nfa<Grammar.ISymbol>.Union(new[] {result, termNfa});
            }

            return result.Minimized();
        }

        private static Grammar.Production ProcessProductionClause(Parser.Job job, Match production) {
            string name = ProcessIdentifierClause(job, job.AbstractSyntaxGraph.NodeTable[production.Children[0]].First()).Trim();
            Nfa<Grammar.ISymbol> nfa = ProcessExpressionClause(job, job.AbstractSyntaxGraph.NodeTable[production.Children[2]].First());
            return new Grammar.Production(name, false, nfa);
        }

        private static void ProcessSyntaxClause(Parser.Job job, Match syntax, Grammar result) {
            foreach (MatchClass matchClass in syntax.Children) {
                if (matchClass.Symbol == Grammar.WhiteSpaceTerminal) {
                    continue;
                }
                Grammar.Production production = ProcessProductionClause(job, job.AbstractSyntaxGraph.NodeTable[matchClass].First());
                result.Productions.Add(production);
            }
        }

        private static void ResolveIdentifiers(Grammar grammar) {
            foreach (Grammar.Production recognizer in grammar.Productions) {
                foreach (Nfa<Grammar.ISymbol>.State fromState in recognizer.States) {
                    var toRemoves = new List<Grammar.ISymbol>();
                    var toAdds = new AutoDictionary<Grammar.ISymbol, List<Nfa<Grammar.ISymbol>.State>>(_ => new List<Nfa<Grammar.ISymbol>.State>());
                    foreach (Grammar.ISymbol symbol in recognizer.TransitionFunction[fromState].Keys) {
                        var symbolAsRecognizer = symbol as Grammar.Production;
                        if (symbolAsRecognizer != null) {
                            if (symbolAsRecognizer.Name.StartsWith(PlaceHolderMarker)) {
                                toRemoves.Add(symbol);
                                string deMarkedName = symbolAsRecognizer.Name.Substring(PlaceHolderMarker.Length);
                                Grammar.ISymbol resolved;
                                if (!Grammar.TryGetBuiltinISymbolByName(deMarkedName, out resolved)) {
                                    resolved =
                                        grammar.GetRecognizerByName(deMarkedName) ??
                                        new Grammar.Production(deMarkedName, false, false);
                                }
                                foreach (Nfa<Grammar.ISymbol>.State toState in recognizer.TransitionFunction[fromState][symbol]) {
                                    toAdds[resolved].Add(toState);
                                }
                            }
                        }
                    }
                    foreach (Grammar.ISymbol toRemove in toRemoves) {
                        recognizer.TransitionFunction[fromState].TryRemove(toRemove);
                    }
                    foreach (Grammar.ISymbol symbol in toAdds.Keys) {
                        foreach (Nfa<Grammar.ISymbol>.State toState in toAdds[symbol]) {
                            recognizer.TransitionFunction[fromState][symbol].Add(toState);
                        }
                    }
                }
            }
        }

        public static Grammar GrammarFromString(String text) {
            Parser parser = new Parser(WorthSyntaxNotationParserGrammar);
            Parser.Job j = parser.Parse(text);
            j.Join();
            var asg = j.AbstractSyntaxGraph;
            asg.StripWhiteSpaceEaters();
            if (asg.IsAmbiguous) {
                throw new InvalidOperationException("The given metagrammar resulted in an ambiguous parse tree");
            }
            var result = new Grammar();
            ProcessSyntaxClause(j, j.AbstractSyntaxGraph.NodeTable[j.AbstractSyntaxGraph.Root].First(), result);
            ResolveIdentifiers(result);
            result.MainProduction = result.GetRecognizerByName("SYNTAX");
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

        private static String BehaviorTreeRepetitionToString(BehaviorTree.Repetition repetition) {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append(BehaviorTreeNodeToString(repetition.Child));
            sb.Append("}");
            return sb.ToString();
        }

        private static String BehaviorTreeTerminalToString(BehaviorTree.Leaf leaf) {
            string temp = leaf.Symbol.ToString();
            if (leaf.Symbol is Grammar.StringTerminal) {
                if (temp == "\"") {
                    temp = "doubleQuote";
                } else {
                    temp = Grammar.QuoteStringLiteral(temp);
                }
            }
            String temp2 = Grammar.TryGetBuiltInNameBySymbol(leaf.Symbol);
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
            throw new InvalidOperationException();
        }

        private static void AppendRecognizer(StringBuilder builder, Grammar.Production production) {
            builder.Append(production.Name);
            builder.Append("=");
            var bht = new BehaviorTree(production);
            builder.Append(BehaviorTreeNodeToString(bht.Root));
            builder.AppendLine(".");
        }

        public static String GrammarToString(Grammar grammar) {
            var builder = new StringBuilder();
            foreach (Grammar.Production recognizer in grammar.Productions) {
                AppendRecognizer(builder, recognizer);
            }
            return builder.ToString();
        }

        public class Formatter : IGrammarFormatter {
            public void Serialize(Stream s, Grammar grammar) {
                string text = GrammarToString(grammar);
                using (var sw = new StreamWriter(s, new UTF8Encoding(true))) {
                    sw.Write(text);
                }
            }

            public Grammar Deserialize(Stream s) {
                var sr = new StreamReader(s, Encoding.UTF8);
                string text = sr.ReadToEnd();
                sr.Dispose();
                return GrammarFromString(text);
            }
        }
    }
}