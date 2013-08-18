using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace IDE {
    /// <summary>
    /// A Nondeterministic Finite Automaton (∪-NFA)
    /// </summary>
    /// <typeparam name="TAlphabet">The domain of the transition function is S x TAlphabet, where S is the set of states.</typeparam>
    /// <typeparam name="TAssignment">The type of the value associated with a state.</typeparam>
    public partial class Nfa<TAlphabet, TAssignment> {

        /// <summary>
        /// A State of an NFA
        /// </summary>
        public class State {
            public TAssignment value;

            public State(TAssignment value) {
                this.value = value;
            }
        }

        public HashSet<State> States = new HashSet<State>();
        private readonly State _nullState = new State(default(TAssignment));
        public readonly JaggedAutoDictionary<State, TAlphabet, HashSet<State>> TransitionFunction = new JaggedAutoDictionary<State, TAlphabet, HashSet<State>>(() => new HashSet<State>());
        public readonly HashSet<State> StartStates = new HashSet<State>();
        public HashSet<State> AcceptStates = new HashSet<State>();

        public HashSet<State> TransitionFunctionEx(IEnumerable<State> states, TAlphabet input) {
            var result = new HashSet<State>();
            foreach (var state in states) {
                result.UnionWith(TransitionFunction[state][input]);
            }
            return result;
        }

        public HashSet<State> TransitionFunctionEx(State state, IEnumerable<TAlphabet> inputString) {
            var currentConfiguration = new HashSet<State> {state};
            return inputString.Aggregate(currentConfiguration, TransitionFunctionEx);
        }

        public HashSet<State> TransitionFunctionEx(IEnumerable<State> states, IEnumerable<TAlphabet> inputString) {
            var currentConfiguration = new HashSet<State>(states);
            return inputString.Aggregate(currentConfiguration, TransitionFunctionEx);
        }

        internal struct Configuration : IEnumerable<State> {
            public bool Equals(Configuration other) {
                return _states.SetEquals(other._states);
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) {
                    return false;
                }
                return obj is Configuration && Equals((Configuration)obj);
            }

            public override int GetHashCode() {
                return _hashCode;
            }

            public static bool operator ==(Configuration left, Configuration right) {
                return left.Equals(right);
            }

            public static bool operator !=(Configuration left, Configuration right) {
                return !left.Equals(right);
            }

            private readonly HashSet<State> _states;
            private readonly int _hashCode;

            public Configuration(HashSet<State> states)
                : this() {
                _states = states;
                _hashCode = 0;
                foreach (var state in states) {
                    _hashCode = _hashCode * 397 ^ state.GetHashCode();
                }
            }

            public IEnumerator<State> GetEnumerator() {
                return _states.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
                return _states.GetEnumerator();
            }
        }

        public Nfa<TAlphabet, HashSet<State>> Determinize() {
            var configurationToDState = new ConcurrentDictionary<Configuration, Nfa<TAlphabet, HashSet<State>>.State>();
            var result = new Nfa<TAlphabet, HashSet<State>>();
            var resultAcceptStates = new ConcurrentBag<Nfa<TAlphabet, HashSet<State>>.State>();

            Func<Configuration, Nfa<TAlphabet, HashSet<State>>.State> adder = null;
            adder = (Configuration configuration) => configurationToDState.GetOrAdd(configuration, configurationProxy => {
                var newState = new Nfa<TAlphabet, HashSet<State>>.State(new HashSet<State>(configurationProxy));
                Task.Factory.StartNew(() => {
                    bool isAcceptState = configurationProxy.Any(x => AcceptStates.Contains(x));
                    if (isAcceptState) {
                        resultAcceptStates.Add(newState);
                    }
                    IEnumerable<TAlphabet> awayInputs = configurationProxy.Select(x => TransitionFunction[x]).SelectMany(y => y.Keys).Distinct();
                    Parallel.ForEach(awayInputs, input => {
                        HashSet<State> nextConfiguration = TransitionFunctionEx(configurationProxy, input);
                        Nfa<TAlphabet, HashSet<State>>.State nextState = adder(new Configuration(nextConfiguration));
                        result.TransitionFunction[newState][input] = new HashSet<Nfa<TAlphabet, HashSet<State>>.State> {nextState};
                    });
                }, TaskCreationOptions.AttachedToParent);
                return newState;
            });

            var startConfiguration = new Configuration(StartStates);
            Task.Factory.StartNew(() => {
                result.StartStates.Add(adder(startConfiguration));
            }).Wait();
            foreach (var value in configurationToDState.Values) {
                result.States.Add(value);
            }
            result.AcceptStates = new HashSet<Nfa<TAlphabet, HashSet<State>>.State>(resultAcceptStates);
            return result;
        }
        
    }
}
