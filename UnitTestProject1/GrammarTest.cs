using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using parlex;

namespace UnitTestProject1 {
    [TestClass]
    public class GrammarTest {
        [TestMethod]
        public void TestFullParse() {
            string testFile = File.ReadAllText(@"C:\Users\Brent\Dropbox\parlex\test.parlex");
            var grammarDocument = GrammarDocument.FromString(testFile);
            var compiledGrammar = new CompiledGrammar(grammarDocument);
            string toParseFile = File.ReadAllText(@"C:\Users\Brent\Dropbox\parlex\parse_test.txt");
            var parseResult = Parser.Parse(toParseFile, "document", compiledGrammar);
            System.Diagnostics.Debug.WriteLine(parseResult.Product.Title);
        }
    }
}
