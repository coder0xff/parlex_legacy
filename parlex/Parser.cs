using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Concurrent.More;
using System.Linq;

namespace parlex
{
	public class Parser
	{
		public Parser (Grammar grammar)
		{
			g = grammar;
		}

		Grammar g;

		/// <summary>
		/// Represents a successful matching by a Recognizer. The permutation in Match.children is unique at the Match's location and length (though these values are not stored here).
		/// </summary>
		class Match {
			public Grammar.Symbol symbol;
			public int position;
			public int length;
			public List<MatchGroup> children;

			public Match(Grammar.Symbol symbol, int position, int length, List<MatchGroup> children) {
				this.symbol = symbol;
				this.position = position;
				this.length = length;
				this.children = children;
			}
		}

		/// <summary>
		/// A grouping of Matches that are all the same Grammar.Symbol, document position, and length
		/// </summary>
		class MatchGroup {
			int documentPosition;

			public int DocumentPosition {
				get {
					return documentPosition;
				}
			}

			int parseLength;

			public int ParseLength {
				get {
					return parseLength;
				}
			}

			Grammar.Symbol symbol;

			public Grammar.Symbol Symbol {
				get {
					return symbol;
				}
			}

			List<Match> matches;

			public MatchGroup (int parsePosition, int parseLength, Grammar.Symbol symbol)
			{
				this.documentPosition = parsePosition;
				this.parseLength = parseLength;
				this.symbol = symbol;
				matches = new List<Match>();
			}

			public void AddMatch(Match match) {
				matches.Add (match);
			}
		}

		/// <summary>
		/// IDependent and IProvider are similar to the producer consumer design pattern, but an IDependent is Terminate()'d when it no longer has any IProviders.
		/// </summary>
		interface IDependent<T> {
			void Receive(T data);
			void Terminate();
			void Subcribe(IProvider<T> Provider);
		}

		/// <summary>
		/// See IDependent
		/// </summary>
		interface IProvider<T> {
			void SubscriptionCreatedHandler(IDependent<T> dependent);
		}

		/// <summary>
		/// Acts as a combined IProvider and IDependent in such a way that all IDependents that subscribe to this (on the IProvider side) will be Received every piece of data that come in (on the IDependent side).
		/// This essentially removes temporal ordering - it doesn't matter when an IDependent subcribes or when an IProvider generates data. All data will go everywhere.
		/// When the adapter is created it will be subscribed to IProviders. When all such Providers call Terminate (on the IDependent side) the adapter will call Terminate (on the IProvider side).
		/// </summary>
		class TemporallyUnorderedMultiProviderDependentAdapter<T> : IProvider<T>, IDependent<T> {
			List<IDependent<T>> dependents = new List<IDependent<T>>();
			List<T> alreadyProvidedValues = new List<T>();
			bool isTerminated = false;
			int providerCount = 0;

			#region IDependent implementation

			public void Receive(T data)
			{
				lock (dependents) {
					if (isTerminated)
						throw new Exception();
					for (int index = 0; index < dependents.Count; index++) {
						dependents[index].Receive(data);
					}
					alreadyProvidedValues.Add (data);
				}
			}

			public void Terminate()
			{
				lock (dependents) {
					if (isTerminated)
						throw new Exception();
					if (System.Threading.Interlocked.Decrement(ref providerCount) == 0) {
						isTerminated = true;
						for (int index = 0; index < dependents.Count; index++) {
							dependents[index].Terminate();
						}
					}
				}
			}

			public void Subcribe (IProvider<T> Provider) {
				System.Threading.Interlocked.Increment(ref providerCount);
				Provider.SubscriptionCreatedHandler(this);
			}

			#endregion

			#region IProvider implementation
			public void SubscriptionCreatedHandler(IDependent<T> dependent)
			{
				lock (dependents) {
					lock (alreadyProvidedValues) {
						foreach (T alreadyProvidedValue in alreadyProvidedValues) {
							dependent.Receive(alreadyProvidedValue);
						}
					}
					dependents.Add(dependent);
					if (isTerminated) {
						dependent.Terminate();
					}
				}
			}
			#endregion

		}

		/// <summary>
		/// Searches for MatchGroups using the specified Grammar.Recognizer, at the specified location
		/// Results are returned through the IProvider interface.
		/// It also places all Matches is receives into the Abstract Syntax Graph
		/// </summary>
		class ParseSubJob : IProvider<MatchGroup>, IDependent<Match> {
			ParseJob owner;

