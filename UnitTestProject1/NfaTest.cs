using System;
using System.Collections.Generic.More;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IDE;
using parlex;
using nfa = IDE.Nfa<int, int>;

namespace UnitTestProject1 {
    [TestClass]
    public class NfaTest {
        private static readonly nfa TestNfa;
        static NfaTest() {
            TestNfa = new nfa();
            var state1 = new nfa.State(1);
            var state2 = new nfa.State(2);
            var state3 = new nfa.State(3);
            TestNfa.States.Add(state1);
            TestNfa.States.Add(state2);
            TestNfa.States.Add(state3);
            TestNfa.StartStates.Add(state1);
            TestNfa.AcceptStates.Add(state2);
            TestNfa.AcceptStates.Add(state3);
            TestNfa.TransitionFunction[state1][0].Add(state1);
            TestNfa.TransitionFunction[state1][0].Add(state3);
            TestNfa.TransitionFunction[state1][1].Add(state2);
            TestNfa.TransitionFunction[state2][0].Add(state1);
            TestNfa.TransitionFunction[state2][1].Add(state2);
            TestNfa.TransitionFunction[state2][1].Add(state3);
            TestNfa.TransitionFunction[state3][0].Add(state1);
            TestNfa.TransitionFunction[state3][1].Add(state3);
        }

        [TestMethod]
        public void TestDeterminize() {
            var determinized = TestNfa.Determinize();
        }

        [TestMethod]
        public void TestDual() {
            var dual = TestNfa.Dual();
        }

        [TestMethod]
        public void TestMakeStateMap() {
            Nfa<int, nfa.Configuration> determinized;
            var sm = TestNfa.MakeStateMap(out determinized);
        }

        [TestMethod]
        public void TestReduceStateMap() {
            Nfa<int, nfa.Configuration> determinized;
            var sm = TestNfa.MakeStateMap(out determinized);
            Nfa<int, int> minimizedSubsetConstructionDfa;
            var rsm = nfa.ReduceStateMap(sm, determinized, out minimizedSubsetConstructionDfa);
        }

        [TestMethod]
        public void TestComputePrimeGrids() {
            Nfa<int, nfa.Configuration> determinized;
            var sm = TestNfa.MakeStateMap(out determinized);
            Nfa<int, int> minimizedSubsetConstructionDfa;
            var rsm = nfa.ReduceStateMap(sm, determinized, out minimizedSubsetConstructionDfa);
            var ram = nfa.MakeReducedAutomataMatrix(rsm);
            var primeGrids = nfa.ComputePrimeGrids(ram);
        }

        [TestMethod]
        public void TestEmumerateCovers() {
            Nfa<int, nfa.Configuration> determinized;
            var sm = TestNfa.MakeStateMap(out determinized);
            Nfa<int, int> minimizedSubsetConstructionDfa;
            var rsm = nfa.ReduceStateMap(sm, determinized, out minimizedSubsetConstructionDfa);
            var ram = nfa.MakeReducedAutomataMatrix(rsm);
            var primeGrids = nfa.ComputePrimeGrids(ram);
            var covers = nfa.EnumerateCovers(ram, primeGrids);
            foreach (var enumerateCover in covers) {
            }
        }

        [TestMethod]
        public void TestFromIntersectionRule() {
            Nfa<int, nfa.Configuration> determinized;
            var sm = TestNfa.MakeStateMap(out determinized);
            Nfa<int, int> minimizedSubsetConstructionDfa;
            var rsm = nfa.ReduceStateMap(sm, determinized, out minimizedSubsetConstructionDfa);
            var ram = nfa.MakeReducedAutomataMatrix(rsm);
            var primeGrids = nfa.ComputePrimeGrids(ram);
            var covers = nfa.EnumerateCovers(ram, primeGrids);
            var cover = covers.First();            
            Bimap<int, Nfa<int, int>.Grid> orderedGrids;
            var result = nfa.FromIntersectionRule(minimizedSubsetConstructionDfa, cover, out orderedGrids);
        }

