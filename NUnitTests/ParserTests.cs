using System.Diagnostics;
using Automata;
using NUnit.Framework;
using Parlex;
using System.Linq;

namespace NUnitTests {
    [TestFixture]
    public class ParserTests {
        [Test]
        public void TestMethod1() {
            var g = new NfaGrammar();
            var identifier = new NfaProduction("identifier", true, false);
            var identifier0 = new Nfa<RecognizerDefinition>.State();
            var identifier1 = new Nfa<RecognizerDefinition>.State();
            identifier.Nfa.States.Add(identifier0);
            identifier.Nfa.States.Add(identifier1);
            identifier.Nfa.StartStates.Add(identifier0);
            identifier.Nfa.AcceptStates.Add(identifier1);
            identifier.Nfa.TransitionFunction[identifier0][StandardSymbols.LetterTerminalDefinition].Add(identifier1);
            identifier.Nfa.TransitionFunction[identifier1][StandardSymbols.LetterTerminalDefinition].Add(identifier1);

            var syntax = new NfaProduction("syntax", false, false);
            var syntax0 = new Nfa<RecognizerDefinition>.State();
            var syntax1 = new Nfa<RecognizerDefinition>.State();
            var syntax2 = new Nfa<RecognizerDefinition>.State();
            var syntax3 = new Nfa<RecognizerDefinition>.State();
            syntax.Nfa.States.Add(syntax0);
            syntax.Nfa.States.Add(syntax1);
            syntax.Nfa.States.Add(syntax2);
            syntax.Nfa.States.Add(syntax3);
            syntax.Nfa.StartStates.Add(syntax0);
            syntax.Nfa.AcceptStates.Add(syntax3);
            syntax.Nfa.TransitionFunction[syntax0][identifier].Add(syntax1);
            syntax.Nfa.TransitionFunction[syntax1][new StringTerminalDefinition("=")].Add(syntax2);
            syntax.Nfa.TransitionFunction[syntax2][identifier].Add(syntax3);

            g.Productions.Add(syntax);
            g.Productions.Add(identifier);
            g.Main = syntax;

            Parser parser = new Parser(g);
            for (int i = 0; i < 1000; i++) {
                Debug.WriteLine("██████████ Iteration: " + i + " ██████████");
                Parser.Job j = parser.Parse("AB=E");
                j.Join();
                var asf = j.AbstractSyntaxGraph;
                Debug.Assert(!asf.IsEmpty);
            }
        }
        private class LetterTerminal : ParseNode {
            public override void Start() {
                if (Position < Engine.CodePoints.Length) {
                    if (Unicode.LowercaseLetters.Contains(Engine.CodePoints[Position])) {
                        Position++;
                        Accept();
                    }
                }
            }

            public override void OnCompletion(NodeParseResult result) {
            }
        }

        private class SumProduction : ParseNode {
            public override void Start() {
                Transition<LetterTerminal>(State1);
            }

            private void State1() {
                if (Position < Engine.CodePoints.Length) {
                    if (Engine.CodePoints[Position] == '+') {
                        Position++;
                        Transition<LetterTerminal>(State2);
                    }
                }
            }

            private void State2() {
                Accept();
            }

            public override void OnCompletion(NodeParseResult result) {
            }
        }

        [Test]
        public void TestMethod2() {
            for (int i = 0; i < 1000; i++) {
                Debug.WriteLine("██████████ Iteration: " + i + " ██████████");
                var parser = new ParseEngine("a+b", new GenericParseNodeFactory<SumProduction>(), 0, 3);
                parser.Join();
                Debug.Assert(parser.AbstractSyntaxGraph.NodeTable.Count == 3);
            }
        }
    }
}