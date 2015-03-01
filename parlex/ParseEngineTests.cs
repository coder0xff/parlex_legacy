using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Parlex;

// ReSharper disable once CheckNamespace
namespace NUnitTests {
    [TestFixture]
    public class ParserEngineTests {
        private class LetterTerminal : SyntaxNode {
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

        private class LetterTerminalFactory : ISyntaxNodeFactory {
            public string Name { get { return "LetterTerminal"; } }
            public bool IsGreedy { get { return false; } }
            public SyntaxNode Create() {
                return new LetterTerminal();
            }

            public bool Is(Grammar.ITerminal terminal) {
                return false;
            }

            public bool Is(Grammar.Production production) {
                return false;
            }
        }

        private class SumProduction : SyntaxNode {
            public override void Start() {
                Transition(new LetterTerminalFactory(), State1);
            }

            private void State1() {
                if (Position < Engine.CodePoints.Length) {
                    if (Engine.CodePoints[Position] == '+') {
                        Position++;
                        Transition(new LetterTerminalFactory(), State2);
                    }
                }
            }

            private void State2() {
                Accept();
            }

            public override void OnCompletion(NodeParseResult result) {
            }
        }

        private class SumProductionFactory : ISyntaxNodeFactory {
            public string Name { get { return "SumProduction"; } }
            public bool IsGreedy { get { return false; } }
            public SyntaxNode Create() {
                return new SumProduction();
            }

            public bool Is(Grammar.ITerminal terminal) {
                return false;
            }

            public bool Is(Grammar.Production production) {
                return false;
            }
        }

        [Test]
        public void TestMethod1() {
            for (int i = 0; i < 1000; i++) {
                Debug.WriteLine("██████████ Iteration: " + i + " ██████████");
                var parser = new ParserEngine("a+b", 0, 3, new SumProductionFactory());
                parser.Join();
                Debug.Assert(parser.AbstractSyntaxGraph.NodeTable.Count == 3);
            }
        }
    }
}
