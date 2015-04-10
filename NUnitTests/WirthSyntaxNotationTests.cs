using System.Linq;
using NUnit.Framework;
using Parlex;

namespace NUnitTests {
    [TestFixture]
    public class WirthSyntaxNotationTests {
        [Test]
        public void SelfReferentialParseTest() {
            const string metaMetaSyntax = "SYNTAX={PRODUCTION}.PRODUCTION=IDENTIFIER \"=\" EXPRESSION\".\".EXPRESSION=TERM {\"|\" TERM}.TERM=FACTOR {\" \" FACTOR}.FACTOR=IDENTIFIER|stringLiteral|\"[\" EXPRESSION \"]\"|\"(\" EXPRESSION \")\"|\"{\" EXPRESSION \"}\".IDENTIFIER=letter {letter}.";
            var grammar = WirthSyntaxNotation.GrammarFromString(metaMetaSyntax).ToNfaGrammar();
            grammar.Main = grammar.Productions.First(x => x.Name == "SYNTAX");
            var parser = new Parser(grammar);
            var job = parser.Parse(metaMetaSyntax);
            job.Join();
            var asf = job.AbstractSyntaxGraph;
        }
    }
}