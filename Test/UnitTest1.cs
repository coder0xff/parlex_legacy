using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NondeterministicFiniteAutomata;
using Parlex;

namespace Test {
    [TestClass]
    public class NfaTests
    {
        [TestMethod]
        public void TestDeterminize()
        {
            for (int counter = 0; counter < 10; counter++)
            {
                NFA<Char, int>.State state0 = new NFA<Char, int>.State(0);
                NFA<Char, int>.State state1 = new NFA<Char, int>.State(1);
                NFA<Char, int> nfa = new NFA<Char, int>();
                nfa.States.Add(state1);
                nfa.States.Add(state0);
                nfa.StartStates.Add(state0);
                nfa.AcceptStates.Add(state1);
                nfa.TransitionFunction[state0]['A'].Add(state1);
                var result = nfa.Determinize();
                Debug.Assert(result.States.Count == 2);
                Debug.Assert(result.StartStates.Count == 1);
                Debug.Assert(result.AcceptStates.Count == 1);
                var resultStartState = result.StartStates.First();
                Debug.Assert(result.TransitionFunction[resultStartState].Keys.Contains('A'));
                Debug.Assert(result.TransitionFunction[resultStartState]['A'].Count == 1);
                var resultAcceptState = result.TransitionFunction[resultStartState]['A'].First();
                Debug.Assert(resultStartState != resultAcceptState);
            }
        }

        [TestMethod]
        public void TestMinimize() {
            for (int counter = 0; counter < 10; counter++) {
                NFA<Char, int>.State state0 = new NFA<Char, int>.State(0);
                NFA<Char, int>.State state1 = new NFA<Char, int>.State(1);
                NFA<Char, int> nfa = new NFA<Char, int>();
                nfa.States.Add(state1);
                nfa.States.Add(state0);
                nfa.StartStates.Add(state0);
                nfa.AcceptStates.Add(state1);
                nfa.TransitionFunction[state0]['A'].Add(state1);
                var result = nfa.Minimized();
                Debug.Assert(result.States.Count == 2);
                Debug.Assert(result.StartStates.Count == 1);
                Debug.Assert(result.AcceptStates.Count == 1);
                var resultStartState = result.StartStates.First();
                Debug.Assert(result.TransitionFunction[resultStartState].Keys.Contains('A'));
                Debug.Assert(result.TransitionFunction[resultStartState]['A'].Count == 1);
                var resultAcceptState = result.TransitionFunction[resultStartState]['A'].First();
                Debug.Assert(resultStartState != resultAcceptState);
            }
        }
    }
    [TestClass]
    public class ParserTests {
        [TestMethod]
        public void TestMethod1() {
            var g = new Grammar();
            var identifier = new Grammar.Recognizer("identifier", true);
            var identifier0 = new NFA<Grammar.ISymbol>.State();
            var identifier1 = new NFA<Grammar.ISymbol>.State();
            identifier.States.Add(identifier0);
            identifier.States.Add(identifier1);
            identifier.StartStates.Add(identifier0);
            identifier.AcceptStates.Add(identifier1);
            identifier.TransitionFunction[identifier0][Grammar.LetterTerminal].Add(identifier1);
            identifier.TransitionFunction[identifier1][Grammar.LetterTerminal].Add(identifier1);

            var syntax = new Grammar.Recognizer("syntax", false);
            var syntax0 = new NFA<Grammar.ISymbol>.State();
            var syntax1 = new NFA<Grammar.ISymbol>.State();
            var syntax2 = new NFA<Grammar.ISymbol>.State();
            var syntax3 = new NFA<Grammar.ISymbol>.State();
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

            var j = Parser.Parse("A=B", 0, g.MainProduction);
            j.Wait();
            var asf = j.AbstractSyntaxForest;
        }
    }

    [TestClass]
    public class WirthSyntaxNotationTests {
        [TestMethod]
        public void SelfReferentialParseTest()
        {
            var metaMetaSyntax = System.IO.File.ReadAllText("C:\\WirthSyntaxNotationDefinedInItself.txt");
            var grammar = WirthSyntaxNotation.LoadGrammar(metaMetaSyntax);
            grammar.MainProduction = grammar.Productions.First(x => x.Name == "SYNTAX");
            var job = Parser.Parse(metaMetaSyntax, 0, grammar.MainProduction);
            job.Wait();
            var asf = job.AbstractSyntaxForest;
        }
    }
}
