namespace Parlex {
    public static class WirthSyntaxNotationGrammar {
        public static NfaGrammar NfaGrammar = new NfaGrammar();
        private static readonly ITerminal DotTerminal = new StringTerminal(".");
        private static readonly ITerminal EqualTerminal = new StringTerminal("=");
        private static readonly ITerminal PipeTerminal = new StringTerminal("|");
        private static readonly ITerminal CloseSquareTerminal = new StringTerminal("]");
        internal static readonly ITerminal OpenParenthesisTerminal = new StringTerminal("(");
        internal static readonly ITerminal OpenCurlyTerminal = new StringTerminal("{");
        public static readonly ITerminal OpenSquareTerminal = new StringTerminal("[");
        private static readonly ITerminal CloseCurlyTerminal = new StringTerminal("}");
        private static readonly ITerminal CloseParenthesisTerminal = new StringTerminal(")");
        private static readonly ITerminal UnderscoreTerminal = new StringTerminal("_");
        private static readonly NfaProduction Syntax = new NfaProduction("Syntax", true, false);
        private static readonly NfaProduction Production = new NfaProduction("Production", false, false);
        private static readonly NfaProduction Expression = new NfaProduction("Expression", true, false);
        private static readonly NfaProduction Term = new NfaProduction("Term", true, false);
        private static readonly NfaProduction Factor = new NfaProduction("Factor", true, false);
        public static readonly NfaProduction Identifier = new NfaProduction("Identifier", true, false);
        public static readonly NfaProduction Literal = new NfaProduction("Literal", true, false);

