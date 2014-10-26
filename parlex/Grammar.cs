using System;
using System.Collections.Generic;
using System.Linq;
using Automata;

namespace Parlex {
    public class Grammar {
        public static readonly LetterTerminalT LetterTerminal = new LetterTerminalT();

        public static CharacterTerminalT CharacterTerminal = new CharacterTerminalT();

        public static readonly WhiteSpaceTerminalT WhiteSpaceTerminal = new WhiteSpaceTerminalT();

        public static readonly Recognizer WhiteSpacesEater = new Recognizer("whitespaces", true, false);

        public static readonly NonDoubleQuoteCharacterTerminalT NonDoubleQuoteCharacterTerminal = new NonDoubleQuoteCharacterTerminalT();
        public static readonly StringTerminal DoubleQuoteTerminal = new StringTerminal("\"");

        private static readonly Dictionary<String, ISymbol> NameToBuiltInSymbol = new Dictionary<string, ISymbol>();

        public readonly List<Recognizer> Productions = new List<Recognizer>();
        public Recognizer MainProduction;

        static Grammar() {
            var whiteSpacesState0 = new Nfa<ISymbol>.State();
            WhiteSpacesEater.States.Add(whiteSpacesState0);
            WhiteSpacesEater.StartStates.Add(whiteSpacesState0);
            WhiteSpacesEater.AcceptStates.Add(whiteSpacesState0);
            WhiteSpacesEater.TransitionFunction[whiteSpacesState0][WhiteSpaceTerminal].Add(whiteSpacesState0);

            NameToBuiltInSymbol["letter"] = LetterTerminal;
            NameToBuiltInSymbol["character"] = CharacterTerminal;
            NameToBuiltInSymbol["whiteSpace"] = WhiteSpaceTerminal;
            NameToBuiltInSymbol["doubleQuote"] = DoubleQuoteTerminal;
            NameToBuiltInSymbol["nonDoubleQuote"] = NonDoubleQuoteCharacterTerminal;
        }

        public Recognizer GetRecognizerByName(String name) {
            return Productions.FirstOrDefault(x => x.Name == name);
        }

        public static bool TryGetBuiltinISymbolByName(String name, out ISymbol symbol) {
            return NameToBuiltInSymbol.TryGetValue(name, out symbol);
        }

        public class CharacterSet : ITerminal {
            private readonly String _name;
            private readonly HashSet<Int32> _unicodeCodePoints;

            public CharacterSet(String name, IEnumerable<Int32> unicodeCodePoints) {
                _name = name;
                _unicodeCodePoints = new HashSet<Int32>(unicodeCodePoints);
            }

            public bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex >= documentUtf32CodePoints.Length) {
                    return false;
                }
                return _unicodeCodePoints.Contains(documentUtf32CodePoints[documentIndex]);
            }

            public int Length {
                get { return 1; }
            }

            public string Name {
                get { return "Character set: " + _name; }
            }

            public override string ToString() {
                return _name;
            }
        }

        public class CharacterTerminalT : ITerminal {
            public bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex >= documentUtf32CodePoints.Length) {
                    return false;
                }
                return true;
            }

            public int Length {
                get { return 1; }
            }

            public String Name {
                get { return "Any character"; }
            }

            public override string ToString() {
                return "character";
            }
        }

        public interface ISymbol {
            String Name { get; }
        }

        public interface ITerminal : ISymbol {
            int Length { get; }
            bool Matches(Int32[] documentUtf32CodePoints, int documentIndex);
        }

        public class LetterTerminalT : ITerminal {
            public bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex >= documentUtf32CodePoints.Length) {
                    return false;
                }
                return Unicode.Letters.Contains(documentUtf32CodePoints[documentIndex]);
            }

            public int Length {
                get { return 1; }
            }

            public String Name {
                get { return "Any letter character"; }
            }

            public override string ToString() {
                return "letter";
            }
        }

        public class NonDoubleQuoteCharacterTerminalT : ITerminal {
            public bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                return documentUtf32CodePoints[documentIndex] != Char.ConvertToUtf32("\"", 0);
            }

            public int Length {
                get { return 1; }
            }

            public string Name {
                get { return "Non-double quote character"; }
            }

            public override string ToString() {
                return "nonDoubleQuote";
            }
        }

        public class Recognizer : Nfa<ISymbol>, ISymbol {
            private readonly bool _eatTrailingWhitespace;
            private readonly bool _greedy;
            private readonly String _name;

            public Recognizer(String name, bool greedy, bool eatTrailingWhitespace) {
                _name = name;
                _greedy = greedy;
                _eatTrailingWhitespace = eatTrailingWhitespace;
            }

            public Recognizer(String name, bool greedy, Nfa<ISymbol> source)
                : base(source) {
                _name = name;
                _greedy = greedy;
            }

            public bool Greedy {
                get { return _greedy; }
            }

            public bool EatWhiteSpace {
                get { return _eatTrailingWhitespace; }
            }

            public String Name {
                get { return _name; }
            }

            public override string ToString() {
                return Name;
            }
        }

        public class StringTerminal : ITerminal {
            private readonly String _text;
            private readonly Int32[] _unicodeCodePoints;

            public StringTerminal(String text) {
                if (text == null) {
                    throw new ArgumentNullException("text");
                }
                _text = text;
                _unicodeCodePoints = text.GetUtf32CodePoints();
            }

            public bool Matches(Int32[] documentUtf32CodePoints, int documentIndex) {
                foreach (int codePoint in _unicodeCodePoints) {
                    if (documentIndex >= documentUtf32CodePoints.Length) {
                        return false;
                    }
                    if (documentUtf32CodePoints[documentIndex++] != codePoint) {
                        return false;
                    }
                }
                return true;
            }

            public int Length {
                get { return _unicodeCodePoints.Length; }
            }

            public String Name {
                get { return "String terminal: " + _text; }
            }

            public override string ToString() {
                return _text;
            }
        }

        public class WhiteSpaceTerminalT : ITerminal {
            public bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex >= documentUtf32CodePoints.Length) {
                    return false;
                }
                return Unicode.WhiteSpace.Contains(documentUtf32CodePoints[documentIndex]);
            }

            public int Length {
                get { return 1; }
            }

            public String Name {
                get { return "Any whitespace character"; }
            }

            public override string ToString() {
                return "whiteSpace";
            }
        }
    }
}