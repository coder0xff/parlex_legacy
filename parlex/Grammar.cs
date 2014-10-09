using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Automata;

namespace Parlex {
    public class Grammar {
        public interface ISymbol {
            String Name { get; }
        }

        public interface ITerminal : ISymbol {
            bool Matches(Int32[] documentUtf32CodePoints, int documentIndex);
            int Length { get; }
        }

        public class StringTerminal : ITerminal {
            readonly String _text;
            readonly Int32[] _unicodeCodePoints;

            public StringTerminal(String text) {
                if (text == null) throw new ArgumentNullException("text");
                _text = text;
                _unicodeCodePoints = text.GetUtf32CodePoints();
            }

            public bool Matches(Int32[] documentUtf32CodePoints, int documentIndex) {
                foreach (var codePoint in _unicodeCodePoints) {
                    if (documentIndex >= documentUtf32CodePoints.Length) return false;
                    if (documentUtf32CodePoints[documentIndex++] != codePoint) return false;
                }
                return true;
            }

            public int Length { get { return _unicodeCodePoints.Length; } }

            public String Name { get { return "String terminal: " + _text; } }

            public override string ToString() { return _text; }
        }

        public class LetterTerminalT : ITerminal {
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

            public override string ToString() { return "letter"; }
        }

        static public readonly LetterTerminalT LetterTerminal = new LetterTerminalT();

        public class CharacterTerminalT : ITerminal {
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

            public override string ToString() { return "character"; }
        }

        static public CharacterTerminalT CharacterTerminal = new CharacterTerminalT();

        public class WhiteSpaceTerminalT : ITerminal {
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

            public override string ToString() {
                return "whitespace";
            }
        }

        public static readonly WhiteSpaceTerminalT WhiteSpaceTerminal = new WhiteSpaceTerminalT();

        public class NonDoubleQuoteCharacterTerminalT : ITerminal
        {

            public bool Matches(int[] documentUtf32CodePoints, int documentIndex)
            {
                return documentUtf32CodePoints[documentIndex] != Char.ConvertToUtf32("\"", 0);
            }

            public int Length
            {
                get { return 1; }
            }

            public string Name
            {
                get { return "Non-double quote character"; }
            }
        }

        public static readonly NonDoubleQuoteCharacterTerminalT NonDoubleQuoteCharacterTerminal = new NonDoubleQuoteCharacterTerminalT();
        public static readonly StringTerminal DoubleQuoteTerminal = new StringTerminal("\"");

        private static readonly Dictionary<String, ISymbol> NameToBuiltInSymbol = new Dictionary<string, ISymbol>();

        static Grammar() {
            NameToBuiltInSymbol["letter"] = LetterTerminal;
            NameToBuiltInSymbol["character"] = CharacterTerminal;
            NameToBuiltInSymbol["whiteSpace"] = WhiteSpaceTerminal;
            NameToBuiltInSymbol["doubleQuote"] = DoubleQuoteTerminal;
            NameToBuiltInSymbol["nonDoubleQuote"] = NonDoubleQuoteCharacterTerminal;
        }

        public class Recognizer : Nfa<ISymbol>, ISymbol {
            readonly String _name;
            readonly bool _greedy;

            public Recognizer(String name, bool greedy) {
                _name = name;
                _greedy = greedy;
            }

            public Recognizer(String name, bool greedy, Nfa<ISymbol> source)
                : base(source) {
                _name = name;
                _greedy = greedy;
            }

            public String Name { get { return _name; } }

            public override string ToString() {
                return Name;
            }

            public bool Greedy { get { return _greedy; } }
        }

        public class CharacterSet : ITerminal {
            readonly HashSet<Int32> _unicodeCodePoints;
            readonly String _name;

            public CharacterSet(String name, IEnumerable<Int32> unicodeCodePoints) {
                _name = name;
                _unicodeCodePoints = new HashSet<Int32>(unicodeCodePoints);
            }

            public bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex >= documentUtf32CodePoints.Length) return false;
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

        public readonly List<Recognizer> Productions = new List<Recognizer>();
        public Recognizer MainProduction;

        public Recognizer GetRecognizerByName(String name) {
            return Productions.FirstOrDefault(x => x.Name == name);
        }

        public static bool TryGetBuiltinISymbolByName(String name, out ISymbol symbol) {
            return NameToBuiltInSymbol.TryGetValue(name, out symbol);
        }
    }
}

