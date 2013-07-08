using System;
using System.Collections.Generic;
using System.Linq;

namespace parlex {
    class Parser {
        public class SubMatchChain {
            public class Entry {
                public Product Product;
                public int LengthInParsedText;

                public Entry(Product product, int lengthInParsedText) {
                    Product = product;
                    LengthInParsedText = lengthInParsedText;
                }
            }

            public readonly List<Entry> SubMatches;
            public readonly int LengthInParsedText;

            public SubMatchChain(List<Entry> subMatches, int lengthInParsedText) {
                System.Diagnostics.Debug.Assert(lengthInParsedText > 0);
                SubMatches = subMatches;
                LengthInParsedText = lengthInParsedText;
            }
        }

        private readonly Dictionary<Product, Dictionary<int /*length*/, SubMatchChain>>[] _completedProductMatches;
        private readonly Int32[] _textCodePoints;

        public class ParseResult {
            public readonly Product Product;
            public readonly int ParsedTextStartIndex;
            public readonly int ParsedTextLength;
            public readonly IReadOnlyList<ParseResult> SubResults;

            public ParseResult(Product product, int parsedTextStartIndex, int parsedTextLength, List<ParseResult> subResults) {
                Product = product;
                ParsedTextStartIndex = parsedTextStartIndex;
                ParsedTextLength = parsedTextLength;
                SubResults = subResults.AsReadOnly();
            }
        }

        private readonly List<ParseResult> _results;

        public IEnumerable<ParseResult> Results { get { return _results; } }

        public Parser(String text,
                       IEnumerable<Product> products) {
            _textCodePoints = text.GetUtf32CodePoints();
            _completedProductMatches = new Dictionary<Product, Dictionary<int, SubMatchChain>>[_textCodePoints.Length];
            for (int initCollections = 0; initCollections < _textCodePoints.Length; initCollections++) {
                _completedProductMatches[initCollections] = new Dictionary<Product, Dictionary<int, SubMatchChain>>();
            }
            //ProcessBuiltInCharacterProducts(builtInCharacterProducts);
            foreach (Product product in products) {
                if (product is IBuiltInCharacterProduct) continue;
                if (product.Title != "identifier") continue;
                MatchProduct(product, 0, new Dictionary<Product, List<SubMatchChain>>(), new Dictionary<Product, List<SequenceMatchState>>());

            }
            _results = new List<ParseResult>();
            PrepareResults();
        }

        private class SequenceMatchState {
            private readonly Parser _parser;
            private readonly Analyzer.NfaSequence _sequence;
            private readonly int _counter;
            private readonly int _textToParseIndex;
            private readonly int _sequenceStartingTextToParseIndex;
            private readonly Analyzer.NfaSequence.ProductReference _neededProduct;
            private readonly List<SubMatchChain.Entry> _matchesThusFar;
            private readonly List<SubMatchChain> _completionResults;

            public SequenceMatchState(Parser parser, Analyzer.NfaSequence sequence, int counter, int textToParseIndex, int sequenceStartingTextToParseIndex, Analyzer.NfaSequence.ProductReference neededProduct, List<SubMatchChain.Entry> matchesThusFar, List<SubMatchChain> completionResults) {
                _parser = parser;
                _sequence = sequence;
                _counter = counter;
                _textToParseIndex = textToParseIndex;
                _sequenceStartingTextToParseIndex = sequenceStartingTextToParseIndex;
                _neededProduct = neededProduct;
                _matchesThusFar = matchesThusFar;
                _completionResults = completionResults;
            }

            public void DependencyFulfilled(SubMatchChain subMatchChain) {
                int nextTextToParseIndex = _textToParseIndex + subMatchChain.LengthInParsedText;
                var nextMatchesThusFar = new List<SubMatchChain.Entry>(_matchesThusFar);
                nextMatchesThusFar.Add(new SubMatchChain.Entry(_neededProduct.Product, subMatchChain.LengthInParsedText));
                if (_neededProduct.IsRepetitious) {
                    CreateNextStates(_counter, nextTextToParseIndex, nextMatchesThusFar, new Dictionary<Product, List<SubMatchChain>>(), new Dictionary<Product, List<SequenceMatchState>>());
                } else {
                    if (_neededProduct.ExitSequenceCounter == _sequence.SpanStart + _sequence.SpanLength) {
                        _completionResults.Add(new SubMatchChain(nextMatchesThusFar, nextTextToParseIndex - _sequenceStartingTextToParseIndex));
                    } else {
                        CreateNextStates(_neededProduct.ExitSequenceCounter, nextTextToParseIndex, nextMatchesThusFar, new Dictionary<Product, List<SubMatchChain>>(), new Dictionary<Product, List<SequenceMatchState>>());
                    }
                }
            }