        [TestMethod]
        public void TestSubsetAssignmentIsLegitimate() {
            Nfa<int, nfa.Configuration> determinized;
            var sm = TestNfa.MakeStateMap(out determinized);
            Nfa<int, int> minimizedSubsetConstructionDfa;
            var rsm = nfa.ReduceStateMap(sm, determinized, out minimizedSubsetConstructionDfa);
            var ram = nfa.MakeReducedAutomataMatrix(rsm);
            var primeGrids = nfa.ComputePrimeGrids(ram);
            var covers = nfa.EnumerateCovers(ram, primeGrids);
            var cover = covers.First();
            Bimap<int, nfa.Grid> orderedGrids;
            var minNfa = nfa.FromIntersectionRule(minimizedSubsetConstructionDfa, cover, out orderedGrids);
            var result = nfa.SubsetAssignmentIsLegitimate(minNfa, minimizedSubsetConstructionDfa, ram, orderedGrids);
        }

        [TestMethod]
        public void TestMinimize() {
            var result = TestNfa.Minimized();
        }

        [TestMethod]
        public void TestProductToNfaToExemplarSources() {
            var testFile = File.ReadAllText(@"C:\Users\Brent\Dropbox\parlex\test.parlex");
            var grammarDocument = GrammarDocument.FromString(testFile);
            var compiledGrammar = new CompiledGrammar(grammarDocument);
            var product = compiledGrammar.UserProducts["product_name"];
            var nfa = product.ToNfa();
            nfa = nfa.Minimized();
            var exemplarSources = nfa.ToExemplarSources("test_product");
            foreach (var exemplarSource in exemplarSources) {
                System.Diagnostics.Debug.WriteLine(exemplarSource.ToString());
            }
        }

        [TestMethod]
        public void TestRoundTrip0() {
            var testFile = File.ReadAllText(@"C:\Users\Brent\Dropbox\parlex\test.parlex");
            var grammarDocument = GrammarDocument.FromString(testFile);
            var compiledGrammar = new CompiledGrammar(grammarDocument);
            var aProduct = compiledGrammar.UserProducts["codePoint000041"];
            var state0 = new Nfa<Product, int>.State(0);
            var state1 = new Nfa<Product, int>.State(1);
            var nfa = new Nfa<Product, int>();
            nfa.States.Add(state0);
            nfa.States.Add(state1);
            nfa.StartStates.Add(state0);
            nfa.AcceptStates.Add(state1);
            nfa.TransitionFunction[state0][aProduct].Add(state1);
            var exemplarSources = nfa.ToExemplarSources("test_product");
            String generatedGrammar = exemplarSources.Aggregate("", (current, exemplarSource) => current + (Environment.NewLine + exemplarSource));
            var generatedGrammarDocument = GrammarDocument.FromString(generatedGrammar);
            var generatedCompiledGrammar = new CompiledGrammar(generatedGrammarDocument);
            var roundTripProduct = generatedCompiledGrammar.UserProducts["test_product"];
            var roundTripNfa = roundTripProduct.ToNfa();
            System.Diagnostics.Debug.Assert(roundTripNfa.IsEquivalent(nfa));
        }

        [TestMethod]
        public void TestRoundTrip1() {
            var testFile = File.ReadAllText(@"C:\Users\Brent\Dropbox\parlex\test.parlex");
            var grammarDocument = GrammarDocument.FromString(testFile);
            var compiledGrammar = new CompiledGrammar(grammarDocument);
            var aProduct = compiledGrammar.UserProducts["codePoint000041"];
            var bProduct = compiledGrammar.UserProducts["codePoint000042"];
            var cProduct = compiledGrammar.UserProducts["codePoint000043"];
            var state0 = new Nfa<Product, int>.State(0);
            var state1 = new Nfa<Product, int>.State(1);
            var state2 = new Nfa<Product, int>.State(2);
            var nfa = new Nfa<Product, int>();
            nfa.States.Add(state0);
            nfa.States.Add(state1);
            nfa.States.Add(state2);
            nfa.StartStates.Add(state0);
            nfa.AcceptStates.Add(state1);
            nfa.TransitionFunction[state0][aProduct].Add(state1);
            nfa.TransitionFunction[state1][bProduct].Add(state2);
            nfa.TransitionFunction[state2][cProduct].Add(state1);
            var exemplarSources = nfa.ToExemplarSources("test_product");
            String generatedGrammar = exemplarSources.Aggregate("", (current, exemplarSource) => current + (Environment.NewLine + exemplarSource));
            var generatedGrammarDocument = GrammarDocument.FromString(generatedGrammar);
            var generatedCompiledGrammar = new CompiledGrammar(generatedGrammarDocument);
            var roundTripProduct = generatedCompiledGrammar.UserProducts["test_product"];
            var roundTripNfa = roundTripProduct.ToNfa();
            System.Diagnostics.Debug.Assert(roundTripNfa.IsEquivalent(nfa));
        }

