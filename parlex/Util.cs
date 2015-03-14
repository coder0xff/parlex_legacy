using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    public class Util {
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
            //text = text.Replace("'", "\'");
            text = text.Replace("\"", "\\\"");
            //text = text.Replace("?", "\\?");
            return "\"" + text + "\"";
        }
    }
}
