using System;
using System.Collections.Concurrent.More;
using System.Linq;
using Automata;

namespace Parlex {
    public class Parser {
        private readonly Recognizer _mainRecognizerDefinition;
        public Parser(NfaGrammar grammar, Recognizer mainRecognizerDefinition = null) {
            _mainRecognizerDefinition = mainRecognizerDefinition ?? grammar.Main;
        }

        public class Job {
            private readonly ParseEngine _engine;
            private readonly String _document;
            public String Document { get { return _document; } }
            internal Job(String document, int start, int length, Recognizer main) {
                _document = document;
                _engine = new ParseEngine(_document, main, start, length);
            }

            public AbstractSyntaxGraph AbstractSyntaxGraph { get { return _engine.AbstractSyntaxGraph; } }

            public Int32[] CodePoints { get { return _engine.CodePoints; } }

            public void Join() {
                _engine.Join();
            }
        }

        public Job Parse(String document, int start = 0, int length = -1, Recognizer mainRecognizerDefinition = null) {
            return new Job(document, start, length, mainRecognizerDefinition ?? _mainRecognizerDefinition);
        }
    }
}
