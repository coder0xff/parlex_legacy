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

        private class SumProduction : SyntaxNode {
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
        public void TestMethod1() {
            for (int i = 0; i < 1000; i++) {
                Debug.WriteLine("██████████ Iteration: " + i + " ██████████");
                var parser = new ParserEngine("a+b", 0, 3, new GenericSyntaxNodeFactory<SumProduction>());
                parser.Join();
                Debug.Assert(parser.AbstractSyntaxGraph.NodeTable.Count == 3);
            }
        }
    }
}
