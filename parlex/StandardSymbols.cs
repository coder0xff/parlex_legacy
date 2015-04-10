using System;
using System.Collections.Generic;
using System.Collections.Generic.More;
using System.Globalization;
using System.Linq;
using System.Linq.More;
using System.Reflection;
using Automata;

namespace Parlex {
    public static class StandardSymbols {
        public static readonly Terminal Letter = new CharacterSetTerminal("a letter", Unicode.Letters, "letter");
        public static readonly Terminal Number = new CharacterSetTerminal("a number", Unicode.Numbers, "number");
        public static readonly Terminal DecimalDigit = new CharacterSetTerminal("a decimal digit", Unicode.DecimalDigits, "decimalDigit");
        public static readonly Terminal HexDigit = new CharacterSetTerminal("a hex digit", Unicode.HexadecimalDigits, "hexDigit");
        public static readonly Terminal Alphanumeric = new CharacterSetTerminal("alphanumeric", Unicode.Alphanumeric);
        public static readonly Terminal Any = new AnyCharacterTerminal();
        public static readonly Terminal WhiteSpace = new CharacterSetTerminal("whiteSpace", Unicode.WhiteSpace);
        public static readonly Terminal NonDoubleQuote = new NonDoubleQuoteCharacterTerminal();
        public static readonly Terminal DoubleQuote = new StringTerminal("\"");
        public static readonly Terminal NonDoubleQuoteNonBackslash = new NonDoubleQuoteNonBackslashCharacterTerminal();
        public static readonly Terminal CarriageReturn = new CarriageReturnTerminal();
        public static readonly Terminal Linefeed = new LinefeedTerminal();
        public static readonly Terminal SimpleEscapeSequence = new SimpleEscapeSequenceTerminal();
        public static readonly Terminal UnicodeEscapeSequence = new UnicodeEscapeSequenceTerminal();
        public static readonly NfaProduction Newline = new NfaProduction("newline", true);
        public static readonly NfaProduction WhiteSpaces = new NfaProduction("whiteSpaces", true);
        public static readonly NfaProduction StringLiteral = new NfaProduction("stringLiteral", false);
        public static readonly Dictionary<String, Recognizer> NameToBuiltInSymbol = new Dictionary<string, Recognizer>();

        public static bool IsBuiltIn(Recognizer terminal) {
            return NameToBuiltInSymbol.ContainsValue(terminal);
        }

