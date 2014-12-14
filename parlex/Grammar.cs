﻿using System;
using System.Collections.Generic;
using System.Linq;
using Automata;

namespace Parlex {
    public class Grammar {
        public static readonly LetterTerminalT LetterTerminal = new LetterTerminalT();

        public static CharacterTerminalT CharacterTerminal = new CharacterTerminalT();

        public static readonly WhiteSpaceTerminalT WhiteSpaceTerminal = new WhiteSpaceTerminalT();

        public static readonly Recognizer WhiteSpacesEater = new Recognizer("whiteSpaces", true, false);

        public static readonly NonDoubleQuoteCharacterTerminalT NonDoubleQuoteCharacterTerminal = new NonDoubleQuoteCharacterTerminalT();

        public static readonly StringTerminal DoubleQuoteTerminal = new StringTerminal("\"");

        public static readonly NonDoubleQuoteNonBackSlashCharacterTerminalT NonDoubleQuoteNonBackSlashCharacterTerminal = new NonDoubleQuoteNonBackSlashCharacterTerminalT();

        public static readonly SimpleEscapeSequenceTerminalT SimpleEscapeSequenceTerminal = new SimpleEscapeSequenceTerminalT();

        public static readonly UnicodeEscapeSequnceTerminalT UnicodeEscapeSequnceTerminal = new UnicodeEscapeSequnceTerminalT();

        public static readonly Recognizer StringLiteral = new Recognizer("stringLiteral", false, true);

        private static readonly Dictionary<String, ISymbol> NameToBuiltInSymbol = new Dictionary<string, ISymbol>();

        public readonly List<Recognizer> Productions = new List<Recognizer>();
        public Recognizer MainSymbol;

        static Grammar() {
            var whiteSpacesState0 = new Recognizer.State();
            var whiteSpacesState1 = new Recognizer.State();
            WhiteSpacesEater.States.Add(whiteSpacesState0);
            WhiteSpacesEater.States.Add(whiteSpacesState1);
            WhiteSpacesEater.StartStates.Add(whiteSpacesState0);
            WhiteSpacesEater.AcceptStates.Add(whiteSpacesState1);
            WhiteSpacesEater.TransitionFunction[whiteSpacesState0][WhiteSpaceTerminal].Add(whiteSpacesState1);
            WhiteSpacesEater.TransitionFunction[whiteSpacesState1][WhiteSpaceTerminal].Add(whiteSpacesState1);

            var stringLiteralState0 = new Recognizer.State();
            var stringLiteralState1 = new Recognizer.State();
            var stringLiteralState2 = new Recognizer.State();
            StringLiteral.States.Add(stringLiteralState0);
            StringLiteral.States.Add(stringLiteralState1);
            StringLiteral.States.Add(stringLiteralState2);
            StringLiteral.StartStates.Add(stringLiteralState0);
            StringLiteral.AcceptStates.Add(stringLiteralState2);
            StringLiteral.TransitionFunction[stringLiteralState0][DoubleQuoteTerminal].Add(stringLiteralState1);
            StringLiteral.TransitionFunction[stringLiteralState1][NonDoubleQuoteNonBackSlashCharacterTerminal].Add(stringLiteralState1);
            StringLiteral.TransitionFunction[stringLiteralState1][SimpleEscapeSequenceTerminal].Add(stringLiteralState1);
            StringLiteral.TransitionFunction[stringLiteralState1][UnicodeEscapeSequnceTerminal].Add(stringLiteralState1);
            StringLiteral.TransitionFunction[stringLiteralState1][DoubleQuoteTerminal].Add(stringLiteralState2);

            NameToBuiltInSymbol["letter"] = LetterTerminal;
            NameToBuiltInSymbol["character"] = CharacterTerminal;
            NameToBuiltInSymbol["whiteSpaces"] = WhiteSpaceTerminal;
            NameToBuiltInSymbol["doubleQuote"] = DoubleQuoteTerminal;
            NameToBuiltInSymbol["nonDoubleQuote"] = NonDoubleQuoteCharacterTerminal;
            NameToBuiltInSymbol["nonDoubleQuoteNonBackSlash"] = NonDoubleQuoteNonBackSlashCharacterTerminal;
            NameToBuiltInSymbol["simpleEscapeSequence"] = SimpleEscapeSequenceTerminal;
            NameToBuiltInSymbol["unicodeEscapeSequence"] = UnicodeEscapeSequnceTerminal;
            NameToBuiltInSymbol["stringLiteral"] = StringLiteral;
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

        public class SimpleEscapeSequenceTerminalT : ITerminal {
            public string Name { get { return "Escape sequence"; } }
            public int Length { get { return 2; } }
            public bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex + 1 < documentUtf32CodePoints.Length) {
                    if (documentUtf32CodePoints[documentIndex] == Char.ConvertToUtf32("\\", 0)) {
                        if (!Unicode.DecimalDigitNumbers.Contains(documentUtf32CodePoints[documentIndex + 1])) {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public class UnicodeEscapeSequnceTerminalT : ITerminal {
            public string Name { get { return "Unicode escape sequence"; } }
            public int Length { get { return 9; } }
            public bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex + 8 < documentUtf32CodePoints.Length) {
                    if (documentUtf32CodePoints[documentIndex] == Char.ConvertToUtf32("\\", 0)) {
                        if (Enumerable.Range(1, 8).Select(i => documentUtf32CodePoints[documentIndex + i]).All(c => Unicode.DecimalDigitNumbers.Contains(c))) {
                            return true;
                        }
                    }
                }
                return false;
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

        public class NonDoubleQuoteNonBackSlashCharacterTerminalT : ITerminal {
            public string Name { get { return "Non-double quote character, non-back slash character"; } }
            public int Length { get { return 1; }}
            public bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                return documentUtf32CodePoints[documentIndex] != Char.ConvertToUtf32("\"", 0) &&
                       documentUtf32CodePoints[documentIndex] != Char.ConvertToUtf32("\\", 0);
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