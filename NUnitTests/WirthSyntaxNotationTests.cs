using System.IO;
using System.Linq;
using NUnit.Framework;
using Parlex;

namespace NUnitTests {
    [TestFixture]
    public class WirthSyntaxNotationTests {
        [Test]
        public void SelfReferentialParseTest() {
            string metaMetaSyntax = File.ReadAllText("C:\\WirthSyntaxNotationDefinedInItself.txt");
            Grammar grammar = WirthSyntaxNotation.GrammarFromString(metaMetaSyntax);
            grammar.MainProduction = grammar.Productions.First(x => x.Name == "SYNTAX");
            Parser parser = new Parser(grammar);
            Parser.Job job = parser.Parse(metaMetaSyntax);
            job.Join();
            var asf = job.AbstractSyntaxGraph;
        }
    }
}