            public void DependencyUnfulfilled() {
                if (_neededProduct.IsRepetitious) {
                    if (_neededProduct.ExitSequenceCounter == _sequence.SpanStart + _sequence.SpanLength) {
                        _completionResults.Add(new SubMatchChain(_matchesThusFar, _textToParseIndex));
                    } else {
                        CreateNextStates(_neededProduct.ExitSequenceCounter, _textToParseIndex, _matchesThusFar, new Dictionary<Product, List<SubMatchChain>>(), new Dictionary<Product, List<SequenceMatchState>>());
                    }
                }
            }

            public void CreateNextStates(int nextCounter, int nextTextToParseIndex, List<SubMatchChain.Entry> nextMatchesThusFar, Dictionary<Product, List<SubMatchChain>> productsMatchResults, Dictionary<Product, List<SequenceMatchState>> productsLeftRecursions) {
                var nextProducts = _sequence.RelationBranches[nextCounter - _sequence.SpanStart];
                foreach (var nextProductReference in nextProducts) {
                    var nextProduct = nextProductReference.Product;
                    bool isNotALeftRecursion = !productsMatchResults.ContainsKey(nextProduct);
                    var nextSequenceMatchState = new SequenceMatchState(_parser, _sequence, nextCounter, nextTextToParseIndex, _sequenceStartingTextToParseIndex, nextProductReference, nextMatchesThusFar, _completionResults);
                    if (isNotALeftRecursion) {
                        List<SubMatchChain> matchResults = _parser.MatchProduct(nextProductReference.Product, nextTextToParseIndex, productsMatchResults, productsLeftRecursions);
                        if (matchResults.Count > 0) {
                            foreach (var matchResult in matchResults) {
                                nextSequenceMatchState.DependencyFulfilled(matchResult);
                            }
                        } else {
                            nextSequenceMatchState.DependencyUnfulfilled();
                        }
                    } else {
                        productsLeftRecursions[nextProduct].Add(nextSequenceMatchState);
                    }
                }
            }
        }

        private List<SubMatchChain> MatchProduct(Product product, int textToParseIndex, Dictionary<Product, List<SubMatchChain>> productsMatchResults, Dictionary<Product, List<SequenceMatchState>> productsLeftRecursions) {
            System.Diagnostics.Debug.Assert(!productsMatchResults.ContainsKey(product));
            System.Diagnostics.Debug.Assert(!productsLeftRecursions.ContainsKey(product));

            if (textToParseIndex >= _textCodePoints.Length) {
                return new List<SubMatchChain>();
            }

            if (_completedProductMatches[textToParseIndex].ContainsKey(product)) {
                return _completedProductMatches[textToParseIndex][product].Select(x => x.Value).ToList();
            }

            var results = new List<SubMatchChain>();

            productsMatchResults.Add(product, results);
            var leftRecursions = new List<SequenceMatchState>();
            productsLeftRecursions.Add(product, leftRecursions);

            var builtInCharacterProduct = product as IBuiltInCharacterProduct;
            if (builtInCharacterProduct != null) {
                if (builtInCharacterProduct.Match(_textCodePoints[textToParseIndex])) {
                    var subMatches = new List<SubMatchChain.Entry>();
                    results.Add(new SubMatchChain(subMatches, 1));
                }
            } else {
                for (int sequenceNumber = 0; sequenceNumber < product.Sequences.Count; sequenceNumber++) {
                    var sequence = product.Sequences[sequenceNumber];
                    var nextSequenceMatchState = new SequenceMatchState(this, sequence, sequence.SpanStart, textToParseIndex, textToParseIndex, null, null, results);

                    nextSequenceMatchState.CreateNextStates(sequence.SpanStart, textToParseIndex, new List<SubMatchChain.Entry>(), productsMatchResults, productsLeftRecursions);
                }
            }
            // Do not use foreach or linq here. We expect that results.Count might increase
            if (results.Count > 0) {
                for (int resultIndex = 0; resultIndex < results.Count; resultIndex++) {
                    foreach (SequenceMatchState sequenceMatchState in leftRecursions) {
                        sequenceMatchState.DependencyFulfilled(results[resultIndex]);
                    }
                }
            } else {
                foreach (SequenceMatchState sequenceMatchState in leftRecursions) {
                    sequenceMatchState.DependencyUnfulfilled();
                }
            }
            productsMatchResults.Remove(product);
            productsLeftRecursions.Remove(product);

            //do this even with no results, because no match is still a correct result
            var bestMatchChainsPerLength = new Dictionary<int, SubMatchChain>();

            foreach (var subMatchChain in results) {
                int length = subMatchChain.LengthInParsedText;
                if (bestMatchChainsPerLength.ContainsKey(length)) {
                    bestMatchChainsPerLength[length] = ChooseBestSubMatchChain(textToParseIndex, bestMatchChainsPerLength[length], subMatchChain);
                } else {
                    bestMatchChainsPerLength[length] = subMatchChain;
                }
            }

            _completedProductMatches[textToParseIndex][product] = bestMatchChainsPerLength;

            return results;
        }

