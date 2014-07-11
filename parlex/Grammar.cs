using System;
using System.Collections.Generic;
using System.Linq;

namespace Parlex {
	public class Grammar {
		public interface Symbol {
			String Name { get; }
		}

		public interface Terminal : Symbol {
			bool Matches(Int32[] documentUtf32CodePoints, int documentIndex);
			int Length { get; }
		}

		public class StringTerminal : Terminal {
			String text;
			Int32[] UnicodeCodePoints;

			public StringTerminal(String text) {
				this.text = text;
				UnicodeCodePoints = text.GetUtf32CodePoints();
			}

			public bool Matches(Int32[] documentUtf32CodePoints, int documentIndex) {
				foreach (Int32 codePoint in UnicodeCodePoints) {
                    if (documentIndex >= documentUtf32CodePoints.Length) return false;
                    if (documentUtf32CodePoints[documentIndex++] != codePoint) return false;
				}
				return true;
			}

			public int Length { get { return UnicodeCodePoints.Length; } }

			public String Name { get { return "String terminal: " + text; } }

			public override string ToString() { return Name; }
		}

		public class LetterTerminal_t : Terminal {
            public bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex >= documentUtf32CodePoints.Length) return false;
				return Unicode.Letters.Contains(documentUtf32CodePoints[documentIndex]);
			}

			public int Length {
				get {
					return 1;
				}
			}

			public String Name { get { return "Any letter character"; } }

			public override string ToString() { return Name; }
		}

        static public LetterTerminal_t LetterTerminal = new LetterTerminal_t();

		public class CharacterTerminal_t : Terminal {
			public bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex >= documentUtf32CodePoints.Length) return false;
                return true;
			}

			public int Length {
				get {
					return 1;
				}
			}

			public String Name { get { return "Any character"; } }

			public override string ToString() { return Name; }
		}

        static public CharacterTerminal_t CharacterTerminal = new CharacterTerminal_t();

		public class WhiteSpaceTerminal_t : Terminal {
			public bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex >= documentUtf32CodePoints.Length) return false;
                return Unicode.WhiteSpace.Contains(documentUtf32CodePoints[documentIndex]);
			}

			public int Length {
				get {
					return 1;
				}
			}

			public String Name { get { return "Any whitespace character"; } }

			public override string ToString()
			{
				return Name;
			}
		}

        public static WhiteSpaceTerminal_t WhiteSpaceTerminal = new WhiteSpaceTerminal_t();

		public class Recognizer : Nfa<Symbol>, Symbol {
			String name;
            bool greedy;

			public Recognizer(String name, bool greedy) { 
                this.name = name;
                this.greedy = greedy;
            }

            public Recognizer(String name, bool greedy, Nfa<Symbol> source) : base(source)
            {
                this.name = name;
                this.greedy = greedy;
            }

			public String Name { get { return name; } }
            public override string ToString() { return Name; }

            public bool Greedy { get { return greedy; } }
        }

        public class CharacterSet : Terminal
        {
            HashSet<Int32> unicodeCodePoints;
            String name;

            public CharacterSet(String name, IEnumerable<Int32> unicodeCodePoints)
            {
                this.name = name;
                this.unicodeCodePoints = new HashSet<Int32>(unicodeCodePoints);
            }

            public bool Matches(int[] documentUtf32CodePoints, int documentIndex)
            {
                if (documentIndex >= documentUtf32CodePoints.Length) return false;
                return unicodeCodePoints.Contains(documentUtf32CodePoints[documentIndex]);
            }

            public int Length
            {
                get { return 1; }
            }

            public string Name
            {
                get { return "Character set: " + name; }
            }
        }

		public List<Recognizer> Productions = new List<Recognizer>();
		public Recognizer MainProduction;

        public Recognizer GetRecognizerByName(String name)
        {
            return Productions.FirstOrDefault(x => x.Name == name);
        }
	}
}

