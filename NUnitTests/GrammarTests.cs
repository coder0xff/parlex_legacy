using System;
using NUnit.Framework;
using Parlex;

namespace NUnitTests {
    [TestFixture]
    public class GrammarTests {
        [Test]
        public void TestSerialization() {
            String s = WirthSyntaxNotation.SaveGrammar(WirthSyntaxNotation.WorthSyntaxNotationParserGrammar);
            System.Diagnostics.Debug.WriteLine(s);
        }
    }
}