			public ParseJob Owner {
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

			int parsePosition;

			public int ParsePosition {
				get {
					return parsePosition;
				}
			}

			public ParseSubJob (ParseJob owner, Grammar.Recognizer recognizer, int parsePosition)
			{
				this.owner = owner;
				this.recognizer = recognizer;
				this.parsePosition = parsePosition;
			}

			TemporallyUnorderedMultiProviderDependentAdapter<MatchGroup> providerDependentAdapter = new TemporallyUnorderedMultiProviderDependentAdapter<MatchGroup>();

			#region IProvider implementation
			public void SubscriptionCreatedHandler(IDependent<MatchGroup> dependent)
			{
				providerDependentAdapter.SubscriptionCreatedHandler(dependent);
			}
			#endregion

			#region IDependent implementation

			ConcurrentSet<int> alreadyReturnedMatchGroupLengths = new ConcurrentSet<int>();

			public void Receive (Match data)
			{
				MatchGroup matchGroup = owner.matchGroups [data.symbol as Grammar.Recognizer] [data.position] [data.length];
				matchGroup.AddMatch (data);
				if (alreadyReturnedMatchGroupLengths.TryAdd(data.length)) {
					providerDependentAdapter.Receive (matchGroup);
				}
			}

			public void Terminate ()
			{
				providerDependentAdapter.Terminate ();
			}

			public void Subcribe (IProvider<Match> provider)
			{
				provider.SubscriptionCreatedHandler (this);
			}

			#endregion

			public void CreateAndQueueFirstRecognizerState() {
				RecognizerState state = new RecognizerState(this);
			}
		}

		/// <summary>
		/// Stores a reference to a SubParseJobInfo, and stores the current states of the recognizer (which is a NFA)
		/// It also stores information regarding previous transition symbols, so that when an accept state is terminated upon, a record of all the transitions can be compiled into the Abstract Syntax Graph
		/// It depends on the MatchGroups returned by ParseSubJobs, and (on completing recognition) returns new Matches through it's IProvider interface
		/// </summary>
		class RecognizerState : IProvider<Match>, IDependent<MatchGroup> {

			/// <summary>
			/// The ParseJob that owns this instance
			/// </summary>
			ParseSubJob parseSubJob;

			/// <summary>
			/// The state of the recognizer for subParseJobInfo.symbol that this ParseState represents
			/// </summary>
			Grammar.Recognizer.State[] recognizerNfaState;

			/// <summary>
			/// A history of the transition symbols that led to the current NFA state, which are used when each satisfaction of the PositionedMatchJobInfo occurs, as a means of creating the hierarchy of the resulting Abstract Syntax Graph
			/// </summary>
			List<MatchGroup> transitionHistory;

			/// <summary>
			/// The document position that the next transition symbol will be read from
			/// </summary>
			int currentDocumentPosition;

			public RecognizerState(ParseSubJob parseSubJob) {
				this.parseSubJob = parseSubJob;
				this.recognizerNfaState = this.parseSubJob.Recognizer.StartStates.ToArray();
				this.transitionHistory = new List<MatchGroup>();
				this.currentDocumentPosition = this.parseSubJob.ParsePosition;
				parseSubJob.Subcribe(this);
			}

			RecognizerState (ParseSubJob parseSubJob, Grammar.Recognizer.State[] recognizerNfaState, List<MatchGroup> transitionHistory, int currentDocumentPosition) {
				this.parseSubJob = parseSubJob;
				this.recognizerNfaState = recognizerNfaState.ToArray();
				this.transitionHistory = transitionHistory;
				this.currentDocumentPosition = currentDocumentPosition;
				parseSubJob.Subcribe(this);
			}

			RecognizerState Transition(MatchGroup matchGroup) {
				Grammar.Symbol transitionSymbol = matchGroup.Symbol;
				int length = matchGroup.ParseLength;
				HashSet<Grammar.Recognizer.State> nfaStatesAfterTransition = new HashSet<Nfa<Grammar.Symbol, int>.State>();
				foreach (Grammar.Recognizer.State fromState in recognizerNfaState) {
					foreach (Grammar.Recognizer.State toState in parseSubJob.Recognizer.TransitionFunction[fromState][transitionSymbol]) {
						nfaStatesAfterTransition.Add(toState);
					}
				}
				var updatedTransitionHistory = new List<MatchGroup> (transitionHistory);
				updatedTransitionHistory.Add (matchGroup);
				return new RecognizerState(parseSubJob, nfaStatesAfterTransition.ToArray(), updatedTransitionHistory, currentDocumentPosition + length);
			}
				
