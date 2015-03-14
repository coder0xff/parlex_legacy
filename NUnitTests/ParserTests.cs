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
            var identifier = new NfaProduction("identifier", true, false);
            var identifier0 = new Nfa<ISymbol>.State();
            var identifier1 = new Nfa<ISymbol>.State();
            identifier.States.Add(identifier0);
            identifier.States.Add(identifier1);
            identifier.StartStates.Add(identifier0);
            identifier.AcceptStates.Add(identifier1);
            identifier.TransitionFunction[identifier0][StandardSymbols.LetterTerminal].Add(identifier1);
            identifier.TransitionFunction[identifier1][StandardSymbols.LetterTerminal].Add(identifier1);

            var syntax = new NfaProduction("syntax", false, false);
            var syntax0 = new Nfa<ISymbol>.State();
            var syntax1 = new Nfa<ISymbol>.State();
            var syntax2 = new Nfa<ISymbol>.State();
            var syntax3 = new Nfa<ISymbol>.State();
            syntax.States.Add(syntax0);
            syntax.States.Add(syntax1);
            syntax.States.Add(syntax2);
            syntax.States.Add(syntax3);
            syntax.StartStates.Add(syntax0);
            syntax.AcceptStates.Add(syntax3);
            syntax.TransitionFunction[syntax0][identifier].Add(syntax1);
            syntax.TransitionFunction[syntax1][new StringTerminal("=")].Add(syntax2);
            syntax.TransitionFunction[syntax2][identifier].Add(syntax3);

            g.Productions.Add(syntax);
            g.Productions.Add(identifier);
            g.MainProduction = syntax;

            Parser parser = new Parser(g);
            for (int i = 0; i < 1000; i++) {
                Debug.WriteLine("██████████ Iteration: " + i + " ██████████");
                Parser.Job j = parser.Parse("AB=E");
                j.Join();
                var asf = j.AbstractSyntaxGraph;
                Debug.Assert(!asf.IsEmpty);
            }
        }
    }
}