using System;
using System.Collections.Generic;
using System.Linq;

namespace parlex {
    class Parser {
        public class SubMatchChain {
            public class Entry {
                public readonly Product Product;
                public readonly int LengthInParsedText;

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
            foreach (Product product in products) {
                if (product is IBuiltInCharacterProduct) continue;
                MatchProduct(product, 0, new Dictionary<Product, DependencyMediator>());
            }
            _results = new List<ParseResult>();
            PrepareResults();
        }

        private class DependencyMediator {
            private readonly List<SubMatchChain> _completedSoFar = new List<SubMatchChain>();
            private readonly HashSet<int> _completedLengthSoFar = new HashSet<int>();
            private readonly List<SequenceMatchState> _dependents = new List<SequenceMatchState>();
            private readonly HashSet<SequenceMatchState> _unfulfilledDependents = new HashSet<SequenceMatchState>();

            public void AddMatchChain(SubMatchChain subMatchChain) {
                _completedSoFar.Add(subMatchChain);
                if (_completedLengthSoFar.Add(subMatchChain.LengthInParsedText)) {
                    foreach (var sequenceMatchState in _dependents) {
                        sequenceMatchState.DependencyFulfilled(subMatchChain.LengthInParsedText);
                        _unfulfilledDependents.Remove(sequenceMatchState);
                    }
                }
            }

            public void AddDependent(SequenceMatchState sequenceMatchState) {
                _dependents.Add(sequenceMatchState);
                if (_completedSoFar.Count > 0) {
                    foreach (var subMatchChain in new List<SubMatchChain>(_completedSoFar)) {
                        sequenceMatchState.DependencyFulfilled(subMatchChain.LengthInParsedText);
                    }
                } else {
                    _unfulfilledDependents.Add(sequenceMatchState);
                }
            }

            public void MatchingFinished() {
                foreach (var unfulfilledDependent in _unfulfilledDependents) {
                    unfulfilledDependent.DependencyUnfulfilled();
                }
            }

            public IEnumerable<SubMatchChain> Results { get { return _completedSoFar; } }

            public int ResultCount { get { return _completedSoFar.Count; } }
        }

        private class SequenceMatchState {
            private readonly Parser _parser;
            private readonly Analyzer.NfaSequence _sequence;
            private readonly int _counter;
            private readonly int _textToParseIndex;
            private readonly int _sequenceStartingTextToParseIndex;
            private readonly Analyzer.NfaSequence.ProductReference _neededProduct;
            private readonly List<SubMatchChain.Entry> _matchesThusFar;
            private readonly DependencyMediator _dependencyMediator;

            public SequenceMatchState(Parser parser, Analyzer.NfaSequence sequence, int counter, int textToParseIndex, int sequenceStartingTextToParseIndex, Analyzer.NfaSequence.ProductReference neededProduct, List<SubMatchChain.Entry> matchesThusFar, DependencyMediator dependencyMediator) {
                _parser = parser;
                _sequence = sequence;
                _counter = counter;
                _textToParseIndex = textToParseIndex;
                _sequenceStartingTextToParseIndex = sequenceStartingTextToParseIndex;
                _neededProduct = neededProduct;
                _matchesThusFar = matchesThusFar;
                _dependencyMediator = dependencyMediator;
            }

            public void DependencyFulfilled(int length) {
                int nextTextToParseIndex = _textToParseIndex + length;
                var nextMatchesThusFar = new List<SubMatchChain.Entry>(_matchesThusFar) {new SubMatchChain.Entry(_neededProduct.Product, length)};
                if (_neededProduct.IsRepetitious) {
                    CreateNextStates(_counter, nextTextToParseIndex, nextMatchesThusFar, new Dictionary<Product, DependencyMediator>());
                } else {
                    if (_neededProduct.ExitSequenceCounter == _sequence.SpanStart + _sequence.SpanLength) {
                        _dependencyMediator.AddMatchChain(new SubMatchChain(nextMatchesThusFar, nextTextToParseIndex - _sequenceStartingTextToParseIndex));
                    } else {
                        CreateNextStates(_neededProduct.ExitSequenceCounter, nextTextToParseIndex, nextMatchesThusFar, new Dictionary<Product, DependencyMediator>());
                    }
                }
            }

            public void DependencyUnfulfilled() {
                if (_neededProduct.IsRepetitious) {
                    if (_neededProduct.ExitSequenceCounter == _sequence.SpanStart + _sequence.SpanLength) {
                        _dependencyMediator.AddMatchChain(new SubMatchChain(_matchesThusFar, _textToParseIndex - _sequenceStartingTextToParseIndex));
                    } else {
                        CreateNextStates(_neededProduct.ExitSequenceCounter, _textToParseIndex, _matchesThusFar, new Dictionary<Product, DependencyMediator>());
                    }
                }
            }

            public void CreateNextStates(int nextCounter, int nextTextToParseIndex, List<SubMatchChain.Entry> nextMatchesThusFar, Dictionary<Product, DependencyMediator> dependencyMediators) {
                var nextProducts = _sequence.RelationBranches[nextCounter - _sequence.SpanStart];
                foreach (var nextProductReference in nextProducts) {
                    var nextProduct = nextProductReference.Product;
                    var nextSequenceMatchState = new SequenceMatchState(_parser, _sequence, nextCounter, nextTextToParseIndex, _sequenceStartingTextToParseIndex, nextProductReference, nextMatchesThusFar, _dependencyMediator);
                    HashSet<int> matchResults = null;
                    if (!dependencyMediators.ContainsKey(nextProduct)) {
                        matchResults = _parser.MatchProduct(nextProduct, nextTextToParseIndex, dependencyMediators);
                    }
                    if (matchResults != null) {
                        if (matchResults.Count > 0) {
                            foreach (var matchResult in matchResults) {
                                nextSequenceMatchState.DependencyFulfilled(matchResult);
                            }
                        } else {
                            nextSequenceMatchState.DependencyUnfulfilled();
                        }
                    } else {
                        dependencyMediators[nextProduct].AddDependent(nextSequenceMatchState);
                    }
                }
            }
        }

