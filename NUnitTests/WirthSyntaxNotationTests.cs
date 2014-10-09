using System.Linq;
using NUnit.Framework;
using Parlex;

namespace NUnitTests {
    [TestFixture]
    public class WirthSyntaxNotationTests {
        [Test]
        public void SelfReferentialParseTest()
        {
            var metaMetaSyntax = System.IO.File.ReadAllText("C:\\WirthSyntaxNotationDefinedInItself.txt");
            var grammar = WirthSyntaxNotation.GrammarFromString(metaMetaSyntax);
            grammar.MainProduction = grammar.Productions.First(x => x.Name == "SYNTAX");
            var job = Parser.Parse(metaMetaSyntax, 0, grammar.MainProduction);
            job.Wait();
            var asf = job.AbstractSyntaxForest;
        }
    }
}