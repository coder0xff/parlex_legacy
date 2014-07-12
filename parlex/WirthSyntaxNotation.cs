using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Linq;
using NondeterministicFiniteAutomata;

namespace Parlex {
    public class WirthSyntaxNotation {
        static Grammar worthSyntaxNotationParserGrammar;
        static Grammar.ITerminal equalsTerminal = new Grammar.StringTerminal("=");
        static Grammar.ITerminal periodTerminal = new Grammar.StringTerminal(".");
        static Grammar.ITerminal pipeTerminal = new Grammar.StringTerminal("|");
        static Grammar.ITerminal openParenthesisTerminal = new Grammar.StringTerminal("(");
        static Grammar.ITerminal closeParenthesisTerminal = new Grammar.StringTerminal(")");
        static Grammar.ITerminal openSquareTerminal = new Grammar.StringTerminal("[");
        static Grammar.ITerminal closeSquareTerminal = new Grammar.StringTerminal("]");
        static Grammar.ITerminal openCurlyTerminal = new Grammar.StringTerminal("{");
        static Grammar.ITerminal closeCurlyTerminal = new Grammar.StringTerminal("}");
        static Grammar.ITerminal doubleQuoteTerminal = new Grammar.StringTerminal("\"");
        static Grammar.CharacterSet notDoubleQuoteCharacterSet = new Grammar.CharacterSet("notDoubleQuotes", Unicode.All.Except(new Int32[] { Char.ConvertToUtf32("\"", 0) }));
        static Grammar.Recognizer syntax = new Grammar.Recognizer("syntax", true);
        static Grammar.Recognizer production = new Grammar.Recognizer("production", false);
        static Grammar.Recognizer expression = new Grammar.Recognizer("expression", false);
        static Grammar.Recognizer term = new Grammar.Recognizer("term", false);
        static Grammar.Recognizer factor = new Grammar.Recognizer("factor", false);
        static Grammar.Recognizer identifier = new Grammar.Recognizer("identifier", true);
        static Grammar.Recognizer literal = new Grammar.Recognizer("literal", false);
        static String placeHolderMarker = "6CC3C4B8-33EC-4093-ADB4-418C2BA0E97B ";