        private HashSet<int> MatchProduct(Product product, int textToParseIndex, Dictionary<Product, DependencyMediator> dependencyMediators) {
            System.Diagnostics.Debug.Assert(!dependencyMediators.ContainsKey(product));
            bool isTopRecursionLevel = dependencyMediators.Count == 0;

            if (textToParseIndex >= _textCodePoints.Length) {
                return new HashSet<int>();
            }

            var builtInCharacterProduct = product as IBuiltInCharacterProduct;
            if (builtInCharacterProduct != null) {
                if (builtInCharacterProduct.Match(_textCodePoints[textToParseIndex])) {
                    return new HashSet<int> { 1 };
                }
            }

            if (_completedProductMatches[textToParseIndex].ContainsKey(product)) {
                return new HashSet<int>(_completedProductMatches[textToParseIndex][product].Keys);
            }

            var dependencyMediator = new DependencyMediator();
            dependencyMediators.Add(product, dependencyMediator);

            foreach (var sequence in product.Sequences) {
                var precursorSequenceMatchState = new SequenceMatchState(this, sequence, sequence.SpanStart, textToParseIndex, textToParseIndex, null, null, dependencyMediator);
                precursorSequenceMatchState.CreateNextStates(sequence.SpanStart, textToParseIndex, new List<SubMatchChain.Entry>(), dependencyMediators);
            }

            bool hadNoResults = dependencyMediators[product].ResultCount == 0;
            if (isTopRecursionLevel) {
                foreach (var mediator in dependencyMediators) {
                    mediator.Value.MatchingFinished();

                    var bestMatchChainsPerLength = new Dictionary<int, SubMatchChain>();

                    foreach (var subMatchChain in mediator.Value.Results) {
                        int length = subMatchChain.LengthInParsedText;
                        if (bestMatchChainsPerLength.ContainsKey(length)) {
                            bestMatchChainsPerLength[length] = ChooseBestSubMatchChain(textToParseIndex, bestMatchChainsPerLength[length], subMatchChain);
                        } else {
                            bestMatchChainsPerLength[length] = subMatchChain;
                        }
                    }

                    _completedProductMatches[textToParseIndex][mediator.Key] = bestMatchChainsPerLength;
                }

                if (hadNoResults && dependencyMediators[product].ResultCount > 0) {
                    int selectedLength = _completedProductMatches[textToParseIndex][product].Keys.Max();
                    SubMatchChain singularResult = _completedProductMatches[textToParseIndex][product][selectedLength];
                    _completedProductMatches[textToParseIndex][product].Clear();
                    _completedProductMatches[textToParseIndex][product].Add(selectedLength, singularResult);
                }

                return new HashSet<int>(_completedProductMatches[textToParseIndex][product].Keys);
            }

            return null;
        }

        int GetSubMatchChainDescendentCount(int indexInTextParse, SubMatchChain subMatchChain) {
            int resultMinusOne = 0;
            foreach (var entry in subMatchChain.SubMatches) {
                int childDepth = 0;
                if (_completedProductMatches[indexInTextParse].ContainsKey(entry.Product)) {
                    var childSubMatchChain = _completedProductMatches[indexInTextParse][entry.Product][entry.LengthInParsedText];
                    childDepth = GetSubMatchChainDescendentCount(indexInTextParse, childSubMatchChain);
                }
                resultMinusOne += childDepth;
                indexInTextParse += entry.LengthInParsedText;
            }
            return resultMinusOne + 1;
        }

        SubMatchChain ChooseBestSubMatchChain(int indexInTextToParse, SubMatchChain a, SubMatchChain b) {
            bool aIsBuiltIn = a.SubMatches.Count == 0;
            bool bIsBuiltIn = b.SubMatches.Count == 0;
            if (aIsBuiltIn && bIsBuiltIn) throw new ApplicationException();
            if (aIsBuiltIn) return b;
            if (bIsBuiltIn) return a;
            for (int subMatchIndex = 0; subMatchIndex < a.SubMatches.Count; subMatchIndex++) {
                int aSubLength = a.SubMatches[subMatchIndex].LengthInParsedText;
                int bSubLength = b.SubMatches[subMatchIndex].LengthInParsedText;
                if (aSubLength > bSubLength) return a;
                if (bSubLength > aSubLength) return b;
            }
            return GetSubMatchChainDescendentCount(indexInTextToParse, a) > GetSubMatchChainDescendentCount(indexInTextToParse, b) ? a : b;
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
                if (_completedProductMatches[indexInParsedText].ContainsKey(product)) {
                    var subMatchChain = _completedProductMatches[indexInParsedText][product][length];
                    foreach (var subMatch in subMatchChain.SubMatches) {
                        subMatches.Add(Resultify(currentIndexInParsedText, subMatch.Product, subMatch.LengthInParsedText, converted));
                        currentIndexInParsedText += subMatch.LengthInParsedText;
                    }
                }
                result = new ParseResult(product, indexInParsedText, length, subMatches);
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