        int GetSubMatchChainDepthComplexity(int indexInTextParse, SubMatchChain subMatchChain) {
            int resultMinusOne = 0;
            foreach (var entry in subMatchChain.SubMatches) {
                var childSubMatchChain = _completedProductMatches[indexInTextParse][entry.Product][entry.LengthInParsedText];
                resultMinusOne = Math.Max(resultMinusOne, GetSubMatchChainDepthComplexity(indexInTextParse, childSubMatchChain));
                indexInTextParse += childSubMatchChain.LengthInParsedText;
            }
            return resultMinusOne + 1;
        }

        SubMatchChain ChooseBestSubMatchChain(int indexInTextToParse, SubMatchChain a, SubMatchChain b) {
            return GetSubMatchChainDepthComplexity(indexInTextToParse, a) > GetSubMatchChainDepthComplexity(indexInTextToParse, b) ? a : b;
        }

        ParseResult Resultify(int indexInParsedText, Product product, int length, Dictionary<Product, Dictionary<int, ParseResult>>[] converted) {
            var indexConversions = converted[indexInParsedText];
            if (indexConversions == null) {
                indexConversions = new Dictionary<Product, Dictionary<int, ParseResult>>();
                converted[indexInParsedText] = indexConversions;
            }

            Dictionary<int, ParseResult> productConversions;
            if (indexConversions.ContainsKey(product)) {
                productConversions = indexConversions[product];
            } else {
                productConversions = new Dictionary<int, ParseResult>();
                indexConversions.Add(product, productConversions);
            }

            ParseResult result;
            if (productConversions.ContainsKey(length)) {
                result = productConversions[length];
            } else {
                int currentIndexInParsedText = indexInParsedText;
                var subMatches = new List<ParseResult>();
                var subMatchChain = _completedProductMatches[indexInParsedText][product][length];
                foreach (var subMatch in subMatchChain.SubMatches) {
                    subMatches.Add(Resultify(currentIndexInParsedText, subMatch.Product, subMatch.LengthInParsedText, converted));
                    currentIndexInParsedText += subMatch.LengthInParsedText;
                }
                result = new ParseResult(product, indexInParsedText, subMatchChain.LengthInParsedText, subMatches);
                productConversions.Add(length, result);
            }

            return result;
        }

        void PrepareResults() {
            var converted = new Dictionary<Product, Dictionary<int, ParseResult>>[_textCodePoints.Length];
            foreach (var completedProductMatch in _completedProductMatches[0]) {
                if (completedProductMatch.Value.ContainsKey(_textCodePoints.Length)) {
                    _results.Add(Resultify(0, completedProductMatch.Key, _textCodePoints.Length, converted));
                }
            }
        }
    }
}
