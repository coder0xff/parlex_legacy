using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Linq;

namespace Parlex {
	public class WirthSyntaxNotation {
		static Grammar worthSyntaxNotationParserGrammar;
        static Grammar.Terminal equalsTerminal = new Grammar.StringTerminal("=");
        static Grammar.Terminal periodTerminal = new Grammar.StringTerminal(".");
        static Grammar.Terminal pipeTerminal = new Grammar.StringTerminal("|");
        static Grammar.Terminal openParenthesisTerminal = new Grammar.StringTerminal("(");
        static Grammar.Terminal closeParenthesisTerminal = new Grammar.StringTerminal(")");
        static Grammar.Terminal openSquareTerminal = new Grammar.StringTerminal("[");
        static Grammar.Terminal closeSquareTerminal = new Grammar.StringTerminal("]");
        static Grammar.Terminal openCurlyTerminal = new Grammar.StringTerminal("{");
        static Grammar.Terminal closeCurlyTerminal = new Grammar.StringTerminal("}");
        static Grammar.Terminal doubleQuoteTerminal = new Grammar.StringTerminal("\"");
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
            
			Grammar.Recognizer.State syntaxState0 = new Grammar.Recognizer.State();
			syntax.States.Add(syntaxState0);
			syntax.StartStates.Add(syntaxState0);
			syntax.AcceptStates.Add(syntaxState0);
			syntax.TransitionFunction[syntaxState0][production].Add(syntaxState0);

			Grammar.Recognizer.State productionState0 = new Grammar.Recognizer.State();
			Grammar.Recognizer.State productionState1 = new Grammar.Recognizer.State();
			Grammar.Recognizer.State productionState2 = new Grammar.Recognizer.State();
			Grammar.Recognizer.State productionState3 = new Grammar.Recognizer.State();
			Grammar.Recognizer.State productionState4 = new Grammar.Recognizer.State();
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

			Grammar.Recognizer.State expressionState0 = new Grammar.Recognizer.State();
			Grammar.Recognizer.State expressionState1 = new Grammar.Recognizer.State();
			expression.States.Add(expressionState0);
			expression.States.Add(expressionState1);
			expression.StartStates.Add(expressionState0);
			expression.AcceptStates.Add(expressionState1);
			expression.TransitionFunction[expressionState0][term].Add(expressionState1);
			expression.TransitionFunction[expressionState1][pipeTerminal].Add(expressionState0);

			Grammar.Recognizer.State termState0 = new Grammar.Recognizer.State();
			Grammar.Recognizer.State termState1 = new Grammar.Recognizer.State();
			term.States.Add(termState0);
			term.States.Add(termState1);
			term.StartStates.Add(termState0);
			term.AcceptStates.Add(termState1);
			term.TransitionFunction[termState0][factor].Add(termState1);
			term.TransitionFunction[termState1][factor].Add(termState1);

			Grammar.Recognizer.State factorState0 = new Grammar.Recognizer.State();
			Grammar.Recognizer.State factorState1 = new Grammar.Recognizer.State();
			Grammar.Recognizer.State factorState2 = new Grammar.Recognizer.State();
			Grammar.Recognizer.State factorState3 = new Grammar.Recognizer.State();
			Grammar.Recognizer.State factorState4 = new Grammar.Recognizer.State();
            Grammar.Recognizer.State factorState5 = new Grammar.Recognizer.State();
			Grammar.Recognizer.State factorState6 = new Grammar.Recognizer.State();
			Grammar.Recognizer.State factorState7 = new Grammar.Recognizer.State();
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

			Grammar.Recognizer.State identifierState0 = new Grammar.Recognizer.State();
			Grammar.Recognizer.State identifierState1 = new Grammar.Recognizer.State();
			identifier.States.Add(identifierState0);
			identifier.States.Add(identifierState1);
			identifier.StartStates.Add(identifierState0);
			identifier.AcceptStates.Add(identifierState1);
			identifier.TransitionFunction[identifierState0][Grammar.LetterTerminal].Add(identifierState1);
			identifier.TransitionFunction[identifierState1][Grammar.LetterTerminal].Add(identifierState1);

			Grammar.Recognizer.State literalState0 = new Grammar.Recognizer.State();
			Grammar.Recognizer.State literalState1 = new Grammar.Recognizer.State();
			Grammar.Recognizer.State literalState2 = new Grammar.Recognizer.State();
			Grammar.Recognizer.State literalState3 = new Grammar.Recognizer.State();
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