        static WirthSyntaxNotation() {

            var syntaxState0 = new NFA<Grammar.ISymbol>.State();
            syntax.States.Add(syntaxState0);
            syntax.StartStates.Add(syntaxState0);
            syntax.AcceptStates.Add(syntaxState0);
            syntax.TransitionFunction[syntaxState0][production].Add(syntaxState0);

            var productionState0 = new NFA<Grammar.ISymbol>.State();
            var productionState1 = new NFA<Grammar.ISymbol>.State();
            var productionState2 = new NFA<Grammar.ISymbol>.State();
            var productionState3 = new NFA<Grammar.ISymbol>.State();
            var productionState4 = new NFA<Grammar.ISymbol>.State();
            production.States.Add(productionState0);
            production.States.Add(productionState1);
            production.States.Add(productionState2);
            production.States.Add(productionState3);
            production.States.Add(productionState4);
            production.StartStates.Add(productionState0);
            production.AcceptStates.Add(productionState4);
            production.TransitionFunction[productionState0][identifier].Add(productionState1);
            production.TransitionFunction[productionState1][equalsTerminal].Add(productionState2);
            production.TransitionFunction[productionState2][expression].Add(productionState3);
            production.TransitionFunction[productionState3][periodTerminal].Add(productionState4);

            var expressionState0 = new NFA<Grammar.ISymbol>.State();
            var expressionState1 = new NFA<Grammar.ISymbol>.State();
            expression.States.Add(expressionState0);
            expression.States.Add(expressionState1);
            expression.StartStates.Add(expressionState0);
            expression.AcceptStates.Add(expressionState1);
            expression.TransitionFunction[expressionState0][term].Add(expressionState1);
            expression.TransitionFunction[expressionState1][pipeTerminal].Add(expressionState0);

            var termState0 = new NFA<Grammar.ISymbol>.State();
            var termState1 = new NFA<Grammar.ISymbol>.State();
            term.States.Add(termState0);
            term.States.Add(termState1);
            term.StartStates.Add(termState0);
            term.AcceptStates.Add(termState1);
            term.TransitionFunction[termState0][factor].Add(termState1);
            term.TransitionFunction[termState1][factor].Add(termState1);

            var factorState0 = new NFA<Grammar.ISymbol>.State();
            var factorState1 = new NFA<Grammar.ISymbol>.State();
            var factorState2 = new NFA<Grammar.ISymbol>.State();
            var factorState3 = new NFA<Grammar.ISymbol>.State();
            var factorState4 = new NFA<Grammar.ISymbol>.State();
            var factorState5 = new NFA<Grammar.ISymbol>.State();
            var factorState6 = new NFA<Grammar.ISymbol>.State();
            var factorState7 = new NFA<Grammar.ISymbol>.State();
            factor.States.Add(factorState0);
            factor.States.Add(factorState1);
            factor.States.Add(factorState2);
            factor.States.Add(factorState3);
            factor.StartStates.Add(factorState0);
            factor.AcceptStates.Add(factorState1);
            factor.TransitionFunction[factorState0][identifier].Add(factorState1);
            factor.TransitionFunction[factorState0][literal].Add(factorState1);
            factor.TransitionFunction[factorState0][openSquareTerminal].Add(factorState2);
            factor.TransitionFunction[factorState0][openParenthesisTerminal].Add(factorState3);
            factor.TransitionFunction[factorState0][openCurlyTerminal].Add(factorState4);
            factor.TransitionFunction[factorState2][expression].Add(factorState5);
            factor.TransitionFunction[factorState3][expression].Add(factorState6);
            factor.TransitionFunction[factorState4][expression].Add(factorState7);
            factor.TransitionFunction[factorState5][closeSquareTerminal].Add(factorState1);
            factor.TransitionFunction[factorState6][closeParenthesisTerminal].Add(factorState1);
            factor.TransitionFunction[factorState7][closeCurlyTerminal].Add(factorState1);

            var identifierState0 = new NFA<Grammar.ISymbol>.State();
            var identifierState1 = new NFA<Grammar.ISymbol>.State();
            identifier.States.Add(identifierState0);
            identifier.States.Add(identifierState1);
            identifier.StartStates.Add(identifierState0);
            identifier.AcceptStates.Add(identifierState1);
            identifier.TransitionFunction[identifierState0][Grammar.LetterTerminal].Add(identifierState1);
            identifier.TransitionFunction[identifierState1][Grammar.LetterTerminal].Add(identifierState1);

            var literalState0 = new NFA<Grammar.ISymbol>.State();
            var literalState1 = new NFA<Grammar.ISymbol>.State();
            var literalState2 = new NFA<Grammar.ISymbol>.State();
            var literalState3 = new NFA<Grammar.ISymbol>.State();
            literal.States.Add(literalState0);
            literal.States.Add(literalState1);
            literal.States.Add(literalState2);
            literal.States.Add(literalState3);
            literal.StartStates.Add(literalState0);
            literal.AcceptStates.Add(literalState3);

            literal.TransitionFunction[literalState0][doubleQuoteTerminal].Add(literalState1);
            literal.TransitionFunction[literalState1][notDoubleQuoteCharacterSet].Add(literalState2);
            literal.TransitionFunction[literalState2][notDoubleQuoteCharacterSet].Add(literalState2);
            literal.TransitionFunction[literalState2][doubleQuoteTerminal].Add(literalState3);

            worthSyntaxNotationParserGrammar = new Grammar();
            worthSyntaxNotationParserGrammar.Productions.Add(syntax);
            worthSyntaxNotationParserGrammar.Productions.Add(production);
            worthSyntaxNotationParserGrammar.Productions.Add(expression);
            worthSyntaxNotationParserGrammar.Productions.Add(term);
            worthSyntaxNotationParserGrammar.Productions.Add(factor);
            worthSyntaxNotationParserGrammar.Productions.Add(identifier);
            worthSyntaxNotationParserGrammar.Productions.Add(literal);
            worthSyntaxNotationParserGrammar.MainProduction = syntax;
        }

