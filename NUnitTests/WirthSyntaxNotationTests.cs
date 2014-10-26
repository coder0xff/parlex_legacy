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
            Parser.Job job = Parser.Parse(metaMetaSyntax, 0, grammar.MainProduction);
            job.Wait();
            Parser.AbstractSyntaxForest asf = job.AbstractSyntaxForest;
        }
    }
}