        static WirthSyntaxNotationGrammar() {
            var syntaxState0 = new NfaProduction.State();
            Syntax.States.Add(syntaxState0);
            var syntaxState1 = new NfaProduction.State();
            Syntax.States.Add(syntaxState1);
            Syntax.StartStates.Add(syntaxState0);
            Syntax.AcceptStates.Add(syntaxState0);
            Syntax.AcceptStates.Add(syntaxState1);
            Syntax.TransitionFunction[syntaxState0][StandardSymbols.WhiteSpaces].Add(syntaxState1);
            Syntax.TransitionFunction[syntaxState0][Production].Add(syntaxState0);
            NfaGrammar.Productions.Add(Syntax);

            var productionState0 = new NfaProduction.State();
            Production.States.Add(productionState0);
            var productionState1 = new NfaProduction.State();
            Production.States.Add(productionState1);
            var productionState2 = new NfaProduction.State();
            Production.States.Add(productionState2);
            var productionState3 = new NfaProduction.State();
            Production.States.Add(productionState3);
            var productionState4 = new NfaProduction.State();
            Production.States.Add(productionState4);
            Production.StartStates.Add(productionState2);
            Production.AcceptStates.Add(productionState4);
            Production.TransitionFunction[productionState1][Expression].Add(productionState3);
            Production.TransitionFunction[productionState2][Identifier].Add(productionState0);
            Production.TransitionFunction[productionState3][DotTerminal].Add(productionState4);
            Production.TransitionFunction[productionState3][StandardSymbols.WhiteSpaces].Add(productionState3);
            Production.TransitionFunction[productionState0][StandardSymbols.WhiteSpaces].Add(productionState0);
            Production.TransitionFunction[productionState0][EqualTerminal].Add(productionState1);
            NfaGrammar.Productions.Add(Production);

            var expressionState0 = new NfaProduction.State();
            Expression.States.Add(expressionState0);
            var expressionState1 = new NfaProduction.State();
            Expression.States.Add(expressionState1);
            Expression.StartStates.Add(expressionState1);
            Expression.AcceptStates.Add(expressionState0);
            Expression.TransitionFunction[expressionState0][StandardSymbols.WhiteSpaces].Add(expressionState0);
            Expression.TransitionFunction[expressionState0][PipeTerminal].Add(expressionState1);
            Expression.TransitionFunction[expressionState1][Term].Add(expressionState0);
            NfaGrammar.Productions.Add(Expression);

            var termState0 = new NfaProduction.State();
            Term.States.Add(termState0);
            var termState1 = new NfaProduction.State();
            Term.States.Add(termState1);
            Term.StartStates.Add(termState0);
            Term.AcceptStates.Add(termState1);
            Term.TransitionFunction[termState1][Factor].Add(termState1);
            Term.TransitionFunction[termState0][Factor].Add(termState1);
            NfaGrammar.Productions.Add(Term);

            var factorState0 = new NfaProduction.State();
            Factor.States.Add(factorState0);
            var factorState1 = new NfaProduction.State();
            Factor.States.Add(factorState1);
            var factorState2 = new NfaProduction.State();
            Factor.States.Add(factorState2);
            var factorState3 = new NfaProduction.State();
            Factor.States.Add(factorState3);
            var factorState4 = new NfaProduction.State();
            Factor.States.Add(factorState4);
            var factorState5 = new NfaProduction.State();
            Factor.States.Add(factorState5);
            var factorState6 = new NfaProduction.State();
            Factor.States.Add(factorState6);
            var factorState7 = new NfaProduction.State();
            Factor.States.Add(factorState7);
            var factorState8 = new NfaProduction.State();
            Factor.States.Add(factorState8);
            Factor.StartStates.Add(factorState4);
            Factor.StartStates.Add(factorState1);
            Factor.AcceptStates.Add(factorState0);
            Factor.TransitionFunction[factorState2][CloseSquareTerminal].Add(factorState0);
            Factor.TransitionFunction[factorState6][Expression].Add(factorState2);
            Factor.TransitionFunction[factorState8][Expression].Add(factorState5);
            Factor.TransitionFunction[factorState4][OpenParenthesisTerminal].Add(factorState7);
            Factor.TransitionFunction[factorState4][OpenCurlyTerminal].Add(factorState8);
            Factor.TransitionFunction[factorState4][OpenSquareTerminal].Add(factorState6);
            Factor.TransitionFunction[factorState5][CloseCurlyTerminal].Add(factorState0);
            Factor.TransitionFunction[factorState3][CloseParenthesisTerminal].Add(factorState0);
            Factor.TransitionFunction[factorState1][StandardSymbols.WhiteSpaces].Add(factorState4);
            Factor.TransitionFunction[factorState1][Literal].Add(factorState0);
            Factor.TransitionFunction[factorState1][Identifier].Add(factorState0);
            Factor.TransitionFunction[factorState7][Expression].Add(factorState3);
            NfaGrammar.Productions.Add(Factor);

            var identifierState0 = new NfaProduction.State();
            Identifier.States.Add(identifierState0);
            var identifierState1 = new NfaProduction.State();
            Identifier.States.Add(identifierState1);
            Identifier.StartStates.Add(identifierState0);
            Identifier.AcceptStates.Add(identifierState1);
            Identifier.TransitionFunction[identifierState0][StandardSymbols.AlphaNumericTerminal].Add(identifierState1);
            Identifier.TransitionFunction[identifierState0][StandardSymbols.WhiteSpaces].Add(identifierState0);
            Identifier.TransitionFunction[identifierState0][UnderscoreTerminal].Add(identifierState1);
            Identifier.TransitionFunction[identifierState1][StandardSymbols.AlphaNumericTerminal].Add(identifierState1);
            Identifier.TransitionFunction[identifierState1][UnderscoreTerminal].Add(identifierState1);
            NfaGrammar.Productions.Add(Identifier);

            var literalState0 = new NfaProduction.State();
            Literal.States.Add(literalState0);
            var literalState1 = new NfaProduction.State();
            Literal.States.Add(literalState1);
            Literal.StartStates.Add(literalState1);
            Literal.AcceptStates.Add(literalState0);
            Literal.TransitionFunction[literalState1][StandardSymbols.WhiteSpaces].Add(literalState1);
            Literal.TransitionFunction[literalState1][StandardSymbols.StringLiteral].Add(literalState0);
            NfaGrammar.Productions.Add(Literal);

            NfaGrammar.Main = Syntax;
        }
    }
}
