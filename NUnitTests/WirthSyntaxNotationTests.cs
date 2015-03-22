using System.IO;
using System.Linq;
using NUnit.Framework;
using Parlex;

namespace NUnitTests {
    [TestFixture]
    public class WirthSyntaxNotationTests {
        [Test]
        public void SelfReferentialParseTest() {
            var metaMetaSyntax = File.ReadAllText(@"C:\Users\coder_000\Dropbox\parlex\WorthSyntaxNotationDefinedInItself.wsn");
            var grammar = WirthSyntaxNotation.GrammarFromString(metaMetaSyntax).ToNfaGrammar();
            grammar.Main = grammar.Productions.First(x => x.Name == "SYNTAX");
            var parser = new Parser(grammar);
            var job = parser.Parse(metaMetaSyntax);
            job.Join();
            var asf = job.AbstractSyntaxGraph;
        }
    }
}