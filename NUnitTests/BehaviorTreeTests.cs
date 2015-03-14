using System;
using System.Globalization;
using System.Linq;
using Automata;
using NUnit.Framework;
using Parlex;

namespace NUnitTests {
    [TestFixture]
    public class BehaviorTreeTests {
        private static Nfa<ISymbol> MakeNfa() {
            var rnd = new Random();
            var nfa = new Nfa<ISymbol>();
            while (true) {
                nfa = new Nfa<ISymbol>();
                int stateCount = rnd.Next(4, 10);
                int transitionCount = 1;
                for (int i = 0; i < stateCount; ++i) {
                    var state = new Nfa<ISymbol>.State();
                    nfa.States.Add(state);
                    if (i == 0 || rnd.NextDouble() > 0.95) {
                        nfa.StartStates.Add(state);
                    }
                    if (i == stateCount - 1 || rnd.NextDouble() > 0.95) {
                        nfa.AcceptStates.Add(state);
                    }
                    if (i < stateCount/2) {
                        transitionCount *= (i + 1); //compute (N/2)! while where at it
                    }
                }
                Nfa<ISymbol>.State[] indexableStates = nfa.States.ToArray();
                for (int i = 0; i < transitionCount; ++i) {
                    var symbol = new Grammar.StringTerminal(('A' + i).ToString(CultureInfo.InvariantCulture));
                    Nfa<ISymbol>.State fromState = indexableStates[rnd.Next(0, stateCount)];
                    Nfa<ISymbol>.State toState = indexableStates[rnd.Next(0, stateCount)];
                    nfa.TransitionFunction[fromState][symbol].Add(toState);
                }
                nfa = nfa.Minimized();
                if (nfa.States.Count == 0 || nfa.StartStates.Count == 0 || nfa.AcceptStates.Count == 0) {
                    continue;
                }
                foreach (Nfa<ISymbol>.State startState in nfa.StartStates) {
                    foreach (Nfa<ISymbol>.State acceptState in nfa.AcceptStates) {
                        if (nfa.GetRoutes(startState, acceptState).Any()) {
                            return nfa;
                        }
                    }
                }
            }
        }

        [Test]
        public void Test() {
            Nfa<ISymbol> nfa = MakeNfa();
            new BehaviorTree(nfa);
        }
    }
}