        [TestMethod]
        public void TestNfaToExemplarSourcesEasy() {
            var testFile = File.ReadAllText(@"C:\Users\Brent\Dropbox\parlex\test.parlex");
            var grammarDocument = GrammarDocument.FromString(testFile);
            var compiledGrammar = new CompiledGrammar(grammarDocument);
            var aProduct = compiledGrammar.UserProducts["codePoint000041"];
            var bProduct = compiledGrammar.UserProducts["codePoint000042"];
            var cProduct = compiledGrammar.UserProducts["codePoint000043"];
            var dProduct = compiledGrammar.UserProducts["codePoint000044"];
            var eProduct = compiledGrammar.UserProducts["codePoint000045"];
            var fProduct = compiledGrammar.UserProducts["codePoint000046"];
            var gProduct = compiledGrammar.UserProducts["codePoint000047"];
            var state0 = new Nfa<Product, int>.State(0);
            var state1 = new Nfa<Product, int>.State(1);
            var state2 = new Nfa<Product, int>.State(2);
            var state3 = new Nfa<Product, int>.State(3);
            var state4 = new Nfa<Product, int>.State(4);
            var nfa = new Nfa<Product, int>();
            nfa.States.Add(state0);
            nfa.States.Add(state1);
            nfa.States.Add(state2);
            nfa.States.Add(state3);
            nfa.States.Add(state4);
            nfa.StartStates.Add(state0);
            nfa.AcceptStates.Add(state4);
            nfa.TransitionFunction[state0][aProduct].Add(state1);
            nfa.TransitionFunction[state1][bProduct].Add(state2);
            nfa.TransitionFunction[state1][cProduct].Add(state3);
            nfa.TransitionFunction[state2][dProduct].Add(state0);
            nfa.TransitionFunction[state3][eProduct].Add(state0);
            nfa.TransitionFunction[state2][fProduct].Add(state4);
            nfa.TransitionFunction[state3][gProduct].Add(state4);
            System.Diagnostics.Debug.Assert(nfa.States.Count > 0);
            var exemplarSources = nfa.ToExemplarSources("test_product");
            String generatedGrammar = exemplarSources.Aggregate("", (current, exemplarSource) => current + (Environment.NewLine + exemplarSource));
            var generatedGrammarDocument = GrammarDocument.FromString(generatedGrammar);
            var generatedCompiledGrammar = new CompiledGrammar(generatedGrammarDocument);
            var roundTripProduct = generatedCompiledGrammar.UserProducts["test_product"];
            var roundTripNfa = roundTripProduct.ToNfa().Minimized();
            nfa = nfa.Minimized();
            System.Diagnostics.Debug.Assert(roundTripNfa.IsEquivalent(nfa));
        }

