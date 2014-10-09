using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Automata;
using Parlex;
using NUnit.Framework;

namespace NUnitTests {
    [TestFixture ()]
    public class BehaviorTreeTests
    {
        static Nfa<Grammar.ISymbol> MakeNfa() {
            var rnd = new Random();
            var nfa = new Nfa<Grammar.ISymbol>();
            while (true) {
                nfa = new Nfa<Grammar.ISymbol>();
                var stateCount = rnd.Next(4, 10);
                var transitionCount = 1;
                for (var i = 0; i < stateCount; ++i) {
                    var state = new Nfa<Grammar.ISymbol>.State();
                    nfa.States.Add(state);
                    if (i == 0 || rnd.NextDouble() > 0.95) {
                        nfa.StartStates.Add(state);
                    }
                    if (i == stateCount - 1 || rnd.NextDouble() > 0.95) {
                        nfa.AcceptStates.Add(state);
                    }
                    if (i < stateCount / 2) {
                        transitionCount *= (i + 1); //compute (N/2)! while where at it
                    }
                }
                var indexableStates = nfa.States.ToArray();
                for (var i = 0; i < transitionCount; ++i) {
                    var symbol = new Grammar.StringTerminal(('A' + i).ToString(CultureInfo.InvariantCulture));
                    var fromState = indexableStates[rnd.Next(0, stateCount)];
                    var toState = indexableStates[rnd.Next(0, stateCount)];
                    nfa.TransitionFunction[fromState][symbol].Add(toState);
                }
                nfa = nfa.Minimized();
                if (nfa.States.Count == 0 || nfa.StartStates.Count == 0 || nfa.AcceptStates.Count == 0) continue;
                foreach (var startState in nfa.StartStates) {
                    foreach (var acceptState in nfa.AcceptStates) {
                        if (nfa.GetRoutes(startState, acceptState).Any()) return nfa;
                    }
                }
            }
            return nfa;
        }

        [Test ()]
        public void Test() {
            var nfa = MakeNfa();
            new BehaviorTree(nfa);
        }
    }
}