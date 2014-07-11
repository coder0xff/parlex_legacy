using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parlex;

namespace Test
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            Grammar g = new Grammar();
            Grammar.Recognizer identifier = new Grammar.Recognizer("identifier", true);
            Grammar.Recognizer.State identifier0 = new Grammar.Recognizer.State();
            Grammar.Recognizer.State identifier1 = new Grammar.Recognizer.State();
            identifier.States.Add(identifier0);
            identifier.States.Add(identifier1);
            identifier.StartStates.Add(identifier0);
            identifier.AcceptStates.Add(identifier1);
            identifier.TransitionFunction[identifier0][Grammar.LetterTerminal].Add(identifier1);
            identifier.TransitionFunction[identifier1][Grammar.LetterTerminal].Add(identifier1);

            Grammar.Recognizer syntax = new Grammar.Recognizer("syntax", false);
            Grammar.Recognizer.State syntax0 = new Grammar.Recognizer.State();
            Grammar.Recognizer.State syntax1 = new Grammar.Recognizer.State();
            Grammar.Recognizer.State syntax2 = new Grammar.Recognizer.State();
            Grammar.Recognizer.State syntax3 = new Grammar.Recognizer.State();
            syntax.States.Add(syntax0);
            syntax.States.Add(syntax1);
            syntax.States.Add(syntax2);
            syntax.States.Add(syntax3);
            syntax.StartStates.Add(syntax0);
            syntax.AcceptStates.Add(syntax3);
            syntax.TransitionFunction[syntax0][identifier].Add(syntax1);
            syntax.TransitionFunction[syntax1][new Grammar.StringTerminal("=")].Add(syntax2);
            syntax.TransitionFunction[syntax2][identifier].Add(syntax3);

            g.Productions.Add(syntax);
            g.Productions.Add(identifier);
            g.MainProduction = syntax;

            Parser p = new Parser(g);
            Parser.Job j = p.Parse("A=B");
            j.Wait();
            Parser.AbstractSyntaxForest asf = j.abstractSyntaxForest;
        }
    }
}
