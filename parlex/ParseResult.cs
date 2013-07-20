using System.Collections.Generic;

namespace parlex {
    public class ParseResult {
        public readonly Product Product;
        public readonly int ParsedTextStartIndex;
        public readonly int ParsedTextLength;
        public readonly IReadOnlyList<ParseResult> SubResults;

        internal ParseResult(Product product, int parsedTextStartIndex, int parsedTextLength, List<ParseResult> subResults) {
            Product = product;
            ParsedTextStartIndex = parsedTextStartIndex;
            ParsedTextLength = parsedTextLength;
            SubResults = subResults.AsReadOnly();
        }
    }
}