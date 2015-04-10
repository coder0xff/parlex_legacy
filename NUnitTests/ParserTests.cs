using System.Diagnostics;
using Automata;
using NUnit.Framework;
using Parlex;

namespace NUnitTests {
    [TestFixture]
    public class ParserTests {
        [Test]
        public void TestMethod1() {
            var g = new NfaGrammar();
            var identifier = new NfaProduction("identifier", true);
            var identifier0 = new Nfa<Recognizer>.State();
            var identifier1 = new Nfa<Recognizer>.State();
            identifier.Nfa.States.Add(identifier0);
            identifier.Nfa.States.Add(identifier1);
            identifier.Nfa.StartStates.Add(identifier0);
            identifier.Nfa.AcceptStates.Add(identifier1);
            identifier.Nfa.TransitionFunction[identifier0][StandardSymbols.Letter].Add(identifier1);
            identifier.Nfa.TransitionFunction[identifier1][StandardSymbols.Letter].Add(identifier1);

            var syntax = new NfaProduction("syntax", false);
            var syntax0 = new Nfa<Recognizer>.State();
            var syntax1 = new Nfa<Recognizer>.State();
            var syntax2 = new Nfa<Recognizer>.State();
            var syntax3 = new Nfa<Recognizer>.State();
            syntax.Nfa.States.Add(syntax0);
            syntax.Nfa.States.Add(syntax1);
            syntax.Nfa.States.Add(syntax2);
            syntax.Nfa.States.Add(syntax3);
            syntax.Nfa.StartStates.Add(syntax0);
            syntax.Nfa.AcceptStates.Add(syntax3);
            syntax.Nfa.TransitionFunction[syntax0][identifier].Add(syntax1);
            syntax.Nfa.TransitionFunction[syntax1][new StringTerminal("=")].Add(syntax2);
            syntax.Nfa.TransitionFunction[syntax2][identifier].Add(syntax3);

            g.Productions.Add(syntax);
            g.Productions.Add(identifier);
            g.Main = syntax;

            var parser = new Parser(g);
            for (int i = 0; i < 1000; i++) {
                Debug.WriteLine("██████████ Iteration: " + i + " ██████████");
                Job j = parser.Parse("AB=E");
                j.Join();
                var asf = j.AbstractSyntaxGraph;
                Debug.Assert(!asf.IsEmpty);
            }
        }

        private class SumProduction : Recognizer {
            public override string Name {
                get { return "SumProduction"; }
            }

            public override bool IsGreedy {
                get { return false; }
            }

            public override void Start() {
                Transition(StandardSymbols.Letter, State1);
            }

            private void State1() {
                if (ParseContext.Position < ParseContext.Engine.CodePoints.Count) {
                    if (ParseContext.Engine.CodePoints[ParseContext.Position] == '+') {
                        ParseContext.Position++;
                        Transition(StandardSymbols.Letter, State2);
                    }
                }
            }

            private void State2() {
                Accept();
            }
        }

        [Test]
        public void TestMethod2() {
            for (int i = 0; i < 1000; i++) {
                Debug.WriteLine("██████████ Iteration: " + i + " ██████████");
                var parser = new ParseEngine("a+b", new SumProduction(), 0, 3);
                parser.Join();
                Debug.Assert(parser.AbstractSyntaxGraph.NodeTable.Count == 3);
            }
        }
    }
}