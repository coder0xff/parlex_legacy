using System;
using System.IO;

namespace parlex {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            string testFile = File.ReadAllText(@"C:\Users\Brent\Dropbox\parlex\test.ple");
            var grammarDocument = GrammarDocument.FromString(testFile);
            var compiledGrammar = new CompiledGrammar(grammarDocument);
            string toParseFile = File.ReadAllText(@"C:\Users\Brent\Dropbox\parlex\parse_test.txt");
            var parseResult = Parser.Parse(toParseFile, "document", compiledGrammar);
            System.Diagnostics.Debug.WriteLine(parseResult.Product.Title);
        }
    }
}
