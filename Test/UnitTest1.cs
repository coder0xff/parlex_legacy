using Microsoft.VisualStudio.TestTools.UnitTesting;
using NondeterministicFiniteAutomata;
using Parlex;

namespace Test
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var g = new Grammar();
            var identifier = new Grammar.Recognizer("identifier", true);
            var identifier0 = new NondeterministicFiniteAutomaton<Grammar.ISymbol>.State();
            var identifier1 = new NondeterministicFiniteAutomaton<Grammar.ISymbol>.State();
            identifier.States.Add(identifier0);
            identifier.States.Add(identifier1);
            identifier.StartStates.Add(identifier0);
            identifier.AcceptStates.Add(identifier1);
            identifier.TransitionFunction[identifier0][Grammar.LetterTerminal].Add(identifier1);
            identifier.TransitionFunction[identifier1][Grammar.LetterTerminal].Add(identifier1);

            var syntax = new Grammar.Recognizer("syntax", false);
            var syntax0 = new NondeterministicFiniteAutomaton<Grammar.ISymbol>.State();
            var syntax1 = new NondeterministicFiniteAutomaton<Grammar.ISymbol>.State();
            var syntax2 = new NondeterministicFiniteAutomaton<Grammar.ISymbol>.State();
            var syntax3 = new NondeterministicFiniteAutomaton<Grammar.ISymbol>.State();
            syntax.States.Add(syntax0);
            syntax.States.Add(syntax1);
            syntax.States.Add(syntax2);
            syntax.States.Add(syntax3);
            syntax.StartStates.Add(syntax0);
            syntax.AcceptStates.Add(syntax3);
            syntax.TransitionFunction[syntax0][identifier].Add(syntax1);
            syntax.TransitionFunction[syntax1][new Grammar.StringTerminal("=")].Add(syntax2);
            syntax.TransitionFunction[syntax2][identifier].Add(syntax3);

            g.Productions.Add(syntax);
            g.Productions.Add(identifier);
            g.MainProduction = syntax;

            var p = new Parser(g);
            var j = p.Parse("A=B");
            j.Wait();
            var asf = j.AbstractSyntaxForest;
        }
    }
}