        static string ProcessIdentifierClause(Parser.Job job, Parser.Match identifier)
        {
            return job.text.Substring(identifier.position, identifier.length);
        }

        static string ProcessLiteralClause(Parser.Job job, Parser.Match literal)
        {
            return job.text.Substring(literal.position + 1, literal.length - 2); //remove quotes
        }

        static Grammar.Recognizer GetRecognizerByIdentifierString(Grammar result, String identifierString)
        {
            foreach (Grammar.Recognizer recognizer in result.Productions)
            {
                if (recognizer.Name == identifierString.Trim()) return recognizer;
            }
            Grammar.Recognizer newRecognizer = new Grammar.Recognizer(identifierString.Trim(), false);
            result.Productions.Add(newRecognizer);
            return newRecognizer;
        }


        static Nfa<Grammar.Symbol> ProcessFactorClause(Parser.Job job, Parser.Match factor)
        {
            Parser.Match firstChild = job.abstractSyntaxForest.nodeTable[factor.children[0]].First();

            if (firstChild.symbol == identifier)
            {
                Grammar.Symbol transition = new Grammar.Recognizer(placeHolderMarker + ProcessIdentifierClause(job, firstChild), false);
                Nfa<Grammar.Symbol> result = new Nfa<Grammar.Symbol>();
                Nfa<Grammar.Symbol>.State state0 = new Nfa<Grammar.Symbol>.State();
                result.StartStates.Add(state0);
                result.States.Add(state0);
                Nfa<Grammar.Symbol>.State state1 = new Nfa<Grammar.Symbol>.State();
                result.AcceptStates.Add(state1);
                result.States.Add(state1);
                result.TransitionFunction[state0][transition].Add(state1);
                return result;
            }

            if (firstChild.symbol == literal)
            {
                Grammar.StringTerminal transition = new Grammar.StringTerminal(ProcessLiteralClause(job, firstChild));
                Nfa<Grammar.Symbol> result = new Nfa<Grammar.Symbol>();
                Nfa<Grammar.Symbol>.State state0 = new Nfa<Grammar.Symbol>.State();
                result.StartStates.Add(state0);
                result.States.Add(state0);
                Nfa<Grammar.Symbol>.State state1 = new Nfa<Grammar.Symbol>.State();
                result.AcceptStates.Add(state1);
                result.States.Add(state1);
                result.TransitionFunction[state0][transition].Add(state1);
                return result;
            }

            if (firstChild.symbol == openSquareTerminal)
            {
                Parser.Match expression = job.abstractSyntaxForest.nodeTable[factor.children[1]].First();
                Grammar.Recognizer result = ProcessExpressionClause(job, expression, "");
                foreach (Grammar.Recognizer.State state in result.StartStates)
                {
                    result.AcceptStates.Add(state); //to make it optional
                }
                return result;
            }

            if (firstChild.symbol == openParenthesisTerminal)
            {
                Parser.Match expression = job.abstractSyntaxForest.nodeTable[factor.children[1]].First();
                Grammar.Recognizer result = ProcessExpressionClause(job, expression, "");
                return result;
            }

            if (firstChild.symbol == openCurlyTerminal)
            {
                Parser.Match expression = job.abstractSyntaxForest.nodeTable[factor.children[1]].First();
                Grammar.Recognizer result = ProcessExpressionClause(job, expression, "");
                foreach (Grammar.Recognizer.State startState in result.StartStates)
                {
                    foreach (Grammar.Symbol symbol in result.TransitionFunction[startState].Keys)
                    {
                        foreach (Grammar.Recognizer.State toState in result.TransitionFunction[startState][symbol])
                        {
                            foreach (Grammar.Recognizer.State acceptState in result.AcceptStates)
                            {
                                result.TransitionFunction[acceptState][symbol].Add(toState); //to make it repeatable
                            }
                        }
                    }
                }
                return result;
            }

            throw new InvalidOperationException();
        }

        static Nfa<Grammar.Symbol> ProcessTermClause(Parser.Job job, Parser.Match term)
        {
            Nfa<Grammar.Symbol> result = new Nfa<Grammar.Symbol>();
            Nfa<Grammar.Symbol>.State state = new Nfa<Grammar.Symbol>.State();
            result.StartStates.Add(state);
            result.States.Add(state);
            result.AcceptStates.Add(state);
            foreach (Parser.MatchClass matchClass in term.children)
            {
                Parser.Match factor = job.abstractSyntaxForest.nodeTable[matchClass].First();
                Nfa<Grammar.Symbol> factorNfa = ProcessFactorClause(job, factor);
                result.Insert(result.AcceptStates.First(), factorNfa);
                result = result.Determinize().Reassign();
            }
            return result;
        }

