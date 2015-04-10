using System;
using System.Collections.Generic;

namespace Parlex {
    public class Parser {
        public Parser(NfaGrammar grammar, Recognizer mainRecognizerDefinition = null) {
            if (grammar == null) {
                throw new ArgumentNullException("grammar");
            }
            _mainRecognizerDefinition = mainRecognizerDefinition ?? grammar.Main;
        }

        public Job Parse(String document, int start = 0, int length = -1, Recognizer mainRecognizerDefinition = null) {
            return new Job(document, start, length, mainRecognizerDefinition ?? _mainRecognizerDefinition);
        }
        private readonly Recognizer _mainRecognizerDefinition;
    }

    public class Job {
        public String Document { get { return _document; } }
        public IReadOnlyList<Int32> CodePoints { get { return _engine.CodePoints; } }

        public AbstractSyntaxGraph AbstractSyntaxGraph { get { return _engine.AbstractSyntaxGraph; } }

        public void Join() {
            _engine.Join();
        }
        internal Job(String document, int start, int length, Recognizer main) {
            _document = document;
            _engine = new ParseEngine(_document, main, start, length);
        }

        private readonly ParseEngine _engine;
        private readonly String _document;
    }
}
