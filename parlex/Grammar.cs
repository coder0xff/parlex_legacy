using System;
using System.Collections.Generic;
using System.Collections.Generic.More;
using System.Linq;
using System.Linq.More;
using System.Reflection;
using System.Text;
using Automata;

namespace Parlex {
    public class Grammar {
        public static readonly CharacterSetTerminal LetterTerminal = new CharacterSetTerminal("any letter character", Unicode.Letters, "letter");

        public static readonly CharacterSetTerminal NumberTerminal = new CharacterSetTerminal("any number character", Unicode.Numbers, "number");

        public static readonly CharacterSetTerminal DecimalDigitTerminal = new CharacterSetTerminal("'0' through '9'", Unicode.DecimalDigits, "decimalDigit");

        public static readonly CharacterSetTerminal HexidecimalDigitTerminal = new CharacterSetTerminal("any hexidecimal digit", Unicode.HexidecimalDigits, "hexDigit");

        public static readonly CharacterSetTerminal AlphaNumericTerminal = new CharacterSetTerminal("alphanumeric", Unicode.Alphanumeric);

        public static CharacterTerminalT CharacterTerminal = new CharacterTerminalT();

        public static readonly CharacterSetTerminal WhiteSpaceTerminal = new CharacterSetTerminal("whitespace", Unicode.WhiteSpace);

        public static readonly NonDoubleQuoteCharacterTerminalT NonDoubleQuoteCharacterTerminal = new NonDoubleQuoteCharacterTerminalT();

        public static readonly StringTerminal DoubleQuoteTerminal = new StringTerminal("\"");

        public static readonly NonDoubleQuoteNonBackSlashCharacterTerminalT NonDoubleQuoteNonBackSlashCharacterTerminal = new NonDoubleQuoteNonBackSlashCharacterTerminalT();

        public static readonly CarriageReturnTerminalT CarriageReturnTerminal = new CarriageReturnTerminalT();

        public static readonly LineFeedTerminalT LineFeedTerminal = new LineFeedTerminalT();

        public static readonly SimpleEscapeSequenceTerminalT SimpleEscapeSequenceTerminal = new SimpleEscapeSequenceTerminalT();

        public static readonly UnicodeEscapeSequnceTerminalT UnicodeEscapeSequnceTerminal = new UnicodeEscapeSequnceTerminalT();

        public static readonly Production NewLine  = new Production("newLine", true, false);

        public static readonly Production WhiteSpaces = new Production("whiteSpaces", true, false);

        public static readonly Production StringLiteral = new Production("stringLiteral", false, true);

        private static readonly Dictionary<String, ISymbol> NameToBuiltInSymbol = new Dictionary<string, ISymbol>();

        public readonly List<Production> Productions = new List<Production>();
        public Production MainProduction;

        static Grammar() {
            var newLineState0 = new Production.State();
            var newLineState1 = new Production.State();
            var newLineState2 = new Production.State();
            var newLineState3 = new Production.State();
            NewLine.States.Add(newLineState0);
            NewLine.States.Add(newLineState1);
            NewLine.States.Add(newLineState2);
            NewLine.States.Add(newLineState3);
            NewLine.StartStates.Add(newLineState0);
            NewLine.AcceptStates.Add(newLineState1);
            NewLine.AcceptStates.Add(newLineState2);
            NewLine.AcceptStates.Add(newLineState3);
            NewLine.TransitionFunction[newLineState0][CarriageReturnTerminal].Add(newLineState1);
            NewLine.TransitionFunction[newLineState1][LineFeedTerminal].Add(newLineState2);
            NewLine.TransitionFunction[newLineState0][LineFeedTerminal].Add(newLineState3);

            var whiteSpacesState0 = new Production.State();
            var whiteSpacesState1 = new Production.State();
            WhiteSpaces.States.Add(whiteSpacesState0);
            WhiteSpaces.States.Add(whiteSpacesState1);
            WhiteSpaces.StartStates.Add(whiteSpacesState0);
            WhiteSpaces.AcceptStates.Add(whiteSpacesState1);
            WhiteSpaces.TransitionFunction[whiteSpacesState0][WhiteSpaceTerminal].Add(whiteSpacesState1);
            WhiteSpaces.TransitionFunction[whiteSpacesState1][WhiteSpaceTerminal].Add(whiteSpacesState1);

            var stringLiteralState0 = new Production.State();
            var stringLiteralState1 = new Production.State();
            var stringLiteralState2 = new Production.State();
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
            NameToBuiltInSymbol["number"] = NumberTerminal;
            NameToBuiltInSymbol["decimalDigit"] = DecimalDigitTerminal;
            NameToBuiltInSymbol["hexDigit"] = HexidecimalDigitTerminal;
            NameToBuiltInSymbol["alphanumeric"] = AlphaNumericTerminal;
            NameToBuiltInSymbol["character"] = CharacterTerminal;
            NameToBuiltInSymbol["whiteSpace"] = WhiteSpaceTerminal;
            NameToBuiltInSymbol["newLine"] = NewLine;
            NameToBuiltInSymbol["whiteSpacesEater"] = WhiteSpaces;
            NameToBuiltInSymbol["nonDoubleQuote"] = NonDoubleQuoteCharacterTerminal;
            NameToBuiltInSymbol["doubleQuote"] = DoubleQuoteTerminal;
            NameToBuiltInSymbol["nonDoubleQuoteNonBackSlash"] = NonDoubleQuoteNonBackSlashCharacterTerminal;
            NameToBuiltInSymbol["carriageReturn"] = CarriageReturnTerminal;
            NameToBuiltInSymbol["lineFeed"] = LineFeedTerminal;
            NameToBuiltInSymbol["simpleEscapeSequence"] = SimpleEscapeSequenceTerminal;
            NameToBuiltInSymbol["unicodeEscapeSequence"] = UnicodeEscapeSequnceTerminal;
            NameToBuiltInSymbol["stringLiteral"] = StringLiteral;
        }