        [TestMethod]
        public void TestNfaToExemplarSourcesMedium() {
            var testFile = File.ReadAllText(@"C:\Users\Brent\Dropbox\parlex\test.parlex");
            var grammarDocument = GrammarDocument.FromString(testFile);
            var compiledGrammar = new CompiledGrammar(grammarDocument);
            var aProduct = compiledGrammar.UserProducts["codePoint000041"];
            var bProduct = compiledGrammar.UserProducts["codePoint000042"];
            var cProduct = compiledGrammar.UserProducts["codePoint000043"];
            var dProduct = compiledGrammar.UserProducts["codePoint000044"];
            var eProduct = compiledGrammar.UserProducts["codePoint000045"];
            var fProduct = compiledGrammar.UserProducts["codePoint000046"];
            var gProduct = compiledGrammar.UserProducts["codePoint000047"];
            var hProduct = compiledGrammar.UserProducts["codePoint000048"];
            var state0 = new Nfa<Product, int>.State(0);
            var state1 = new Nfa<Product, int>.State(1);
            var state2 = new Nfa<Product, int>.State(2);
            var state3 = new Nfa<Product, int>.State(3);
            var state4 = new Nfa<Product, int>.State(4);
            var nfa = new Nfa<Product, int>();
            nfa.States.Add(state0);
            nfa.States.Add(state1);
            nfa.States.Add(state2);
            nfa.States.Add(state3);
            nfa.States.Add(state4);
            nfa.StartStates.Add(state0);
            nfa.AcceptStates.Add(state4);
            nfa.TransitionFunction[state0][aProduct].Add(state1);
            nfa.TransitionFunction[state1][bProduct].Add(state2);
            nfa.TransitionFunction[state1][cProduct].Add(state3);
            nfa.TransitionFunction[state2][dProduct].Add(state0);
            nfa.TransitionFunction[state3][eProduct].Add(state0);
            nfa.TransitionFunction[state2][fProduct].Add(state4);
            nfa.TransitionFunction[state3][gProduct].Add(state4);
            nfa.TransitionFunction[state0][hProduct].Add(state2);
            var exemplarSources = nfa.ToExemplarSources("test_product");
            String generatedGrammar = exemplarSources.Aggregate("", (current, exemplarSource) => current + (Environment.NewLine + exemplarSource));
            var generatedGrammarDocument = GrammarDocument.FromString(generatedGrammar);
            var generatedCompiledGrammar = new CompiledGrammar(generatedGrammarDocument);
            var roundTripProduct = generatedCompiledGrammar.UserProducts["test_product"];
            var roundTripNfa = roundTripProduct.ToNfa();
            System.Diagnostics.Debug.Assert(roundTripNfa.IsEquivalent(nfa));
        }

        [TestMethod]
        public void TestNfaToExemplarSourcesHard() {
            var testFile = File.ReadAllText(@"C:\Users\Brent\Dropbox\parlex\test.parlex");
            var grammarDocument = GrammarDocument.FromString(testFile);
            var compiledGrammar = new CompiledGrammar(grammarDocument);
            var aProduct = compiledGrammar.UserProducts["codePoint000041"];
            var bProduct = compiledGrammar.UserProducts["codePoint000042"];
            var cProduct = compiledGrammar.UserProducts["codePoint000043"];
            var dProduct = compiledGrammar.UserProducts["codePoint000044"];
            var eProduct = compiledGrammar.UserProducts["codePoint000045"];
            var fProduct = compiledGrammar.UserProducts["codePoint000046"];
            var gProduct = compiledGrammar.UserProducts["codePoint000047"];
            var hProduct = compiledGrammar.UserProducts["codePoint000048"];
            var iProduct = compiledGrammar.UserProducts["codePoint000049"];
            var state0 = new Nfa<Product, int>.State(0);
            var state1 = new Nfa<Product, int>.State(1);
            var state2 = new Nfa<Product, int>.State(2);
            var state3 = new Nfa<Product, int>.State(3);
            var state4 = new Nfa<Product, int>.State(4);
            var nfa = new Nfa<Product, int>();
            nfa.States.Add(state0);
            nfa.States.Add(state1);
            nfa.States.Add(state2);
            nfa.States.Add(state3);
            nfa.States.Add(state4);
            nfa.StartStates.Add(state0);
            nfa.AcceptStates.Add(state4);
            nfa.TransitionFunction[state0][aProduct].Add(state1);
            nfa.TransitionFunction[state1][bProduct].Add(state2);
            nfa.TransitionFunction[state1][cProduct].Add(state3);
            nfa.TransitionFunction[state2][dProduct].Add(state0);
            nfa.TransitionFunction[state3][eProduct].Add(state0);
            nfa.TransitionFunction[state2][fProduct].Add(state4);
            nfa.TransitionFunction[state3][gProduct].Add(state4);
            nfa.TransitionFunction[state0][hProduct].Add(state2);
            nfa.TransitionFunction[state4][iProduct].Add(state1);
            var exemplarSources = nfa.ToExemplarSources("test_product");
            String generatedGrammar = exemplarSources.Aggregate("", (current, exemplarSource) => current + (Environment.NewLine + exemplarSource));
            var generatedGrammarDocument = GrammarDocument.FromString(generatedGrammar);
            var generatedCompiledGrammar = new CompiledGrammar(generatedGrammarDocument);
            var roundTripProduct = generatedCompiledGrammar.UserProducts["test_product"];
            var roundTripNfa = roundTripProduct.ToNfa();
            System.Diagnostics.Debug.Assert(roundTripNfa.IsEquivalent(nfa));
        }

    }
}
