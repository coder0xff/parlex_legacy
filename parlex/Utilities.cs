using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parlex {
    public static class Utilities {
        public static String ProcessStringLiteral(IReadOnlyList<Int32> codePoints, int start, int length) {
            if (start + length > codePoints.Count) throw new IndexOutOfRangeException();
            var builder = new StringBuilder();
            int doubleQuote = Char.ConvertToUtf32("\"", 0);
            int backSlash = Char.ConvertToUtf32("\\", 0);
            int i;
            //first, eat leading whitespace
            for (i = start; i < start + length && Unicode.WhiteSpace.Contains(codePoints[i]); i++) {}
            if (i == start + length) return null;
            if (codePoints[i] != doubleQuote) return null;
            i++;
            for (; i < start + length && codePoints[i] != doubleQuote; ) {
                if (codePoints[i] == backSlash) {
                    i++;
                    if (i < start + length) {
                        var c = codePoints[i];
                        if (Unicode.HexadecimalDigits.Contains(c)) {
                            if (i + 5 < start + length) {
                                var hexCharacters = new Int32[6];
                                for (int j = 0; j < 6; j++) {
                                    if (!Unicode.HexadecimalDigits.Contains(c)) return null;
                                    hexCharacters[j] = codePoints[i];
                                    i++;
                                }
                                var hexString = hexCharacters.Utf32ToString();
                                var parsedInt = Convert.ToInt32(hexString, 16);
                                var target = Char.ConvertFromUtf32(parsedInt);
                                builder.Append(target);
                            }
                        } else if (StandardSymbols.EscapeCharMap.Left.Keys.Contains(c)) {
                            var target = StandardSymbols.EscapeCharMap.Left[c];
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
            for (; i < start + length && Unicode.WhiteSpace.Contains(codePoints[i]); i++) {}
            if (i != start + length) return null;
            return builder.ToString();
        }

        public static String QuoteStringLiteral(String text) {
            text = text.Replace("\\", "\\\\");
            text = text.Replace("\a", "\\a");
            text = text.Replace("\b", "\\b");
            text = text.Replace("\f", "\\f");
            text = text.Replace("\n", "\\n");
            text = text.Replace("\r", "\\r");
            text = text.Replace("\t", "\\t");
            //text = text.Replace("'", "\'");
            text = text.Replace("\"", "\\\"");
            //text = text.Replace("?", "\\?");
            return "\"" + text + "\"";
        }
    }
}