        public Production GetRecognizerByName(String name) {
            return Productions.FirstOrDefault(x => x.Name == name);
        }

        public static bool TryGetBuiltinISymbolByName(String name, out ISymbol symbol) {
            return NameToBuiltInSymbol.TryGetValue(name, out symbol);
        }

        public static bool TryGetBuiltinFieldByName(String name, out FieldInfo field) {
            field = null;
            foreach (var fieldInfo in typeof(Grammar).GetFields(BindingFlags.Public | BindingFlags.Static)) {
                if (typeof(ISymbol).IsAssignableFrom(fieldInfo.FieldType)) {
                    if (((ISymbol)fieldInfo.GetValue(null)).Name == name) {
                        field = fieldInfo;
                        return true;
                    }
                }
            }
            return false;
        }

        public ISymbol GetSymbol(String name) {
            ISymbol result;
            if (!TryGetBuiltinISymbolByName(name, out result)) {
                result = GetRecognizerByName(name);
            }
            return result;
        }

        public static String TryGetBuiltInNameBySymbol(ISymbol symbol) {
            return NameToBuiltInSymbol.FirstOrDefault(kvp => kvp.Value == symbol).Key;
        }

        public class CharacterSetTerminal : ITerminal {
            private readonly String _name;
            private readonly HashSet<Int32> _unicodeCodePoints;
            private readonly String _shortName;

            public CharacterSetTerminal(String name, IEnumerable<Int32> unicodeCodePoints, String shortName = null) {
                _name = name;
                _shortName = shortName ?? _name;
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
                get { return _shortName; }
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

        /// <summary>
        /// A bimap from unicode code point escape sequence characters to their target literal
        /// </summary>
        public static readonly Bimap<Int32, Char> EscapeCharMap = new Tuple<char, char>[]{
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
        }.ToBimap(e => Char.ConvertToUtf32(e.Item1.ToString(), 0), e => e.Item2);

     

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

        public class UnicodeEscapeSequnceTerminalT : ITerminal {
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
        
        public class Production : Nfa<ISymbol>, ISymbol {
            private readonly bool _eatTrailingWhitespace;
            private readonly bool _greedy;
            private String _name;

            public Production(String name, bool greedy, bool eatTrailingWhitespace) {
                _name = name;
                _greedy = greedy;
                _eatTrailingWhitespace = eatTrailingWhitespace;
            }

            public Production(String name, bool greedy, Nfa<ISymbol> source)
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
                set { _name = value; }
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

        public static String ProcessStringLiteral(Int32[] codePoints, int start, int length) {
            if (start + length > codePoints.Length) throw new IndexOutOfRangeException();
            var builder = new StringBuilder();
            int doubleQuote = Char.ConvertToUtf32("\"", 0);
            int backSlash = Char.ConvertToUtf32("\\", 0);
            int i;
            //first, eat leading whitespace
            for (i = start; i < start + length && Unicode.WhiteSpace.Contains(codePoints[i]); i++) ;
            if (i == start + length) return null;
            if (codePoints[i] != doubleQuote) return null;
            i++;
            for (; i < start + length && codePoints[i] != doubleQuote; ) {
                if (codePoints[i] == backSlash) {
                    i++;
                    if (i < start + length) {
                        var c = codePoints[i];
                        if (Unicode.HexidecimalDigits.Contains(c)) {
                            if (i + 5 < start + length) {
                                var hexCharacters = new Int32[6];
                                for (int j = 0; j < 6; j++) {
                                    if (!Unicode.HexidecimalDigits.Contains(c)) return null;
                                    hexCharacters[j] = codePoints[i];
                                    i++;
                                }
                                var hexString = hexCharacters.Utf32ToString();
                                var parsedInt = Convert.ToInt32(hexString, 16);
                                var target = Char.ConvertFromUtf32(parsedInt);
                                builder.Append(target);
                            }
                        } else if (EscapeCharMap.Left.Keys.Contains(c)) {
                            var target = EscapeCharMap.Left[c];
                            builder.Append(target);
                        } else {
                            builder.Append('\\');
                            builder.Append(Char.ConvertFromUtf32(c));
                        }
                    } else {
                        return null;
                    }
                } else {
                    builder.Append(Char.ConvertFromUtf32(codePoints[i]));
                    i++;
                }
            }
            if (i >= start + length) return null;
            if (codePoints[i] != doubleQuote) return null;
            i++;
            for (; i < start + length && Unicode.WhiteSpace.Contains(codePoints[i]); i++) ;
            if (i != start + length) return null;
            return builder.ToString();
        }

        public static String QuoteStringLiteral(String text) {
            text = text.Replace("\\", "\\\\");
            text = text.Replace("\a", "\\a");
            text = text.Replace("\b", "\\a");
            text = text.Replace("\f", "\\a");
            text = text.Replace("\n", "\\a");
            text = text.Replace("\r", "\\a");
            text = text.Replace("\t", "\\a");
            text = text.Replace("'", "\'");
            text = text.Replace("\"", "\\\"");
            text = text.Replace("?", "\\?");
            return "\"" + text + "\"";
        }
    }
}