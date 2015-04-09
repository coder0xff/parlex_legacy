using System;
using System.Collections.Generic;
using System.Collections.Generic.More;
using System.Globalization;
using System.Linq;
using System.Linq.More;
using System.Reflection;
using Automata;

namespace Parlex {
    public class StandardSymbols {
        public static readonly Terminal LetterTerminalDefinition = new CharacterSetTerminalDefinition("any letter character", Unicode.Letters, "letter");
        public static readonly Terminal NumberTerminalDefinition = new CharacterSetTerminalDefinition("any number character", Unicode.Numbers, "number");
        public static readonly Terminal DecimalDigitTerminalDefinition = new CharacterSetTerminalDefinition("'0' through '9'", Unicode.DecimalDigits, "decimalDigit");
        public static readonly Terminal HexidecimalDigitTerminalDefinition = new CharacterSetTerminalDefinition("any hexidecimal digit", Unicode.HexidecimalDigits, "hexDigit");
        public static readonly Terminal AlphaNumericTerminalDefinition = new CharacterSetTerminalDefinition("alphanumeric", Unicode.Alphanumeric);
        public static readonly Terminal CharacterTerminalDefinition = new CharacterTerminalDefinitionT();
        public static readonly Terminal WhiteSpaceTerminalDefinition = new CharacterSetTerminalDefinition("whitespace", Unicode.WhiteSpace);
        public static readonly Terminal NonDoubleQuoteCharacterTerminalDefinition = new NonDoubleQuoteCharacterTerminalDefinitionT();
        public static readonly Terminal DoubleQuoteTerminalDefinition = new StringTerminal("\"");
        public static readonly Terminal NonDoubleQuoteNonBackSlashCharacterTerminalDefinition = new NonDoubleQuoteNonBackSlashCharacterTerminalDefinitionT();
        public static readonly Terminal CarriageReturnTerminalDefinition = new CarriageReturnTerminalDefinitionT();
        public static readonly Terminal LineFeedTerminalDefinition = new LineFeedTerminalDefinitionT();
        public static readonly Terminal SimpleEscapeSequenceTerminalDefinition = new SimpleEscapeSequenceTerminalDefinitionT();
        public static readonly Terminal UnicodeEscapeSequenceTerminalDefinition = new UnicodeEscapeSequenceTerminalDefinitionT();
        public static readonly NfaProduction NewLine = new NfaProduction("newLine", true, false);
        public static readonly NfaProduction WhiteSpaces = new NfaProduction("whiteSpaces", true, false);
        public static readonly NfaProduction StringLiteral = new NfaProduction("stringLiteral", false, true);
        public static readonly Dictionary<String, Recognizer> NameToBuiltInSymbol = new Dictionary<string, Recognizer>();

        public static bool TryGetBuiltinISymbolByName(String name, out Recognizer recognizerDefinition) {
            return NameToBuiltInSymbol.TryGetValue(name, out recognizerDefinition);
        }

        public static bool TryGetBuiltinFieldByName(String name, out FieldInfo field) {
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
            FieldInfo field;
            return TryGetBuiltinFieldByName(recognizerDefinition.Name, out field) ? field : null;
        }

        public class CharacterTerminalDefinitionT : Terminal {
            public CharacterTerminalDefinitionT() : base("Any character") { }

