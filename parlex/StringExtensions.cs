using System;
using System.Collections.Generic;
using System.Globalization;

namespace Parlex {
    public static class StringExtensions {
        public static Int32[] GetUtf32CodePoints(this string s) {
            var chars = new List<int>((s.Length * 3) / 2);

            var ee = StringInfo.GetTextElementEnumerator(s);

            while (ee.MoveNext()) {
                var e = ee.GetTextElement();
                chars.Add(char.ConvertToUtf32(e, 0));
            }

            return chars.ToArray();
        }
    }
}
