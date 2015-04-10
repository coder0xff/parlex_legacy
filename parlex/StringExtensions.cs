using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Parlex {
    public static class StringExtensions {
        public static Int32[] GetUtf32CodePoints(this string value) {
            if (value == null) {
                throw new ArgumentNullException("value");
            }
            var chars = new List<int>((value.Length*3)/2);

            TextElementEnumerator ee = StringInfo.GetTextElementEnumerator(value);

            while (ee.MoveNext()) {
                string e = ee.GetTextElement();
                chars.Add(char.ConvertToUtf32(e, 0));
            }

            return chars.ToArray();
        }

        public static String Utf32ToString(this Int32[] codePoints, int startIndex = 0, int length = -1) {
            if (codePoints == null) {
                throw new ArgumentNullException("codePoints");
            }
            if (length == -1) length = codePoints.Length - startIndex;
            var sb = new StringBuilder();
            for (int i = startIndex; i < startIndex + length; ++i) {
                sb.Append(Char.ConvertFromUtf32(codePoints[i]));
            }
            return sb.ToString();
        }

        public static String Utf32Substring(this String value, int startIndex, int length = -1) {
            if (value == null) {
                throw new ArgumentNullException("value");
            }
            Int32[] codePoints = value.GetUtf32CodePoints();
            if (length == -1) length = codePoints.Length - startIndex;
            return codePoints.Utf32ToString(startIndex, length);
        }

        public static string Truncate(this string value, int maxChars) {
            if (value == null) {
                throw new ArgumentNullException("value");
            }
            if (maxChars < 0) {
                throw new ArgumentOutOfRangeException("maxChars", "maxChars must be non-negative");
            }
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + " ..";
        }
    }
}