using Automata;

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
            var syntaxState0 = new Nfa<ISymbol>.State();
            var syntaxState1 = new Nfa<ISymbol>.State();
            Syntax.Nfa.States.Add(syntaxState0);
            Syntax.Nfa.States.Add(syntaxState1);
            Syntax.Nfa.StartStates.Add(syntaxState0);
            Syntax.Nfa.AcceptStates.Add(syntaxState0);
            Syntax.Nfa.AcceptStates.Add(syntaxState1);
            Syntax.Nfa.TransitionFunction[syntaxState0][StandardSymbols.WhiteSpaces].Add(syntaxState1);
            Syntax.Nfa.TransitionFunction[syntaxState0][Production].Add(syntaxState0);
            NfaGrammar.Productions.Add(Syntax);

            var productionState0 = new Nfa<ISymbol>.State();
            var productionState1 = new Nfa<ISymbol>.State();
            var productionState2 = new Nfa<ISymbol>.State();
            var productionState3 = new Nfa<ISymbol>.State();
            var productionState4 = new Nfa<ISymbol>.State();
            Production.Nfa.States.Add(productionState0);
            Production.Nfa.States.Add(productionState1);
            Production.Nfa.States.Add(productionState2);
            Production.Nfa.States.Add(productionState3);
            Production.Nfa.States.Add(productionState4);
            Production.Nfa.StartStates.Add(productionState2);
            Production.Nfa.AcceptStates.Add(productionState4);
            Production.Nfa.TransitionFunction[productionState1][Expression].Add(productionState3);
            Production.Nfa.TransitionFunction[productionState2][Identifier].Add(productionState0);
            Production.Nfa.TransitionFunction[productionState3][DotTerminal].Add(productionState4);
            Production.Nfa.TransitionFunction[productionState3][StandardSymbols.WhiteSpaces].Add(productionState3);
            Production.Nfa.TransitionFunction[productionState0][StandardSymbols.WhiteSpaces].Add(productionState0);
            Production.Nfa.TransitionFunction[productionState0][EqualTerminal].Add(productionState1);
            NfaGrammar.Productions.Add(Production);

            var expressionState0 = new Nfa<ISymbol>.State();
            var expressionState1 = new Nfa<ISymbol>.State();
            Expression.Nfa.States.Add(expressionState0);
            Expression.Nfa.States.Add(expressionState1);
            Expression.Nfa.StartStates.Add(expressionState1);
            Expression.Nfa.AcceptStates.Add(expressionState0);
            Expression.Nfa.TransitionFunction[expressionState0][StandardSymbols.WhiteSpaces].Add(expressionState0);
            Expression.Nfa.TransitionFunction[expressionState0][PipeTerminal].Add(expressionState1);
            Expression.Nfa.TransitionFunction[expressionState1][Term].Add(expressionState0);
            NfaGrammar.Productions.Add(Expression);

            var termState0 = new Nfa<ISymbol>.State();
            var termState1 = new Nfa<ISymbol>.State();
            Term.Nfa.States.Add(termState0);
            Term.Nfa.States.Add(termState1);
            Term.Nfa.StartStates.Add(termState0);
            Term.Nfa.AcceptStates.Add(termState1);
            Term.Nfa.TransitionFunction[termState1][Factor].Add(termState1);
            Term.Nfa.TransitionFunction[termState0][Factor].Add(termState1);
            NfaGrammar.Productions.Add(Term);

            var factorState0 = new Nfa<ISymbol>.State();
            var factorState1 = new Nfa<ISymbol>.State();
            var factorState2 = new Nfa<ISymbol>.State();
            var factorState3 = new Nfa<ISymbol>.State();
            var factorState4 = new Nfa<ISymbol>.State();
            var factorState5 = new Nfa<ISymbol>.State();
            var factorState6 = new Nfa<ISymbol>.State();
            var factorState7 = new Nfa<ISymbol>.State();
            var factorState8 = new Nfa<ISymbol>.State();
            Factor.Nfa.States.Add(factorState0);
            Factor.Nfa.States.Add(factorState1);
            Factor.Nfa.States.Add(factorState2);
            Factor.Nfa.States.Add(factorState3);
            Factor.Nfa.States.Add(factorState4);
            Factor.Nfa.States.Add(factorState5);
            Factor.Nfa.States.Add(factorState6);
            Factor.Nfa.States.Add(factorState7);
            Factor.Nfa.States.Add(factorState8);
            Factor.Nfa.StartStates.Add(factorState4);
            Factor.Nfa.StartStates.Add(factorState1);
            Factor.Nfa.AcceptStates.Add(factorState0);
            Factor.Nfa.TransitionFunction[factorState2][CloseSquareTerminal].Add(factorState0);
            Factor.Nfa.TransitionFunction[factorState6][Expression].Add(factorState2);
            Factor.Nfa.TransitionFunction[factorState8][Expression].Add(factorState5);
            Factor.Nfa.TransitionFunction[factorState4][OpenParenthesisTerminal].Add(factorState7);
            Factor.Nfa.TransitionFunction[factorState4][OpenCurlyTerminal].Add(factorState8);
            Factor.Nfa.TransitionFunction[factorState4][OpenSquareTerminal].Add(factorState6);
            Factor.Nfa.TransitionFunction[factorState5][CloseCurlyTerminal].Add(factorState0);
            Factor.Nfa.TransitionFunction[factorState3][CloseParenthesisTerminal].Add(factorState0);
            Factor.Nfa.TransitionFunction[factorState1][StandardSymbols.WhiteSpaces].Add(factorState4);
            Factor.Nfa.TransitionFunction[factorState1][Literal].Add(factorState0);
            Factor.Nfa.TransitionFunction[factorState1][Identifier].Add(factorState0);
            Factor.Nfa.TransitionFunction[factorState7][Expression].Add(factorState3);
            NfaGrammar.Productions.Add(Factor);

            var identifierState0 = new Nfa<ISymbol>.State();
            var identifierState1 = new Nfa<ISymbol>.State();
            Identifier.Nfa.States.Add(identifierState0);
            Identifier.Nfa.States.Add(identifierState1);
            Identifier.Nfa.StartStates.Add(identifierState0);
            Identifier.Nfa.AcceptStates.Add(identifierState1);
            Identifier.Nfa.TransitionFunction[identifierState0][StandardSymbols.AlphaNumericTerminal].Add(identifierState1);
            Identifier.Nfa.TransitionFunction[identifierState0][StandardSymbols.WhiteSpaces].Add(identifierState0);
            Identifier.Nfa.TransitionFunction[identifierState0][UnderscoreTerminal].Add(identifierState1);
            Identifier.Nfa.TransitionFunction[identifierState1][StandardSymbols.AlphaNumericTerminal].Add(identifierState1);
            Identifier.Nfa.TransitionFunction[identifierState1][UnderscoreTerminal].Add(identifierState1);
            NfaGrammar.Productions.Add(Identifier);

            var literalState0 = new Nfa<ISymbol>.State();
            var literalState1 = new Nfa<ISymbol>.State();
            Literal.Nfa.States.Add(literalState0);
            Literal.Nfa.States.Add(literalState1);
            Literal.Nfa.StartStates.Add(literalState1);
            Literal.Nfa.AcceptStates.Add(literalState0);
            Literal.Nfa.TransitionFunction[literalState1][StandardSymbols.WhiteSpaces].Add(literalState1);
            Literal.Nfa.TransitionFunction[literalState1][StandardSymbols.StringLiteral].Add(literalState0);
            NfaGrammar.Productions.Add(Literal);

            NfaGrammar.Main = Syntax;
        }
    }
}