        static string ProcessIdentifierClause(Parser.Job job, Parser.Match identifier) {
            return job.Text.Substring(identifier.Position, identifier.Length);
        }

        static string ProcessLiteralClause(Parser.Job job, Parser.Match literal) {
            return job.Text.Substring(literal.Position + 1, literal.Length - 2); //remove quotes
        }

        static Grammar.Recognizer GetRecognizerByIdentifierString(Grammar result, String identifierString) {
            foreach (var recognizer in result.Productions) {
                if (recognizer.Name == identifierString.Trim()) return recognizer;
            }
            var newRecognizer = new Grammar.Recognizer(identifierString.Trim(), false);
            result.Productions.Add(newRecognizer);
            return newRecognizer;
        }


        static NFA<Grammar.ISymbol> ProcessFactorClause(Parser.Job job, Parser.Match factor) {
            var firstChild = job.AbstractSyntaxForest.NodeTable[factor.Children[0]].First();

            if (firstChild.Symbol == identifier) {
                Grammar.ISymbol transition = new Grammar.Recognizer(placeHolderMarker + ProcessIdentifierClause(job, firstChild), false);
                var result = new NFA<Grammar.ISymbol>();
                var state0 = new NFA<Grammar.ISymbol>.State();
                result.StartStates.Add(state0);
                result.States.Add(state0);
                var state1 = new NFA<Grammar.ISymbol>.State();
                result.AcceptStates.Add(state1);
                result.States.Add(state1);
                result.TransitionFunction[state0][transition].Add(state1);
                return result;
            }

            if (firstChild.Symbol == literal) {
                var transition = new Grammar.StringTerminal(ProcessLiteralClause(job, firstChild));
                var result = new NFA<Grammar.ISymbol>();
                var state0 = new NFA<Grammar.ISymbol>.State();
                result.StartStates.Add(state0);
                result.States.Add(state0);
                var state1 = new NFA<Grammar.ISymbol>.State();
                result.AcceptStates.Add(state1);
                result.States.Add(state1);
                result.TransitionFunction[state0][transition].Add(state1);
                return result;
            }

            if (firstChild.Symbol == openSquareTerminal) {
                var expression = job.AbstractSyntaxForest.NodeTable[factor.Children[1]].First();
                var result = ProcessExpressionClause(job, expression, "");
                foreach (var state in result.StartStates) {
                    result.AcceptStates.Add(state); //to make it optional
                }
                return result;
            }

            if (firstChild.Symbol == openParenthesisTerminal) {
                var expression = job.AbstractSyntaxForest.NodeTable[factor.Children[1]].First();
                var result = ProcessExpressionClause(job, expression, "");
                return result;
            }

            if (firstChild.Symbol == openCurlyTerminal) {
                var expression = job.AbstractSyntaxForest.NodeTable[factor.Children[1]].First();
                var result = ProcessExpressionClause(job, expression, "");
                foreach (var startState in result.StartStates) {
                    foreach (var symbol in result.TransitionFunction[startState].Keys) {
                        foreach (var toState in result.TransitionFunction[startState][symbol]) {
                            foreach (var acceptState in result.AcceptStates) {
                                result.TransitionFunction[acceptState][symbol].Add(toState); //to make it repeatable
                            }
                        }
                    }
                }
                return result;
            }

            throw new InvalidOperationException();
        }