            public override bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex >= documentUtf32CodePoints.Length) {
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
        public static readonly Bimap<Int32, Char> EscapeCharMap = new[]{
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

        public class SimpleEscapeSequenceTerminalDefinitionT : Terminal {
            public SimpleEscapeSequenceTerminalDefinitionT() : base("escape sequence") {}
            public override int Length { get { return 2; } }
            public override bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex + 1 < documentUtf32CodePoints.Length) {
                    if (documentUtf32CodePoints[documentIndex] == Char.ConvertToUtf32("\\", 0)) {
                        if (!EscapeCharMap.Left.Keys.Contains(documentUtf32CodePoints[documentIndex + 1])) {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public class UnicodeEscapeSequenceTerminalDefinitionT : Terminal {
            public UnicodeEscapeSequenceTerminalDefinitionT() : base("Unicode escape sequence") {}
            public override int Length { get { return 7; } }
            public override bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex + 6 < documentUtf32CodePoints.Length) {
                    if (documentUtf32CodePoints[documentIndex] == Char.ConvertToUtf32("\\", 0)) {
                        if (Enumerable.Range(1, 6).Select(i => documentUtf32CodePoints[documentIndex + i]).All(
                            c => Unicode.HexidecimalDigits.Contains(c))) {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public class NonDoubleQuoteCharacterTerminalDefinitionT : Terminal {
            public NonDoubleQuoteCharacterTerminalDefinitionT() : base("Non-double quote character") {}

            public override bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                return documentUtf32CodePoints[documentIndex] != Char.ConvertToUtf32("\"", 0);
            }

            public override int Length {
                get { return 1; }
            }
        }

        public class NonDoubleQuoteNonBackSlashCharacterTerminalDefinitionT : Terminal {
            public NonDoubleQuoteNonBackSlashCharacterTerminalDefinitionT() : base("Non-double quote character, non-back slash character") {}
            public override int Length { get { return 1; }}
            public override bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex >= documentUtf32CodePoints.Length) return false;
                return documentUtf32CodePoints[documentIndex] != Char.ConvertToUtf32("\"", 0) &&
                       documentUtf32CodePoints[documentIndex] != Char.ConvertToUtf32("\\", 0);
            }
        }

        public class CarriageReturnTerminalDefinitionT : Terminal {
            public CarriageReturnTerminalDefinitionT() : base("Carriage return") {}
            public override int Length { get { return 1; } }
            public override bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex >= documentUtf32CodePoints.Length) return false;
                return documentUtf32CodePoints[documentIndex] == '\r';
            }
        }

        public class LineFeedTerminalDefinitionT : Terminal {
            public LineFeedTerminalDefinitionT() : base("Line feed") { }
            public override int Length { get { return 1; } }
            public override bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex >= documentUtf32CodePoints.Length) return false;
                return documentUtf32CodePoints[documentIndex] == '\n';
            }
        }

        public static bool IsBuiltIn(Recognizer terminal) {
            return NameToBuiltInSymbol.ContainsValue(terminal);
        }

        static StandardSymbols() {
            var newLineState0 = new Nfa<Recognizer>.State();
            var newLineState1 = new Nfa<Recognizer>.State();
            var newLineState2 = new Nfa<Recognizer>.State();
            var newLineState3 = new Nfa<Recognizer>.State();
            var newLine = NewLine as NfaProduction;
            newLine.Nfa.States.Add(newLineState0);
            newLine.Nfa.States.Add(newLineState1);
            newLine.Nfa.States.Add(newLineState2);
            newLine.Nfa.States.Add(newLineState3);
            newLine.Nfa.StartStates.Add(newLineState0);
            newLine.Nfa.AcceptStates.Add(newLineState1);
            newLine.Nfa.AcceptStates.Add(newLineState2);
            newLine.Nfa.AcceptStates.Add(newLineState3);
            newLine.Nfa.TransitionFunction[newLineState0][CarriageReturnTerminalDefinition].Add(newLineState1);
            newLine.Nfa.TransitionFunction[newLineState1][LineFeedTerminalDefinition].Add(newLineState2);
            newLine.Nfa.TransitionFunction[newLineState0][LineFeedTerminalDefinition].Add(newLineState3);

            var whiteSpacesState0 = new Nfa<Recognizer>.State();
            var whiteSpacesState1 = new Nfa<Recognizer>.State();
            var whiteSpaces = WhiteSpaces as NfaProduction;
            whiteSpaces.Nfa.States.Add(whiteSpacesState0);
            whiteSpaces.Nfa.States.Add(whiteSpacesState1);
            whiteSpaces.Nfa.StartStates.Add(whiteSpacesState0);
            whiteSpaces.Nfa.AcceptStates.Add(whiteSpacesState1);
            whiteSpaces.Nfa.TransitionFunction[whiteSpacesState0][WhiteSpaceTerminalDefinition].Add(whiteSpacesState1);
            whiteSpaces.Nfa.TransitionFunction[whiteSpacesState1][WhiteSpaceTerminalDefinition].Add(whiteSpacesState1);

            var stringLiteralState0 = new Nfa<Recognizer>.State();
            var stringLiteralState1 = new Nfa<Recognizer>.State();
            var stringLiteralState2 = new Nfa<Recognizer>.State();
            StringLiteral.Nfa.States.Add(stringLiteralState0);
            StringLiteral.Nfa.States.Add(stringLiteralState1);
            StringLiteral.Nfa.States.Add(stringLiteralState2);
            StringLiteral.Nfa.StartStates.Add(stringLiteralState0);
            StringLiteral.Nfa.AcceptStates.Add(stringLiteralState2);
            StringLiteral.Nfa.TransitionFunction[stringLiteralState0][DoubleQuoteTerminalDefinition].Add(stringLiteralState1);
            StringLiteral.Nfa.TransitionFunction[stringLiteralState1][NonDoubleQuoteNonBackSlashCharacterTerminalDefinition].Add(stringLiteralState1);
            StringLiteral.Nfa.TransitionFunction[stringLiteralState1][SimpleEscapeSequenceTerminalDefinition].Add(stringLiteralState1);
            StringLiteral.Nfa.TransitionFunction[stringLiteralState1][UnicodeEscapeSequenceTerminalDefinition].Add(stringLiteralState1);
            StringLiteral.Nfa.TransitionFunction[stringLiteralState1][DoubleQuoteTerminalDefinition].Add(stringLiteralState2);

            NameToBuiltInSymbol["letter"] = LetterTerminalDefinition;
            NameToBuiltInSymbol["number"] = NumberTerminalDefinition;
            NameToBuiltInSymbol["decimalDigit"] = DecimalDigitTerminalDefinition;
            NameToBuiltInSymbol["hexDigit"] = HexidecimalDigitTerminalDefinition;
            NameToBuiltInSymbol["alphanumeric"] = AlphaNumericTerminalDefinition;
            NameToBuiltInSymbol["character"] = CharacterTerminalDefinition;
            NameToBuiltInSymbol["whiteSpace"] = WhiteSpaceTerminalDefinition;
            NameToBuiltInSymbol["newLine"] = NewLine;
            NameToBuiltInSymbol["whiteSpaces"] = WhiteSpaces;
            NameToBuiltInSymbol["nonDoubleQuote"] = NonDoubleQuoteCharacterTerminalDefinition;
            NameToBuiltInSymbol["doubleQuote"] = DoubleQuoteTerminalDefinition;
            NameToBuiltInSymbol["nonDoubleQuoteNonBackSlash"] = NonDoubleQuoteNonBackSlashCharacterTerminalDefinition;
            NameToBuiltInSymbol["carriageReturn"] = CarriageReturnTerminalDefinition;
            NameToBuiltInSymbol["lineFeed"] = LineFeedTerminalDefinition;
            NameToBuiltInSymbol["simpleEscapeSequence"] = SimpleEscapeSequenceTerminalDefinition;
            NameToBuiltInSymbol["unicodeEscapeSequence"] = UnicodeEscapeSequenceTerminalDefinition;
            NameToBuiltInSymbol["stringLiteral"] = StringLiteral;
        }

    }
}
