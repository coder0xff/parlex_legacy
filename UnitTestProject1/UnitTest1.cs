using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IDE;

using nfa = IDE.Nfa<int, int>;

namespace UnitTestProject1 {
    [TestClass]
    public class UnitTest1 {
        private static readonly nfa TestNfa;
        static UnitTest1() {
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
            var sm = TestNfa.MakeStateMap();
        }

        [TestMethod]
        public void TestReduceStateMap() {
            var sm = TestNfa.MakeStateMap();
            var rsm = nfa.ReduceStateMap(sm);
        }

        [TestMethod]
        public void TestComputePrimeGrids() {
            var sm = TestNfa.MakeStateMap();
            var rsm = nfa.ReduceStateMap(sm);
            var primeGrids = nfa.ComputePrimeGrids(rsm);
        }
    }
}