        static Grammar.Recognizer ProcessExpressionClause(Parser.Job job, Parser.Match expression, String recognizerName)
        {
            Nfa<Grammar.Symbol> builder = new Nfa<Grammar.Symbol>();

            for (int index = 0; index < expression.children.Length; index += 2)
            {
                Nfa<Grammar.Symbol> termNFA = ProcessTermClause(job, job.abstractSyntaxForest.nodeTable[expression.children[index]].First());
                builder = Nfa<Grammar.Symbol>.Union(new Nfa<Grammar.Symbol>[] { builder, termNFA });
            }

            return new Grammar.Recognizer(recognizerName, false, builder);
        }

        static Grammar.Recognizer ProcessProductionClause(Parser.Job job, Parser.Match production)
        {
            string name = ProcessIdentifierClause(job, job.abstractSyntaxForest.nodeTable[production.children[0]].First());
            return ProcessExpressionClause(job, job.abstractSyntaxForest.nodeTable[production.children[2]].First(), name);
        }

        static void ProcessSyntaxClause(Parser.Job job, Parser.Match syntax, Grammar result)
        {
            foreach (Parser.MatchClass matchClass in syntax.children)
            {
                Grammar.Recognizer recognizer = ProcessProductionClause(job, job.abstractSyntaxForest.nodeTable[matchClass].First());
                result.Productions.Add(recognizer);
            }
        }

        static void ResolveIdentifiers(Grammar grammar)
        {
            foreach (Grammar.Recognizer recognizer in grammar.Productions)
            {
                foreach (Grammar.Recognizer.State fromState in recognizer.States)
                {
                    List<Grammar.Symbol> toRemoves = new List<Grammar.Symbol>();
                    AutoDictionary<Grammar.Symbol, List<Grammar.Recognizer.State>> toAdds = new AutoDictionary<Grammar.Symbol, List<Grammar.Recognizer.State>>(_ => new List<Grammar.Recognizer.State>());
                    foreach (Grammar.Symbol symbol in recognizer.TransitionFunction[fromState].Keys)
                    {
                        Grammar.Recognizer symbolAsRecognizer = symbol as Grammar.Recognizer;
                        if (symbolAsRecognizer != null && symbolAsRecognizer.Name.StartsWith(placeHolderMarker))
                        {
                            toRemoves.Add(symbol);
                            Grammar.Symbol resolved = grammar.GetRecognizerByName(symbolAsRecognizer.Name.Substring(placeHolderMarker.Length));
                            if (resolved == null)
                            {
                                resolved = new Grammar.Recognizer(symbolAsRecognizer.Name.Substring(placeHolderMarker.Length), false);
                            }
                            foreach (Grammar.Recognizer.State toState in recognizer.TransitionFunction[fromState][symbol])
                            {
                                toAdds[resolved].Add(toState);
                            }
                        }
                    }
                    foreach (Grammar.Symbol toRemove in toRemoves)
                    {
                        recognizer.TransitionFunction[fromState].TryRemove(toRemove);
                    }
                    foreach (Grammar.Symbol symbol in toAdds.Keys)
                    {
                        foreach (Grammar.Recognizer.State toState in toAdds[symbol])
                        {
                            recognizer.TransitionFunction[fromState][symbol].Add(toState);
                        }
                    }
                }
            }
        }

		public static Grammar LoadGrammar(String text) {
			Parser worthSyntaxNotationParser = new Parser(worthSyntaxNotationParserGrammar);
            Parser.Job j = worthSyntaxNotationParser.Parse(text);
            j.Wait();
			Parser.AbstractSyntaxForest asg = j.abstractSyntaxForest;
            if (asg.IsAmbiguous)
            {
                throw new InvalidOperationException("The given metagrammar resulted in an ambiguous parse tree");
            }
            Grammar result = new Grammar();
            ProcessSyntaxClause(j, j.abstractSyntaxForest.nodeTable[j.abstractSyntaxForest.root].First(), result);
            ResolveIdentifiers(result);
            return result;
		}
	}
}
