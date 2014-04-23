using System;

namespace parlex {
	public class WorthSyntaxNotation {
		static Grammar worthSyntaxNotationParserGrammar;

		static WorthSyntaxNotation() {
			Grammar.Recognizer syntax = new Grammar.Recognizer("syntax");
			Grammar.Recognizer production = new Grammar.Recognizer("production");
			Grammar.Recognizer expression = new Grammar.Recognizer("expression");
			Grammar.Recognizer term = new Grammar.Recognizer("term");
			Grammar.Recognizer factor = new Grammar.Recognizer("factor");
			Grammar.Recognizer identifier = new Grammar.Recognizer("identifier");
			Grammar.Recognizer literal = new Grammar.Recognizer("literal");

			Grammar.Recognizer.State syntaxState0 = new Grammar.Recognizer.State(0);
			syntax.States.Add(syntaxState0);
			syntax.StartStates.Add(syntaxState0);
			syntax.AcceptStates.Add(syntaxState0);
			syntax.TransitionFunction[syntaxState0][production].Add(syntaxState0);

			Grammar.Recognizer.State productionState0 = new Grammar.Recognizer.State(0);
			Grammar.Recognizer.State productionState1 = new Grammar.Recognizer.State(0);
			Grammar.Recognizer.State productionState2 = new Grammar.Recognizer.State(0);
			Grammar.Recognizer.State productionState3 = new Grammar.Recognizer.State(0);
			Grammar.Recognizer.State productionState4 = new Grammar.Recognizer.State(0);
			production.States.Add(productionState0);
			production.States.Add(productionState1);
			production.States.Add(productionState2);
			production.States.Add(productionState3);
			production.States.Add(productionState4);
			production.StartStates.Add(productionState0);
			production.AcceptStates.Add(productionState4);
			production.TransitionFunction[productionState0][identifier].Add(productionState1);
			production.TransitionFunction[productionState1][new Grammar.StringTerminal("=")].Add(productionState2);
			production.TransitionFunction[productionState2][expression].Add(productionState3);
			production.TransitionFunction[productionState3][new Grammar.StringTerminal(".")].Add(productionState4);

			Grammar.Recognizer.State expressionState0 = new Grammar.Recognizer.State(0);
			Grammar.Recognizer.State expressionState1 = new Grammar.Recognizer.State(0);
			expression.States.Add(expressionState0);
			expression.States.Add(expressionState1);
			expression.StartStates.Add(expressionState0);
			expression.AcceptStates.Add(expressionState1);
			expression.TransitionFunction[expressionState0][term].Add(expressionState1);
			expression.TransitionFunction[expressionState1][new Grammar.StringTerminal("|")].Add(expressionState0);

			Grammar.Recognizer.State termState0 = new Grammar.Recognizer.State(0);
			Grammar.Recognizer.State termState1 = new Grammar.Recognizer.State(0);
			term.States.Add(termState0);
			term.States.Add(termState1);
			term.StartStates.Add(termState0);
			term.AcceptStates.Add(termState1);
			term.TransitionFunction[termState0][factor].Add(termState1);
			term.TransitionFunction[termState1][factor].Add(termState1);

			Grammar.Recognizer.State factorState0 = new Grammar.Recognizer.State(0);
			Grammar.Recognizer.State factorState1 = new Grammar.Recognizer.State(0);
			Grammar.Recognizer.State factorState2 = new Grammar.Recognizer.State(0);
			Grammar.Recognizer.State factorState3 = new Grammar.Recognizer.State(0);
			Grammar.Recognizer.State factorState4 = new Grammar.Recognizer.State(0);
			Grammar.Recognizer.State factorState5 = new Grammar.Recognizer.State(0);
			Grammar.Recognizer.State factorState6 = new Grammar.Recognizer.State(0);
			Grammar.Recognizer.State factorState7 = new Grammar.Recognizer.State(0);
			factor.States.Add(factorState0);
			factor.States.Add(factorState1);
			factor.States.Add(factorState2);
			factor.States.Add(factorState3);
			factor.StartStates.Add(factorState0);
			factor.AcceptStates.Add(factorState1);
			factor.TransitionFunction[factorState0][identifier].Add(factorState1);
			factor.TransitionFunction[factorState0][literal].Add(factorState1);
			factor.TransitionFunction[factorState0][new Grammar.StringTerminal("[")].Add(factorState2);
			factor.TransitionFunction[factorState0][new Grammar.StringTerminal("(")].Add(factorState3);
			factor.TransitionFunction[factorState0][new Grammar.StringTerminal("{")].Add(factorState4);
			factor.TransitionFunction[factorState2][expression].Add(factorState5);
			factor.TransitionFunction[factorState3][expression].Add(factorState6);
			factor.TransitionFunction[factorState4][expression].Add(factorState7);
			factor.TransitionFunction[factorState5][new Grammar.StringTerminal("[")].Add(factorState1);
			factor.TransitionFunction[factorState6][new Grammar.StringTerminal(")")].Add(factorState1);
			factor.TransitionFunction[factorState7][new Grammar.StringTerminal("}")].Add(factorState1);

			Grammar.Recognizer.State identifierState0 = new Grammar.Recognizer.State(0);
			Grammar.Recognizer.State identifierState1 = new Grammar.Recognizer.State(0);
			identifier.States.Add(identifierState0);
			identifier.States.Add(identifierState1);
			identifier.StartStates.Add(identifierState0);
			identifier.AcceptStates.Add(identifierState1);
			identifier.TransitionFunction[identifierState0][Grammar.LetterTerminal].Add(identifierState1);
			identifier.TransitionFunction[identifierState1][Grammar.LetterTerminal].Add(identifierState1);

			Grammar.Recognizer.State literalState0 = new Grammar.Recognizer.State(0);
			Grammar.Recognizer.State literalState1 = new Grammar.Recognizer.State(0);
			Grammar.Recognizer.State literalState2 = new Grammar.Recognizer.State(0);
			Grammar.Recognizer.State literalState3 = new Grammar.Recognizer.State(0);
			literal.States.Add(literalState0);
			literal.States.Add(literalState1);
			literal.States.Add(literalState2);
			literal.States.Add(literalState3);
			literal.StartStates.Add(literalState0);
			literal.AcceptStates.Add(literalState3);
			literal.TransitionFunction[literalState0][new Grammar.StringTerminal("\"")].Add(literalState1);
			literal.TransitionFunction[literalState1][Grammar.CharacterTerminal].Add(literalState2);
			literal.TransitionFunction[literalState2][Grammar.CharacterTerminal].Add(literalState2);
			literal.TransitionFunction[literalState2][new Grammar.StringTerminal("\"")].Add(literalState3);

			worthSyntaxNotationParserGrammar = new Grammar();
			worthSyntaxNotationParserGrammar.Productions.Add(syntax);
			worthSyntaxNotationParserGrammar.Productions.Add(production);
			worthSyntaxNotationParserGrammar.Productions.Add(expression);
			worthSyntaxNotationParserGrammar.Productions.Add(term);
			worthSyntaxNotationParserGrammar.Productions.Add(factor);
			worthSyntaxNotationParserGrammar.Productions.Add(identifier);
			worthSyntaxNotationParserGrammar.Productions.Add(literal);
			worthSyntaxNotationParserGrammar.MainProduction = syntax;
			worthSyntaxNotationParserGrammar.EatWhiteSpaceAfterProductions = true;
		}

		public static Grammar LoadGrammar(String text) {
			Parser worthSyntaxNotationParser = new Parser(worthSyntaxNotationParserGrammar);
            Parser.Job j = worthSyntaxNotationParser.Parse(text);
            j.Wait();
			throw new NotImplementedException();
		}
	}
}
