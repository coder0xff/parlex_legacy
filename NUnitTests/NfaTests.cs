using System.Diagnostics;
using System.Linq;
using Automata;
using NUnit.Framework;

namespace NUnitTests {
    [TestFixture]
    public class NfaTests {
        [Test]
        public void TestDeterminize() {
            for (int counter = 0; counter < 10; counter++) {
                var state0 = new Nfa<char, int>.State(0);
                var state1 = new Nfa<char, int>.State(1);
                var nfa = new Nfa<char, int>();
                nfa.States.Add(state1);
                nfa.States.Add(state0);
                nfa.StartStates.Add(state0);
                nfa.AcceptStates.Add(state1);
                nfa.TransitionFunction[state0]['A'].Add(state1);
                Nfa<char, Nfa<char, int>.StateSet> result = nfa.Determinize();
                Debug.Assert(result.States.Count == 2);
                Debug.Assert(result.StartStates.Count == 1);
                Debug.Assert(result.AcceptStates.Count == 1);
                Nfa<char, Nfa<char, int>.StateSet>.State resultStartState = result.StartStates.First();
                Debug.Assert(result.TransitionFunction[resultStartState].Keys.Contains('A'));
                Debug.Assert(result.TransitionFunction[resultStartState]['A'].Count == 1);
                Nfa<char, Nfa<char, int>.StateSet>.State resultAcceptState = result.TransitionFunction[resultStartState]['A'].First();
                Debug.Assert(resultStartState != resultAcceptState);
            }
        }

        [Test]
        public void TestMinimize() {
            for (int counter = 0; counter < 10; counter++) {
                var state0 = new Nfa<char, int>.State(0);
                var state1 = new Nfa<char, int>.State(1);
                var nfa = new Nfa<char, int>();
                nfa.States.Add(state1);
                nfa.States.Add(state0);
                nfa.StartStates.Add(state0);
                nfa.AcceptStates.Add(state1);
                nfa.TransitionFunction[state0]['A'].Add(state1);
                Nfa<char, int> result = nfa.Minimized();
                Debug.Assert(result.States.Count == 2);
                Debug.Assert(result.StartStates.Count == 1);
                Debug.Assert(result.AcceptStates.Count == 1);
                Nfa<char, int>.State resultStartState = result.StartStates.First();
                Debug.Assert(result.TransitionFunction[resultStartState].Keys.Contains('A'));
                Debug.Assert(result.TransitionFunction[resultStartState]['A'].Count == 1);
                Nfa<char, int>.State resultAcceptState = result.TransitionFunction[resultStartState]['A'].First();
                Debug.Assert(resultStartState != resultAcceptState);
            }
        }
    }
}