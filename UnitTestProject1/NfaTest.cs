using System;
using System.Collections.Generic.More;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IDE;

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
            var result = TestNfa.Minimize();
        }
    }
}
