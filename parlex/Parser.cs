using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Concurrent.More;
using System.Linq;

namespace parlex {
	public class Parser {
		public Parser(Grammar grammar) {
			this.grammar = grammar;
		}

		internal Grammar grammar;

		/// <summary>
		/// Represents a successful matching by a Recognizer. The permutation in Match.children is unique at the Match's location and length (though these values are not stored here).
		/// </summary>
		internal class Match {
			public int parseLength;
			public List<Tuple<Grammar.Symbol, int /*parseLength */>> children;

			public Match(int parseLength, List<Tuple<Grammar.Symbol, int /*parseLength */>> children) {
				this.parseLength = parseLength;
				this.children = children;
			}
		}

		/// <summary>
		/// Searches for MatchGroups using the specified Grammar.Recognizer, at the specified location
		/// SubJob receives matches from its recognizer state instances, and transmits match lengths in response.
		/// This list of MatchGroups can be retrieved, forming a portion of the Abstract Syntax Graph
		/// </summary>
		class RecognizerSubJob : IReceiver<Match>, ITransmitter<Tuple<Grammar.Symbol, int /*parseLength*/>> {
			Job owner;
			JaggedAutoDictionary<int, List<Match>> matches = new JaggedAutoDictionary<int, List<Match>>(_ => new List<Match>());

			public Job job {
				get {
					return owner;
				}
			}

			Grammar.Recognizer recognizer;

			public Grammar.Recognizer Recognizer {
				get {
					return recognizer;
				}
			}

			int documentPosition;

			public int DocumentPosition {
				get {
					return documentPosition;
				}
			}

			public RecognizerSubJob(Job owner, Grammar.Recognizer recognizer, int documentPosition) {
                System.Diagnostics.Debug.WriteLine("Created sub job: " + recognizer.Name + " (" + documentPosition + ")");
                System.Threading.Interlocked.Increment(ref owner.runningSubJobCount);
				this.owner = owner;
				this.recognizer = recognizer;
				this.documentPosition = documentPosition;
				owner.Register(documentPosition, recognizer, (IReceiver<Match>)this);
				owner.Register(documentPosition, recognizer, (ITransmitter<Tuple<Grammar.Symbol, int /* parseLength */>>)this);
				new RecognizerState(owner, documentPosition, recognizer);
			}

			#region IReceiver implementation

			public Action<Match> GetReceiveHandler() {
				return (Match match) => {
                    System.Diagnostics.Debug.WriteLine("Sub job received match: " + recognizer.Name + " (" + documentPosition + ", " + match.parseLength + ")");
					matches[match.parseLength].Add(match);
					Transmit(new Tuple<Grammar.Symbol, int>(recognizer as Grammar.Symbol, match.parseLength));
				};
			}

			public Action GetTerminateHandler() {
				return () => {
                    System.Diagnostics.Debug.WriteLine("Terminating sub job: " + recognizer.Name + " (" + documentPosition + ")");
					Terminate();
                    if (System.Threading.Interlocked.Decrement(ref owner.runningSubJobCount) == 0)
                    {
                        owner.OnFinished();
                    }
                };
			}

			#endregion

			#region ITransmitter implementation

			public event Action<Tuple<Grammar.Symbol, int>> Transmit;
			public event Action Terminate;

			#endregion
		}

		/// <summary>
		/// Stores a reference to a SubParseJobInfo, and stores the current states of the recognizer (which is a NFA)
		/// It also stores information regarding previous transition symbols, so that when an accept state is terminated upon, a record of all the transitions can be compiled into the Abstract Syntax Graph
		/// It depends on the MatchGroups returned by ParseSubJobs, and (on completing recognition) returns new Matches through it's IProvider interface
		/// </summary>
		class RecognizerState : IReceiver<Tuple<Grammar.Symbol, int /*parseLength */>>, ITransmitter<Match> {
            /// <summary>
            /// The job that owns this instance
            /// </summary>
            Job job;
            /// <summary>
            /// The document position that the recognizer began parsing at 
            /// </summary>
            int startingDocumentPosition;
            /// <summary>
            /// The recognizer that being used to parse
            /// </summary>
            Grammar.Recognizer recognizer;
			/// <summary>
			/// The state of the recognizer for subParseJobInfo.symbol that this ParseState represents
			/// </summary>
			Grammar.Recognizer.State[] recognizerNfaState;
			/// <summary>
			/// A history of the transition symbols that led to the current NFA state, which are used when each satisfaction of the PositionedMatchJobInfo occurs, as a means of creating the hierarchy of the resulting Abstract Syntax Graph
			/// </summary>
			List<Tuple<Grammar.Symbol, int /*parseLength */>> transitionHistory;
			/// <summary>
			/// The document position that the next transition symbol will be read from
			/// </summary>
			int currentDocumentPosition;
            /// <summary>
            /// The number of unique symbols that this recognizer state can use to transition, minus the number of signals to terminate;
            /// </summary>
            int numberOfUnterminatedSymbolDependencies;

