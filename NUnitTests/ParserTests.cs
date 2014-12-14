using System.Diagnostics;
using Automata;
using NUnit.Framework;
using Parlex;

namespace NUnitTests {
    [TestFixture]
    public class ParserTests {
        [Test]
        public void TestMethod1() {
            var g = new Grammar();
            var identifier = new Grammar.Recognizer("identifier", true, false);
            var identifier0 = new Nfa<Grammar.ISymbol>.State();
            var identifier1 = new Nfa<Grammar.ISymbol>.State();
            identifier.States.Add(identifier0);
            identifier.States.Add(identifier1);
            identifier.StartStates.Add(identifier0);
            identifier.AcceptStates.Add(identifier1);
            identifier.TransitionFunction[identifier0][Grammar.LetterTerminal].Add(identifier1);
            identifier.TransitionFunction[identifier1][Grammar.LetterTerminal].Add(identifier1);

            var syntax = new Grammar.Recognizer("syntax", false, false);
            var syntax0 = new Nfa<Grammar.ISymbol>.State();
            var syntax1 = new Nfa<Grammar.ISymbol>.State();
            var syntax2 = new Nfa<Grammar.ISymbol>.State();
            var syntax3 = new Nfa<Grammar.ISymbol>.State();
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
            g.MainSymbol = syntax;

            Parser parser = new Parser(g);
            for (int i = 0; i < 100; i++) {
                Debug.WriteLine("Iteration: " + i);
                Parser.Job j = parser.Parse("ABCD=EFGH");
                j.Join();
                Parser.AbstractSyntaxForest asf = j.AbstractSyntaxForest;
            }
        }
    }
}