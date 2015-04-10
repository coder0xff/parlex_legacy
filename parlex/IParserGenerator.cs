using System;

namespace Parlex {
    interface IParserGenerator {
        void Generate(String destinationDirectory, NfaGrammar grammar, String parserName);
    }
}