			public RecognizerState(Job job, int startingDocumentPosition, Grammar.Recognizer recognizer) {
                this.job = job;
                this.recognizer = recognizer;
				this.recognizerNfaState = recognizer.StartStates.ToArray();
				this.transitionHistory = new List<Tuple<Grammar.Symbol, int /*parseLength */>>();
				this.currentDocumentPosition = startingDocumentPosition;
                System.Diagnostics.Debug.WriteLine("Creating recognizer state: " + recognizer.Name + "(" + startingDocumentPosition + ")");
                job.Register(currentDocumentPosition, recognizer, (ITransmitter<Match>)this);
				registerAsReceiver();
			}

			RecognizerState(Job job, int startingDocumentPosition, Grammar.Recognizer recognizer, Grammar.Recognizer.State[] recognizerNfaState, List<Tuple<Grammar.Symbol, int /*parseLength */>> transitionHistory, int currentDocumentPosition) {
                this.job = job;
                this.startingDocumentPosition = startingDocumentPosition;
                this.recognizer = recognizer;
				this.recognizerNfaState = recognizerNfaState.ToArray();
				this.transitionHistory = transitionHistory;
				this.currentDocumentPosition = currentDocumentPosition;
                System.Diagnostics.Debug.WriteLine("Creating recognizer state: " + recognizer.Name + "(" + startingDocumentPosition + ", " + currentDocumentPosition + ")");
                job.Register(currentDocumentPosition, recognizer, (ITransmitter<Match>)this);
				registerAsReceiver();
			}

			public bool IsAcceptState { get { return recognizerNfaState.Any(x => recognizer.AcceptStates.Contains(x)); } }

			void registerAsReceiver() {
				HashSet<Grammar.Symbol> possibleTransitionSymbols = new HashSet<Grammar.Symbol>();
				foreach (Grammar.Recognizer.State nfaState in recognizerNfaState) {
					foreach (Grammar.Symbol transitionSymbol in recognizer.TransitionFunction[nfaState].Keys) {
						possibleTransitionSymbols.Add(transitionSymbol);
					}
				}
				if (IsAcceptState && job.parser.grammar.EatWhiteSpaceAfterProductions) {
					possibleTransitionSymbols.Add(Grammar.WhiteSpaceTerminal);
				}
                numberOfUnterminatedSymbolDependencies = possibleTransitionSymbols.Count;
				foreach (Grammar.Symbol possibleTransitionSymbol in possibleTransitionSymbols) {
                    job.Register(currentDocumentPosition, possibleTransitionSymbol, (IReceiver<Tuple<Grammar.Symbol, int /*parseLength */>>)this);
				}
			}

			#region IReceiver implementation

			public Action<Tuple<Grammar.Symbol, int /*parseLength */>> GetReceiveHandler() {
				return (Tuple<Grammar.Symbol, int /*parseLength */> generalizedMatchInfo) => {
					Grammar.Symbol transitionSymbol = generalizedMatchInfo.Item1;
					int length = generalizedMatchInfo.Item2;
                    System.Diagnostics.Debug.WriteLine("Recognizer state received generalized match info. Recognizer: " + recognizer.Name + " Symbol: " + transitionSymbol.ToString() + " Length: " + length);
					HashSet<Grammar.Recognizer.State> nfaStatesAfterTransition = new HashSet<Nfa<Grammar.Symbol, int>.State>();
					foreach (Grammar.Recognizer.State fromState in recognizerNfaState) {
						foreach (Grammar.Recognizer.State toState in recognizer.TransitionFunction[fromState][transitionSymbol]) {
							nfaStatesAfterTransition.Add(toState);
						}
					}
					if (nfaStatesAfterTransition.Count == 0 && job.parser.grammar.EatWhiteSpaceAfterProductions && IsAcceptState) {
						var updatedTransitionHistory = new List<Tuple<Grammar.Symbol, int /*parseLength */>>(transitionHistory);
						updatedTransitionHistory.Add(generalizedMatchInfo);
						new RecognizerState(job, startingDocumentPosition, recognizer, recognizerNfaState.ToArray(), updatedTransitionHistory, currentDocumentPosition + length);
					} else {
						var updatedTransitionHistory = new List<Tuple<Grammar.Symbol, int /*parseLength */>>(transitionHistory);
						updatedTransitionHistory.Add(generalizedMatchInfo);
						new RecognizerState(job, startingDocumentPosition, recognizer, nfaStatesAfterTransition.ToArray(), updatedTransitionHistory, currentDocumentPosition + length);
					}
				};
			}