			#region IDependent implementation

			public void Receive (MatchGroup data)
			{
				RecognizerState next = Transition (data);
				next.SubscribeToSubJobs ();
			}

			public void Terminate ()
			{
				if (IsAcceptState) {
					Match match = new Match (parseSubJob.Recognizer, parseSubJob.ParsePosition, currentDocumentPosition - parseSubJob.ParsePosition, transitionHistory);
					for (int index = 0; index < dependents.Count; index++) {
						dependents [index].Receive (match);
						dependents [index].Terminate ();
					}
				}
			}

			public void Subcribe (IProvider<MatchGroup> Provider)
			{
				Provider.SubscriptionCreatedHandler (this);
			}

			#endregion

			#region IProvider implementation

			List<IDependent<Match>> dependents = new List<IDependent<Match>> ();
			public void SubscriptionCreatedHandler (IDependent<Match> dependent)
			{
				lock (dependents) {
					dependents.Add (dependent);
				}
			}

			#endregion
				
			public bool IsAcceptState { get { return recognizerNfaState.Any(x => parseSubJob.Recognizer.AcceptStates.Contains(x)); } }

			void SubscribeToSubJobs() {
				HashSet<Grammar.Symbol> soughtTransitionSymbols = new HashSet<Grammar.Symbol>();
				foreach (Grammar.Recognizer.State fromState in recognizerNfaState) {
					foreach (Grammar.Symbol transitionSymbol in parseSubJob.Recognizer.TransitionFunction[fromState].Keys) {
						soughtTransitionSymbols.Add(transitionSymbol);
					}
				}
				foreach (Grammar.Symbol soughtTransitionSymbol in soughtTransitionSymbols) {
					Subcribe(parseSubJob.Owner.GetSubParseResultsProvider(soughtTransitionSymbol, currentDocumentPosition));
				}
			}
		}

		class TerminalParseSubJob : IProvider<MatchGroup> {
			TemporallyUnorderedMultiProviderDependentAdapter<MatchGroup> providerDependentAdapter;

			public TerminalParseSubJob(ParseJob owner, Grammar.Terminal terminal, int documentPosition) {
				providerDependentAdapter = new TemporallyUnorderedMultiProviderDependentAdapter<MatchGroup>();
				bool result = true;
				foreach (UInt32 codePoint in terminal.UnicodeCodePoints) {
					if (codePoint != owner.TextAsUTF32[documentPosition++]) {
						result = false;
						break;
					}
				}
				if (result) {
					providerDependentAdapter.Receive(new MatchGroup(documentPosition, terminal.UnicodeCodePoints.Length, null));
				}
				providerDependentAdapter.Terminate();
			}

			#region IProvider implementation
			public void SubscriptionCreatedHandler (IDependent<MatchGroup> dependent)
			{
				providerDependentAdapter.SubscriptionCreatedHandler (dependent);
			}
			#endregion
		}

		class ParseJob {

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

			ParseJob(String text) {
				this.text = text;
				this.textAsUTF32 = text.GetUtf32CodePoints();
				matchGroups = new JaggedAutoDictionary<Grammar.Recognizer, int, int, MatchGroup>((Grammar.Recognizer recognizer, int parsePosition, int parseLength) => new MatchGroup(parsePosition, parseLength, recognizer));
				subParseJobs = new JaggedAutoDictionary<Grammar.Recognizer, int, ParseSubJob>((Grammar.Recognizer recognizer, int parsePosition) => new ParseSubJob(this, recognizer, parsePosition));
			}

			internal JaggedAutoDictionary<Grammar.Recognizer, int /* parsePosition */, int /* parseLength */, MatchGroup> matchGroups;
			JaggedAutoDictionary<Grammar.Recognizer, int /*parsePosition */, ParseSubJob> subParseJobs;
			ConcurrentSet<ParseSubJob> pendingAndCompletedStates = new ConcurrentSet<ParseSubJob>();

			/// <summary>
			/// Queue a sub-parse job for the parser to process, but only if an identical sub-parse job has not already been queued or completed
			/// </summary>
			/// <param name="parsePosition">Parse position.</param>
			/// <param name="symbol">Symbol.</param>
			public IProvider<MatchGroup> GetSubParseResultsProvider(Grammar.Symbol symbol, int documentPosition) {
				if (symbol is Grammar.Terminal) {
					return new TerminalParseSubJob (this, symbol as Grammar.Terminal, documentPosition);
				}
				return subParseJobs [symbol as Grammar.Recognizer] [documentPosition];
			}
		}
	}
}

