using System;
using System.Collections.Generic;

namespace parlex
{
	public class Grammar
	{
		public interface Symbol {
		}

		public class Terminal : Symbol {
			public Int32[] UnicodeCodePoints;
		}

		public class Recognizer : Nfa<Symbol, int>, Symbol  {

		}

		public List<Recognizer> Productions;
		public Recognizer MainProduction;
	}
}