			public Action GetTerminateHandler() {
				return () => {
                    if (System.Threading.Interlocked.Decrement(ref numberOfUnterminatedSymbolDependencies) == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Terminating recognizer parse state: " + recognizer.Name + " start position: " + startingDocumentPosition + " current position: " + currentDocumentPosition);
                        if (IsAcceptState)
                        {
                            int parseLength = currentDocumentPosition - startingDocumentPosition;
                            Match match = new Match(parseLength, transitionHistory);
                            Transmit(match);
                        }
                        Terminate();
                    }
				};
			}

			#endregion

			#region ITransmitter implementation

			public event Action<Match> Transmit;
			public event Action Terminate;

			#endregion
		}

		class TerminalSubJob {
			public TerminalSubJob(Job owner, Grammar.Terminal terminal, int documentPosition) {
			}
		}

		public class Job {
			internal Parser parser;
			String text;

			public String Text {
				get {
					return text;
				}
			}

			Int32[] textAsUTF32;

			public Int32[] TextAsUTF32 {
				get {
					return textAsUTF32;
				}
			}

            internal int runningSubJobCount = 0;
            System.Threading.ManualResetEvent finished = new System.Threading.ManualResetEvent(false);

            internal void OnFinished()
            {
                finished.Set();
            }

            public void Wait() {
                finished.WaitOne();
            }

			class TerminalITransmitter : ITransmitter<Tuple<Grammar.Symbol, int /* parseLength */>> {
				#region ITransmitter implementation

				public event Action<Tuple<Grammar.Symbol, int /* parseLength */>> Transmit;
				public event Action Terminate;

				internal void DoTransmit(Tuple<Grammar.Symbol, int /* parseLength */> value) {
					Transmit(value);
				}

				internal void DoTerminate() {
					Terminate();
				}

				#endregion
			}

			internal Job(Parser parser, String text) {
				this.parser = parser;
				this.text = text;
				this.textAsUTF32 = text.GetUtf32CodePoints();

				symbolLengthTransponders = new JaggedAutoDictionary<int, Grammar.Symbol, Transponder<Tuple<Grammar.Symbol, int /* parseLength */>>>((int documentPosition, Grammar.Symbol symbol) => {
					Transponder<Tuple<Grammar.Symbol, int /* parseLength */>> result = new Transponder<Tuple<Grammar.Symbol, int /* parseLength */>>();
					Grammar.Terminal symbolAsTerminal = symbol as Grammar.Terminal;
					if (symbolAsTerminal != null) {
						TerminalITransmitter terminalITransmitter = new TerminalITransmitter();
						result.Register(terminalITransmitter);
						if (symbolAsTerminal.Matches(textAsUTF32, documentPosition)) {
							terminalITransmitter.DoTransmit(new Tuple<Grammar.Symbol, int /* parseLength */>(symbolAsTerminal, symbolAsTerminal.Length));
						}
						terminalITransmitter.DoTerminate();
					} else {
                        //This case is handled by Job.Register
					}
					return result;
				});

				matchTransponders = new JaggedAutoDictionary<int, Grammar.Symbol, Transponder<Match>>((int documentPosition, Grammar.Symbol symbol) => {
					Transponder<Match> result = new Transponder<Match>();
                    return result;
				});

                new RecognizerSubJob(this, parser.grammar.MainProduction, 0);
			}

			JaggedAutoDictionary<int /* documentPosition */, Grammar.Symbol, Transponder<Tuple<Grammar.Symbol, int /*parseLength */>>> symbolLengthTransponders;
			JaggedAutoDictionary<int /* documentPosition */, Grammar.Symbol, Transponder<Match>> matchTransponders;
            JaggedAutoDictionary<int /* documentPosition */, Grammar.Symbol, RecognizerSubJob> subJobs;

			internal void Register(int documentPosition, Grammar.Symbol symbol, IReceiver<Tuple<Grammar.Symbol, int /*parseLength */>> receiver) {
				symbolLengthTransponders[documentPosition][symbol].Register(receiver);
                Grammar.Terminal symbolAsTerminal = symbol as Grammar.Terminal;
                if (symbolAsTerminal != null)
                {
                    //This case is handled in the transponder constructor lambda
                }
                else
                {
                    new RecognizerSubJob(this, symbol as Grammar.Recognizer, documentPosition);
                }
            }

            internal void Register(int documentPosition, Grammar.Symbol symbol, ITransmitter<Tuple<Grammar.Symbol, int /*parseLength */>> transmitter)
            {
                symbolLengthTransponders[documentPosition][symbol].Register(transmitter);
            }

			internal void Register(int documentPosition, Grammar.Symbol symbol, IReceiver<Match> receiver) {
				matchTransponders[documentPosition][symbol].Register(receiver);
			}

            internal void Register(int documentPosition, Grammar.Symbol symbol, ITransmitter<Match> transmitter)
            {
                matchTransponders[documentPosition][symbol].Register(transmitter);
            }
		}

		public Job Parse(String text) {
			return new Job(this, text);
		}
	}
}

