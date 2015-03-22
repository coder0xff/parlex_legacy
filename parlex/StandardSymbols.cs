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
        public static readonly ITerminal LetterTerminal = new CharacterSetTerminal("any letter character", Unicode.Letters, "letter");
        public static readonly ITerminal NumberTerminal = new CharacterSetTerminal("any number character", Unicode.Numbers, "number");
        public static readonly ITerminal DecimalDigitTerminal = new CharacterSetTerminal("'0' through '9'", Unicode.DecimalDigits, "decimalDigit");
        public static readonly ITerminal HexidecimalDigitTerminal = new CharacterSetTerminal("any hexidecimal digit", Unicode.HexidecimalDigits, "hexDigit");
        public static readonly ITerminal AlphaNumericTerminal = new CharacterSetTerminal("alphanumeric", Unicode.Alphanumeric);
        public static readonly ITerminal CharacterTerminal = new CharacterTerminalT();
        public static readonly ITerminal WhiteSpaceTerminal = new CharacterSetTerminal("whitespace", Unicode.WhiteSpace);
        public static readonly ITerminal NonDoubleQuoteCharacterTerminal = new NonDoubleQuoteCharacterTerminalT();
        public static readonly ITerminal DoubleQuoteTerminal = new StringTerminal("\"");
        public static readonly ITerminal NonDoubleQuoteNonBackSlashCharacterTerminal = new NonDoubleQuoteNonBackSlashCharacterTerminalT();
        public static readonly ITerminal CarriageReturnTerminal = new CarriageReturnTerminalT();
        public static readonly ITerminal LineFeedTerminal = new LineFeedTerminalT();
        public static readonly ITerminal SimpleEscapeSequenceTerminal = new SimpleEscapeSequenceTerminalT();
        public static readonly ITerminal UnicodeEscapeSequenceTerminal = new UnicodeEscapeSequenceTerminalT();
        public static readonly NfaProduction NewLine = new NfaProduction("newLine", true, false);
        public static readonly NfaProduction WhiteSpaces = new NfaProduction("whiteSpaces", true, false);
        public static readonly NfaProduction StringLiteral = new NfaProduction("stringLiteral", false, true);
        public static readonly Dictionary<String, ISymbol> NameToBuiltInSymbol = new Dictionary<string, ISymbol>();

        public static bool TryGetBuiltinISymbolByName(String name, out ISymbol symbol) {
            return NameToBuiltInSymbol.TryGetValue(name, out symbol);
        }

        public static bool TryGetBuiltinFieldByName(String name, out FieldInfo field) {
            field = null;
            foreach (var fieldInfo in typeof(StandardSymbols).GetFields(BindingFlags.Public | BindingFlags.Static)) {
                if (typeof(ISymbol).IsAssignableFrom(fieldInfo.FieldType)) {
                    if (((ISymbol)fieldInfo.GetValue(null)).Name == name) {
                        field = fieldInfo;
                        return true;
                    }
                }
            }
            return false;
        }

        public static String TryGetBuiltInNameBySymbol(ISymbol symbol) {
            var entry = NameToBuiltInSymbol.FirstOrDefault(kvp => kvp.Value.Equals(symbol));
            if (entry.Equals(default(KeyValuePair<String, ISymbol>))) {
                return null;
            }
            return entry.Key;
        }

        public static FieldInfo TryGetBuiltInFieldBySymbol(ISymbol symbol) {
            FieldInfo field;
            return TryGetBuiltinFieldByName(symbol.Name, out field) ? field : null;
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

        public class SimpleEscapeSequenceTerminalT : ITerminal {
            public string Name { get { return "escape sequence"; } }
            public int Length { get { return 2; } }
            public bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
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

        public class UnicodeEscapeSequenceTerminalT : ITerminal {
            public string Name { get { return "Unicode escape sequence"; } }
            public int Length { get { return 7; } }
            public bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
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
                if (documentIndex >= documentUtf32CodePoints.Length) return false;
                return documentUtf32CodePoints[documentIndex] != Char.ConvertToUtf32("\"", 0) &&
                       documentUtf32CodePoints[documentIndex] != Char.ConvertToUtf32("\\", 0);
            }
        }

        public class CarriageReturnTerminalT : ITerminal {
            public string Name { get { return "Carriage return"; } }
            public int Length { get { return 1; } }
            public bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex >= documentUtf32CodePoints.Length) return false;
                return documentUtf32CodePoints[documentIndex] == '\r';
            }
        }

        public class LineFeedTerminalT : ITerminal {
            public string Name { get { return "Carriage return"; } }
            public int Length { get { return 1; } }
            public bool Matches(int[] documentUtf32CodePoints, int documentIndex) {
                if (documentIndex >= documentUtf32CodePoints.Length) return false;
                return documentUtf32CodePoints[documentIndex] == '\n';
            }
        }

        public static bool IsBuiltIn(ISymbol transition) {
            return NameToBuiltInSymbol.ContainsValue(transition);
        }

        static StandardSymbols() {
            var newLineState0 = new Nfa<ISymbol>.State();
            var newLineState1 = new Nfa<ISymbol>.State();
            var newLineState2 = new Nfa<ISymbol>.State();
            var newLineState3 = new Nfa<ISymbol>.State();
            var newLine = NewLine as NfaProduction;
            newLine.States.Add(newLineState0);
            newLine.States.Add(newLineState1);
            newLine.States.Add(newLineState2);
            newLine.States.Add(newLineState3);
            newLine.StartStates.Add(newLineState0);
            newLine.AcceptStates.Add(newLineState1);
            newLine.AcceptStates.Add(newLineState2);
            newLine.AcceptStates.Add(newLineState3);
            newLine.TransitionFunction[newLineState0][CarriageReturnTerminal].Add(newLineState1);
            newLine.TransitionFunction[newLineState1][LineFeedTerminal].Add(newLineState2);
            newLine.TransitionFunction[newLineState0][LineFeedTerminal].Add(newLineState3);

            var whiteSpacesState0 = new Nfa<ISymbol>.State();
            var whiteSpacesState1 = new Nfa<ISymbol>.State();
            var whiteSpaces = WhiteSpaces as NfaProduction;
            whiteSpaces.States.Add(whiteSpacesState0);
            whiteSpaces.States.Add(whiteSpacesState1);
            whiteSpaces.StartStates.Add(whiteSpacesState0);
            whiteSpaces.AcceptStates.Add(whiteSpacesState1);
            whiteSpaces.TransitionFunction[whiteSpacesState0][WhiteSpaceTerminal].Add(whiteSpacesState1);
            whiteSpaces.TransitionFunction[whiteSpacesState1][WhiteSpaceTerminal].Add(whiteSpacesState1);

            var stringLiteralState0 = new Nfa<ISymbol>.State();
            var stringLiteralState1 = new Nfa<ISymbol>.State();
            var stringLiteralState2 = new Nfa<ISymbol>.State();
            StringLiteral.States.Add(stringLiteralState0);
            StringLiteral.States.Add(stringLiteralState1);
            StringLiteral.States.Add(stringLiteralState2);
            StringLiteral.StartStates.Add(stringLiteralState0);
            StringLiteral.AcceptStates.Add(stringLiteralState2);
            StringLiteral.TransitionFunction[stringLiteralState0][DoubleQuoteTerminal].Add(stringLiteralState1);
            StringLiteral.TransitionFunction[stringLiteralState1][NonDoubleQuoteNonBackSlashCharacterTerminal].Add(stringLiteralState1);
            StringLiteral.TransitionFunction[stringLiteralState1][SimpleEscapeSequenceTerminal].Add(stringLiteralState1);
            StringLiteral.TransitionFunction[stringLiteralState1][UnicodeEscapeSequenceTerminal].Add(stringLiteralState1);
            StringLiteral.TransitionFunction[stringLiteralState1][DoubleQuoteTerminal].Add(stringLiteralState2);

            NameToBuiltInSymbol["letter"] = LetterTerminal;
            NameToBuiltInSymbol["number"] = NumberTerminal;
            NameToBuiltInSymbol["decimalDigit"] = DecimalDigitTerminal;
            NameToBuiltInSymbol["hexDigit"] = HexidecimalDigitTerminal;
            NameToBuiltInSymbol["alphanumeric"] = AlphaNumericTerminal;
            NameToBuiltInSymbol["character"] = CharacterTerminal;
            NameToBuiltInSymbol["whiteSpace"] = WhiteSpaceTerminal;
            NameToBuiltInSymbol["newLine"] = NewLine;
            NameToBuiltInSymbol["whiteSpaces"] = WhiteSpaces;
            NameToBuiltInSymbol["nonDoubleQuote"] = NonDoubleQuoteCharacterTerminal;
            NameToBuiltInSymbol["doubleQuote"] = DoubleQuoteTerminal;
            NameToBuiltInSymbol["nonDoubleQuoteNonBackSlash"] = NonDoubleQuoteNonBackSlashCharacterTerminal;
            NameToBuiltInSymbol["carriageReturn"] = CarriageReturnTerminal;
            NameToBuiltInSymbol["lineFeed"] = LineFeedTerminal;
            NameToBuiltInSymbol["simpleEscapeSequence"] = SimpleEscapeSequenceTerminal;
            NameToBuiltInSymbol["unicodeEscapeSequence"] = UnicodeEscapeSequenceTerminal;
            NameToBuiltInSymbol["stringLiteral"] = StringLiteral;
        }

    }
}
