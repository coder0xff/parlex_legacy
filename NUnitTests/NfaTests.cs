using System.Diagnostics;
using System.Linq;
using Automata;
using NUnit.Framework;

namespace NUnitTests {
    [TestFixture ()]
    public class NfaTests
    {
        [Test ()]
        public void TestDeterminize()
        {
            for (int counter = 0; counter < 10; counter++)
            {
                Nfa<char, int>.State state0 = new Nfa<char, int>.State(0);
                Nfa<char, int>.State state1 = new Nfa<char, int>.State(1);
                Nfa<char, int> nfa = new Nfa<char, int>();
                nfa.States.Add(state1);
                nfa.States.Add(state0);
                nfa.StartStates.Add(state0);
                nfa.AcceptStates.Add(state1);
                nfa.TransitionFunction[state0]['A'].Add(state1);
                var result = nfa.Determinize();
                Debug.Assert(result.States.Count == 2);
                Debug.Assert(result.StartStates.Count == 1);
                Debug.Assert(result.AcceptStates.Count == 1);
                var resultStartState = result.StartStates.First();
                Debug.Assert(result.TransitionFunction[resultStartState].Keys.Contains('A'));
                Debug.Assert(result.TransitionFunction[resultStartState]['A'].Count == 1);
                var resultAcceptState = result.TransitionFunction[resultStartState]['A'].First();
                Debug.Assert(resultStartState != resultAcceptState);
            }
        }

        [Test]
        public void TestMinimize() {
            for (int counter = 0; counter < 10; counter++) {
                Nfa<char, int>.State state0 = new Nfa<char, int>.State(0);
                Nfa<char, int>.State state1 = new Nfa<char, int>.State(1);
                Nfa<char, int> nfa = new Nfa<char, int>();
                nfa.States.Add(state1);
                nfa.States.Add(state0);
                nfa.StartStates.Add(state0);
                nfa.AcceptStates.Add(state1);
                nfa.TransitionFunction[state0]['A'].Add(state1);
                var result = nfa.Minimized();
                Debug.Assert(result.States.Count == 2);
                Debug.Assert(result.StartStates.Count == 1);
                Debug.Assert(result.AcceptStates.Count == 1);
                var resultStartState = result.StartStates.First();
                Debug.Assert(result.TransitionFunction[resultStartState].Keys.Contains('A'));
                Debug.Assert(result.TransitionFunction[resultStartState]['A'].Count == 1);
                var resultAcceptState = result.TransitionFunction[resultStartState]['A'].First();
                Debug.Assert(resultStartState != resultAcceptState);
            }
        }
    }
}