        static NFA<Grammar.ISymbol> ProcessTermClause(Parser.Job job, Parser.Match term) {
            var result = new NFA<Grammar.ISymbol>();
            var state = new NFA<Grammar.ISymbol>.State();
            result.StartStates.Add(state);
            result.States.Add(state);
            result.AcceptStates.Add(state);
            foreach (var matchClass in term.Children) {
                var factor = job.AbstractSyntaxForest.NodeTable[matchClass].First();
                var factorNFA = ProcessFactorClause(job, factor);
                result.Insert(result.AcceptStates.First(), factorNFA);
                result = result.Determinize().Reassign();
            }
            return result;
        }

        static Grammar.Recognizer ProcessExpressionClause(Parser.Job job, Parser.Match expression, String recognizerName) {
            var builder = new NFA<Grammar.ISymbol>();

            for (var index = 0; index < expression.Children.Length; index += 2) {
                var termNFA = ProcessTermClause(job, job.AbstractSyntaxForest.NodeTable[expression.Children[index]].First());
                builder = NFA<Grammar.ISymbol>.Union(new NFA<Grammar.ISymbol>[] { builder, termNFA });
            }

            return new Grammar.Recognizer(recognizerName, false, builder);
        }

        static Grammar.Recognizer ProcessProductionClause(Parser.Job job, Parser.Match production) {
            var name = ProcessIdentifierClause(job, job.AbstractSyntaxForest.NodeTable[production.Children[0]].First());
            return ProcessExpressionClause(job, job.AbstractSyntaxForest.NodeTable[production.Children[2]].First(), name);
        }

        static void ProcessSyntaxClause(Parser.Job job, Parser.Match syntax, Grammar result) {
            foreach (var matchClass in syntax.Children) {
                var recognizer = ProcessProductionClause(job, job.AbstractSyntaxForest.NodeTable[matchClass].First());
                result.Productions.Add(recognizer);
            }
        }

        static void ResolveIdentifiers(Grammar grammar) {
            foreach (var recognizer in grammar.Productions) {
                foreach (var fromState in recognizer.States) {
                    var toRemoves = new List<Grammar.ISymbol>();
                    var toAdds = new AutoDictionary<Grammar.ISymbol, List<NFA<Grammar.ISymbol>.State>>(_ => new List<NFA<Grammar.ISymbol>.State>());
                    foreach (var symbol in recognizer.TransitionFunction[fromState].Keys) {
                        var symbolAsRecognizer = symbol as Grammar.Recognizer;
                        if (symbolAsRecognizer != null && symbolAsRecognizer.Name.StartsWith(placeHolderMarker)) {
                            toRemoves.Add(symbol);
                            Grammar.ISymbol resolved = grammar.GetRecognizerByName(symbolAsRecognizer.Name.Substring(placeHolderMarker.Length));
                            if (resolved == null) {
                                resolved = new Grammar.Recognizer(symbolAsRecognizer.Name.Substring(placeHolderMarker.Length), false);
                            }
                            foreach (var toState in recognizer.TransitionFunction[fromState][symbol]) {
                                toAdds[resolved].Add(toState);
                            }
                        }
                    }
                    foreach (var toRemove in toRemoves) {
                        recognizer.TransitionFunction[fromState].TryRemove(toRemove);
                    }
                    foreach (var symbol in toAdds.Keys) {
                        foreach (var toState in toAdds[symbol]) {
                            recognizer.TransitionFunction[fromState][symbol].Add(toState);
                        }
                    }
                }
            }
        }

        public static Grammar LoadGrammar(String text) {
            var worthSyntaxNotationParser = new Parser(worthSyntaxNotationParserGrammar);
            var j = worthSyntaxNotationParser.Parse(text);
            j.Wait();
            var asg = j.AbstractSyntaxForest;
            if (asg.IsAmbiguous) {
                throw new InvalidOperationException("The given metagrammar resulted in an ambiguous parse tree");
            }
            var result = new Grammar();
            ProcessSyntaxClause(j, j.AbstractSyntaxForest.NodeTable[j.AbstractSyntaxForest.Root].First(), result);
            ResolveIdentifiers(result);
            return result;
        }
    }
}