        public static bool TryGetBuiltInISymbolByName(String name, out Recognizer recognizerDefinition) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }
            return NameToBuiltInSymbol.TryGetValue(name, out recognizerDefinition);
        }

        public static bool TryGetBuiltInFieldByName(String name, out FieldInfo field) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }
            field = null;
            foreach (var fieldInfo in typeof(StandardSymbols).GetFields(BindingFlags.Public | BindingFlags.Static)) {
                if (typeof(Recognizer).IsAssignableFrom(fieldInfo.FieldType)) {
                    if (((Recognizer)fieldInfo.GetValue(null)).Name == name) {
                        field = fieldInfo;
                        return true;
                    }
                }
            }
            return false;
        }

        public static String TryGetBuiltInNameBySymbol(Recognizer recognizerDefinition) {
            var entry = NameToBuiltInSymbol.FirstOrDefault(kvp => kvp.Value.Equals(recognizerDefinition));
            if (entry.Equals(default(KeyValuePair<String, Recognizer>))) {
                return null;
            }
            return entry.Key;
        }

        public static FieldInfo TryGetBuiltInFieldBySymbol(Recognizer recognizerDefinition) {
            if (recognizerDefinition == null) {
                throw new ArgumentNullException("recognizerDefinition");
            }
            FieldInfo field;
            return TryGetBuiltInFieldByName(recognizerDefinition.Name, out field) ? field : null;
        }

        private class AnyCharacterTerminal : Terminal {
            public AnyCharacterTerminal() : base("Any character") { }

            public override bool Matches(IReadOnlyList<Int32> documentUtf32CodePoints, int documentIndex) {
                if (documentUtf32CodePoints == null) {
                    throw new ArgumentNullException("documentUtf32CodePoints");
                }
                if (documentIndex < 0) {
                    throw new ArgumentOutOfRangeException("documentIndex", "documentIndex must be non-negative");
                }
                if (documentIndex >= documentUtf32CodePoints.Count) {
                    return false;
                }
                return true;
            }

            public override int Length {
                get { return 1; }
            }
        }

        /// <summary>
        /// A bimap from unicode code point escape sequence characters to their target literal
        /// </summary>
        internal static readonly Bimap<Int32, Char> EscapeCharMap = new[]{
            Tuple.Create('a', '\a'),
            Tuple.Create('b', '\b'),
            Tuple.Create('f', '\f'),
            Tuple.Create('n', '\n'),
            Tuple.Create('r', '\r'),
            Tuple.Create('t', '\t'),
            Tuple.Create('\\', '\\'),
            Tuple.Create('\'', '\''),
            Tuple.Create('"', '"'),
            Tuple.Create('?', '?')
        }.ToBimap(e => Char.ConvertToUtf32(e.Item1.ToString(CultureInfo.InvariantCulture), 0), e => e.Item2);

        private class SimpleEscapeSequenceTerminal : Terminal {
            public SimpleEscapeSequenceTerminal() : base("escape sequence") {}
            public override int Length { get { return 2; } }
            public override bool Matches(IReadOnlyList<Int32> documentUtf32CodePoints, int documentIndex) {
                if (documentUtf32CodePoints == null) {
                    throw new ArgumentNullException("documentUtf32CodePoints");
                }
                if (documentIndex < 0) {
                    throw new ArgumentOutOfRangeException("documentIndex", "documentIndex must be non-negative");
                }
                if (documentIndex + 1 < documentUtf32CodePoints.Count) {
                    if (documentUtf32CodePoints[documentIndex] == Char.ConvertToUtf32("\\", 0)) {
                        if (!EscapeCharMap.Left.Keys.Contains(documentUtf32CodePoints[documentIndex + 1])) {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        private class UnicodeEscapeSequenceTerminal : Terminal {
            public UnicodeEscapeSequenceTerminal() : base("Unicode escape sequence") {}
            public override int Length { get { return 7; } }
            public override bool Matches(IReadOnlyList<Int32> documentUtf32CodePoints, int documentIndex) {
                if (documentUtf32CodePoints == null) {
                    throw new ArgumentNullException("documentUtf32CodePoints");
                }
                if (documentIndex < 0) {
                    throw new ArgumentOutOfRangeException("documentIndex", "documentIndex must be non-negative");
                }
                if (documentIndex + 6 < documentUtf32CodePoints.Count) {
                    if (documentUtf32CodePoints[documentIndex] == Char.ConvertToUtf32("\\", 0)) {
                        if (Enumerable.Range(1, 6).Select(i => documentUtf32CodePoints[documentIndex + i]).All(
                            c => Unicode.HexadecimalDigits.Contains(c))) {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        private class NonDoubleQuoteCharacterTerminal : Terminal {
            public NonDoubleQuoteCharacterTerminal() : base("Non-double quote character") {}

            public override bool Matches(IReadOnlyList<Int32> documentUtf32CodePoints, int documentIndex) {
                if (documentUtf32CodePoints == null) {
                    throw new ArgumentNullException("documentUtf32CodePoints");
                }
                if (documentIndex < 0) {
                    throw new ArgumentOutOfRangeException("documentIndex", "documentIndex must be non-negative");
                }
                if (documentIndex >= documentUtf32CodePoints.Count) return false;
                return documentUtf32CodePoints[documentIndex] != Char.ConvertToUtf32("\"", 0);
            }

            public override int Length {
                get { return 1; }
            }
        }

        private class NonDoubleQuoteNonBackslashCharacterTerminal : Terminal {
            public NonDoubleQuoteNonBackslashCharacterTerminal() : base("Non-double quote character, non-back slash character") {}
            public override int Length { get { return 1; }}
            public override bool Matches(IReadOnlyList<Int32> documentUtf32CodePoints, int documentIndex) {
                if (documentUtf32CodePoints == null) {
                    throw new ArgumentNullException("documentUtf32CodePoints");
                }
                if (documentIndex < 0) {
                    throw new ArgumentOutOfRangeException("documentIndex", "documentIndex must be non-negative");
                }
                if (documentIndex >= documentUtf32CodePoints.Count) return false;
                return documentUtf32CodePoints[documentIndex] != Char.ConvertToUtf32("\"", 0) &&
                       documentUtf32CodePoints[documentIndex] != Char.ConvertToUtf32("\\", 0);
            }
        }

        private class CarriageReturnTerminal : Terminal {
            public CarriageReturnTerminal() : base("Carriage return") {}
            public override int Length { get { return 1; } }
            public override bool Matches(IReadOnlyList<Int32> documentUtf32CodePoints, int documentIndex) {
                if (documentUtf32CodePoints == null) {
                    throw new ArgumentNullException("documentUtf32CodePoints");
                }
                if (documentIndex < 0) {
                    throw new ArgumentOutOfRangeException("documentIndex", "documentIndex must be non-negative");
                }
                if (documentIndex >= documentUtf32CodePoints.Count) return false;
                return documentUtf32CodePoints[documentIndex] == '\r';
            }
        }

        private class LinefeedTerminal : Terminal {
            public LinefeedTerminal() : base("Line feed") { }
            public override int Length { get { return 1; } }
            public override bool Matches(IReadOnlyList<Int32> documentUtf32CodePoints, int documentIndex) {
                if (documentUtf32CodePoints == null) {
                    throw new ArgumentNullException("documentUtf32CodePoints");
                }
                if (documentIndex < 0) {
                    throw new ArgumentOutOfRangeException("documentIndex", "documentIndex must be non-negative");
                }
                if (documentIndex >= documentUtf32CodePoints.Count) return false;
                return documentUtf32CodePoints[documentIndex] == '\n';
            }
        }

        static StandardSymbols() {
            var newLineState0 = new Nfa<Recognizer>.State();
            var newLineState1 = new Nfa<Recognizer>.State();
            var newLineState2 = new Nfa<Recognizer>.State();
            var newLineState3 = new Nfa<Recognizer>.State();
            Newline.Nfa.States.Add(newLineState0);
            Newline.Nfa.States.Add(newLineState1);
            Newline.Nfa.States.Add(newLineState2);
            Newline.Nfa.States.Add(newLineState3);
            Newline.Nfa.StartStates.Add(newLineState0);
            Newline.Nfa.AcceptStates.Add(newLineState1);
            Newline.Nfa.AcceptStates.Add(newLineState2);
            Newline.Nfa.AcceptStates.Add(newLineState3);
            Newline.Nfa.TransitionFunction[newLineState0][CarriageReturn].Add(newLineState1);
            Newline.Nfa.TransitionFunction[newLineState1][Linefeed].Add(newLineState2);
            Newline.Nfa.TransitionFunction[newLineState0][Linefeed].Add(newLineState3);

            var whiteSpacesState0 = new Nfa<Recognizer>.State();
            var whiteSpacesState1 = new Nfa<Recognizer>.State();
            WhiteSpaces.Nfa.States.Add(whiteSpacesState0);
            WhiteSpaces.Nfa.States.Add(whiteSpacesState1);
            WhiteSpaces.Nfa.StartStates.Add(whiteSpacesState0);
            WhiteSpaces.Nfa.AcceptStates.Add(whiteSpacesState1);
            WhiteSpaces.Nfa.TransitionFunction[whiteSpacesState0][WhiteSpace].Add(whiteSpacesState1);
            WhiteSpaces.Nfa.TransitionFunction[whiteSpacesState1][WhiteSpace].Add(whiteSpacesState1);

            var stringLiteralState0 = new Nfa<Recognizer>.State();
            var stringLiteralState1 = new Nfa<Recognizer>.State();
            var stringLiteralState2 = new Nfa<Recognizer>.State();
            StringLiteral.Nfa.States.Add(stringLiteralState0);
            StringLiteral.Nfa.States.Add(stringLiteralState1);
            StringLiteral.Nfa.States.Add(stringLiteralState2);
            StringLiteral.Nfa.StartStates.Add(stringLiteralState0);
            StringLiteral.Nfa.AcceptStates.Add(stringLiteralState2);
            StringLiteral.Nfa.TransitionFunction[stringLiteralState0][DoubleQuote].Add(stringLiteralState1);
            StringLiteral.Nfa.TransitionFunction[stringLiteralState1][NonDoubleQuoteNonBackslash].Add(stringLiteralState1);
            StringLiteral.Nfa.TransitionFunction[stringLiteralState1][SimpleEscapeSequence].Add(stringLiteralState1);
            StringLiteral.Nfa.TransitionFunction[stringLiteralState1][UnicodeEscapeSequence].Add(stringLiteralState1);
            StringLiteral.Nfa.TransitionFunction[stringLiteralState1][DoubleQuote].Add(stringLiteralState2);

            NameToBuiltInSymbol["letter"] = Letter;
            NameToBuiltInSymbol["number"] = Number;
            NameToBuiltInSymbol["decimalDigit"] = DecimalDigit;
            NameToBuiltInSymbol["hexDigit"] = HexDigit;
            NameToBuiltInSymbol["alphanumeric"] = Alphanumeric;
            NameToBuiltInSymbol["any"] = Any;
            NameToBuiltInSymbol["whiteSpace"] = WhiteSpace;
            NameToBuiltInSymbol["nonDoubleQuote"] = NonDoubleQuote;
            NameToBuiltInSymbol["doubleQuote"] = DoubleQuote;
            NameToBuiltInSymbol["nonDoubleQuoteNonBackslash"] = NonDoubleQuoteNonBackslash;
            NameToBuiltInSymbol["carriageReturn"] = CarriageReturn;
            NameToBuiltInSymbol["linefeed"] = Linefeed;
            NameToBuiltInSymbol["simpleEscapeSequence"] = SimpleEscapeSequence;
            NameToBuiltInSymbol["unicodeEscapeSequence"] = UnicodeEscapeSequence;

            NameToBuiltInSymbol["newline"] = Newline;
            NameToBuiltInSymbol["whiteSpaces"] = WhiteSpaces;
            NameToBuiltInSymbol["stringLiteral"] = StringLiteral;
        }

    }
}
