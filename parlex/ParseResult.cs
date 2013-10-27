using System.Collections.Generic;

namespace parlex {
    public class ParseResult {
        public readonly OldProduction Product;
        public readonly int ParsedTextStartIndex;
        public readonly int ParsedTextLength;
        public readonly IReadOnlyList<ParseResult> SubResults;

        internal ParseResult(OldProduction product, int parsedTextStartIndex, int parsedTextLength, List<ParseResult> subResults) {
            Product = product;
            ParsedTextStartIndex = parsedTextStartIndex;
            ParsedTextLength = parsedTextLength;
            SubResults = subResults.AsReadOnly();
        }
    }
}