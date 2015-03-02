using System;
using System.Collections.Concurrent.More;
using System.Linq;
using Automata;

namespace Parlex {
    public class Parser {
        private readonly Grammar.ISymbol _mainSymbol;
        public Parser(Grammar grammar, Grammar.ISymbol mainSymbol = null) {
            _mainSymbol = mainSymbol ?? grammar.MainProduction;
            _factories = new AutoDictionary<Grammar.ISymbol, DynamicSyntaxNodeFactory>(symbol => new DynamicSyntaxNodeFactory(this, symbol));
        }

        private readonly AutoDictionary<Grammar.ISymbol, DynamicSyntaxNodeFactory> _factories;

        private class DynamicSyntaxNode : SyntaxNode {
            private readonly Grammar.Production _production;
            private readonly Parser _parser;

            public DynamicSyntaxNode(Parser parser, Grammar.Production production) {
                _production = production;
                _parser = parser;
            }

            public override void Start() {
                foreach (var state in _production.StartStates) {
                    ProcessState(state);
                }
            }

            private void ProcessState(Grammar.Production.State state) {
                if (_production.AcceptStates.Contains(state)) {
                    Accept();
                }
                foreach (var transition in _production.GetTransitions().Where(transition => transition.FromState == state)) {
                    var transition1 = transition;
                    Transition(_parser._factories[transition.Symbol], () => ProcessState(transition1.ToState));
                }
            }
            public override void OnCompletion(NodeParseResult result) {
            }
        }

        internal class DynamicSyntaxNodeFactory : ISyntaxNodeFactory {
            private readonly Parser _parser;
            private readonly Grammar.Production _production;
            private readonly Grammar.ITerminal _terminal;

            public DynamicSyntaxNodeFactory(Parser parser, Grammar.ISymbol symbol) {
                _parser = parser;
                _production = symbol as Grammar.Production;
                if (_production == null) {
                    _terminal = symbol as Grammar.ITerminal;
                    System.Diagnostics.Debug.Assert(_terminal != null);
                }
            }

            public string Name {
                get {
                    if (_production != null) {
                        return _production.Name;
                    }
                    return _terminal.Name;
                }
            }

            public bool IsGreedy {
                get {
                    if (_production != null) {
                        return _production.Greedy;
                    }
                    return false;
                }
            }

            public SyntaxNode Create() {
                if (_production != null) {
                    return new DynamicSyntaxNode(_parser, _production);
                }
                return new TerminalSyntaxNode(_terminal);
            }

            public bool Is(Grammar.ITerminal terminal) {
                return _terminal == terminal;
            }

            public bool Is(Grammar.Production production) {
                return _production == production;
            }

            public override string ToString() {
                return Name;
            }
        }

        public class Job {
            private readonly ParserEngine _engine;
            private readonly String _document;
            public String Document { get { return _document; } }
            internal Job(String document, int start, int length, DynamicSyntaxNodeFactory main) {
                _document = document;
                _engine = new ParserEngine(_document, start, length, main);
            }

            public AbstractSyntaxGraph AbstractSyntaxGraph { get { return _engine.AbstractSyntaxGraph; } }

            public Int32[] CodePoints { get { return _engine.CodePoints; } }

            public void Join() {
                _engine.Join();
            }
        }

        public Job Parse(String document, int start = 0, int length = -1, Grammar.ISymbol mainSymbol = null) {
            return new Job(document, start, length, _factories[mainSymbol ?? _mainSymbol]);
        }
    }
}
