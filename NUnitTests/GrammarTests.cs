﻿using System;
using System.Diagnostics;
using NUnit.Framework;
using Parlex;

namespace NUnitTests {
    [TestFixture]
    public class GrammarTests {
        [Test]
        public void TestSerialization() {
            String s = WirthSyntaxNotation.GrammarToString(WirthSyntaxNotation.WorthSyntaxNotationParserGrammar);
            Debug.WriteLine(s);
        }
    }
}