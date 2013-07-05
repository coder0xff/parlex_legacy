using System;
using System.Collections.Generic;
using System.Linq;

namespace parlex {
    class Parser {
        internal class ProductMatchResult {
            public readonly Product Product;
            public readonly int SequenceNumber;
            public readonly IReadOnlyList<ProductMatchResult> SubMatches;
            public readonly int StartingInputIndex;
            public readonly int SourceLength;

            public ProductMatchResult(Product product, int sequenceNumber, List<ProductMatchResult> subMatches, int startingInputIndex, int sourceLength) {
                Product = product;
                SequenceNumber = sequenceNumber;
                SubMatches = subMatches != null ? subMatches.AsReadOnly() : null;
                StartingInputIndex = startingInputIndex;
                SourceLength = sourceLength;
            }
        }

        private readonly HashSet<Product>[] _pendingProductMatches;
        private readonly Dictionary<Product, List<ProductMatchResult>>[] _completedProductMatches;
        private readonly Dictionary<Product, HashSet<SequenceMatchState>>[] _dependentSequences;
        private readonly IEnumerable<ProductMatchResult> _results;
        private readonly Int32[] _textCodePoints;

        private Parser(String text,
                       IEnumerable<Product> builtInCharacterProducts,
                       IEnumerable<Product> products) {
            _textCodePoints = text.GetUtf32CodePoints();
            _pendingProductMatches = new HashSet<Product>[_textCodePoints.Length];
            _completedProductMatches = new Dictionary<Product, List<ProductMatchResult>>[_textCodePoints.Length];
            _dependentSequences = new Dictionary<Product, HashSet<SequenceMatchState>>[_textCodePoints.Length];
            for (int initCollections = 0; initCollections < _textCodePoints.Length; initCollections++) {
                _pendingProductMatches[initCollections] = new HashSet<Product>();
                _completedProductMatches[initCollections] = new Dictionary<Product, List<ProductMatchResult>>();
                _dependentSequences[initCollections] = new Dictionary<Product, HashSet<SequenceMatchState>>();
            }
            ProcessBuiltInCharacterProducts(builtInCharacterProducts);
            foreach (Product product in products) {
                if (product is IBuiltInCharacterProduct) continue;
                StartProductMatch(product, 0);
            }
            if (_completedProductMatches[0] == null) {
                _completedProductMatches[0] = new Dictionary<Product, List<ProductMatchResult>>();
            }
            _results = _completedProductMatches[0].SelectMany(x => x.Value);
        }

        private void ProcessBuiltInCharacterProducts(IEnumerable<Product> builtInCharacterProducts) {
            for (int fillBuiltInProducts = 0; fillBuiltInProducts < _textCodePoints.Length; fillBuiltInProducts++) {
// ReSharper disable PossibleMultipleEnumeration
                foreach (Product builtInCharacterProduct in builtInCharacterProducts) {
// ReSharper restore PossibleMultipleEnumeration
                    if (((IBuiltInCharacterProduct) builtInCharacterProduct).Match(_textCodePoints[fillBuiltInProducts])) {
                        AddProductMatch(new ProductMatchResult(builtInCharacterProduct, -1, null, fillBuiltInProducts, 1));
                    }
                }
            }
        }

        private class SequenceMatchState : IEquatable<SequenceMatchState> {
            public bool Equals(SequenceMatchState other) {
                if (ReferenceEquals(null, other)) {
                    return false;
                }
                if (ReferenceEquals(this, other)) {
                    return true;
                }
                return Equals(_parser, other._parser) && _productSequenceNumber == other._productSequenceNumber &&
                       Equals(_sequence, other._sequence) && _counter == other._counter &&
                       InputIndex == other.InputIndex && _sequenceStartingInputIndex == other._sequenceStartingInputIndex &&
                       Equals(NeededProduct, other.NeededProduct) && Equals(_matchesThusFar, other._matchesThusFar);
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) {
                    return false;
                }
                if (ReferenceEquals(this, obj)) {
                    return true;
                }
                if (obj.GetType() != GetType()) {
                    return false;
                }
                return Equals((SequenceMatchState) obj);
            }

            public override int GetHashCode() {
                unchecked {
                    int hashCode = (_parser != null ? _parser.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ _productSequenceNumber;
                    hashCode = (hashCode*397) ^ (_sequence != null ? _sequence.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ _counter;
                    hashCode = (hashCode*397) ^ InputIndex;
                    hashCode = (hashCode*397) ^ _sequenceStartingInputIndex;
                    hashCode = (hashCode*397) ^ (NeededProduct != null ? NeededProduct.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (_matchesThusFar != null ? _matchesThusFar.GetHashCode() : 0);
                    return hashCode;
                }
            }

            public static bool operator ==(SequenceMatchState left, SequenceMatchState right) {
                return Equals(left, right);
            }

            public static bool operator !=(SequenceMatchState left, SequenceMatchState right) {
                return !Equals(left, right);
            }

            private readonly Parser _parser;
            private readonly int _productSequenceNumber;
            private readonly Analyzer.NfaSequence _sequence;
            private readonly int _counter;
            internal readonly int InputIndex;
            private readonly int _sequenceStartingInputIndex;
            public readonly Analyzer.NfaSequence.ProductReference NeededProduct;
            private readonly List<ProductMatchResult> _matchesThusFar;

            public SequenceMatchState(Parser parser,
                                      int productSequenceNumber,
                                      Analyzer.NfaSequence sequence,
                                      int counter,
                                      int inputIndex,
                                      int sequenceStartingInputIndex,
                                      Analyzer.NfaSequence.ProductReference neededProduct,
                                      List<ProductMatchResult> matchesThusFar) {
                _parser = parser;
                _productSequenceNumber = productSequenceNumber;
                _sequence = sequence;
                _counter = counter;
                InputIndex = inputIndex;
                _sequenceStartingInputIndex = sequenceStartingInputIndex;
                NeededProduct = neededProduct;
                _matchesThusFar = matchesThusFar;
            }

            public void DependencyFulfilled(ProductMatchResult productMatchResult) {
                int nextSourceIndex = productMatchResult.StartingInputIndex + productMatchResult.SourceLength;
// ReSharper disable UseObjectOrCollectionInitializer
                var nextMatchesThusFar = new List<ProductMatchResult>(_matchesThusFar);
// ReSharper restore UseObjectOrCollectionInitializer
                nextMatchesThusFar.Add(productMatchResult);
                if (!NeededProduct.IsRepetitious || !CreateNextStates(_counter, nextSourceIndex, nextMatchesThusFar)) {
                    if (NeededProduct.ExitSequenceCounter == _sequence.SpanLength) {
                        AddProductMatchResult(nextMatchesThusFar, nextSourceIndex);
                    } else {
                        CreateNextStates(NeededProduct.ExitSequenceCounter, nextSourceIndex, nextMatchesThusFar);
                    }
                }
            }

            private void AddProductMatchResult(List<ProductMatchResult> nextMatchesThusFar, int nextSourceIndex) {
                var productMatch = new ProductMatchResult(_sequence.OwnerProduct,
                                                          _productSequenceNumber,
                                                          nextMatchesThusFar,
                                                          _sequenceStartingInputIndex,
                                                          nextSourceIndex - _sequenceStartingInputIndex);
                _parser.AddProductMatch(productMatch);
            }

            public bool CreateNextStates(int nextCounter,
                                         int nextSourceIndex,
                                         List<ProductMatchResult> nextMatchesThusFar) {
                var nextProducts = _sequence.RelationBranches[nextCounter - _sequence.SpanStart];
                bool result = false;
                foreach (var nextProduct in nextProducts) {
                    var nextSequenceMatchState = new SequenceMatchState(_parser,
                                                                        _productSequenceNumber,
                                                                        _sequence,
                                                                        nextCounter,
                                                                        nextSourceIndex,
                                                                        _sequenceStartingInputIndex,
                                                                        nextProduct,
                                                                        nextMatchesThusFar);
                    result |= _parser.AddSequenceMatchState(nextSequenceMatchState);
                    result |= _parser.StartProductMatch(nextProduct.Product, nextSourceIndex);
                }
                return result;
            }
        }

        bool StartProductMatch(Product product, int sourceIndex) {
            if (_completedProductMatches[sourceIndex].ContainsKey(product)) return true;
            if (_pendingProductMatches[sourceIndex].Contains(product)) return false;
            var builtInCharacterProduct = product as IBuiltInCharacterProduct;
            if (builtInCharacterProduct != null) {
                if (builtInCharacterProduct.Match(_textCodePoints[sourceIndex])) {
                    AddProductMatch(new ProductMatchResult(product, -1, null, sourceIndex, 1));
                }
            } else {
                for (int sequenceNumber = 0; sequenceNumber < product.Sequences.Count; sequenceNumber++) {
                    var sequence = product.Sequences[sequenceNumber];
                    var nextSequenceMatchState = new SequenceMatchState(this,
                                                                        sequenceNumber,
                                                                        sequence,
                                                                        sequence.SpanStart,
                                                                        sourceIndex,
                                                                        sourceIndex,
                                                                        null,
                                                                        null);
                    nextSequenceMatchState.CreateNextStates(sequence.SpanStart,
                                                            sourceIndex,
                                                            new List<ProductMatchResult>());
                }
            }
            return _completedProductMatches[sourceIndex].ContainsKey(product);
        }

        bool AddSequenceMatchState(SequenceMatchState sequenceMatchState) {
            int startingInputIndex = sequenceMatchState.InputIndex;
            Product product = sequenceMatchState.NeededProduct.Product;
            MakeProductCollections(startingInputIndex, product);
            _dependentSequences[startingInputIndex][product].Add(sequenceMatchState);
            foreach (ProductMatchResult productMatchResult in _completedProductMatches[startingInputIndex][product]) {
                sequenceMatchState.DependencyFulfilled(productMatchResult);
            }
            return _completedProductMatches[startingInputIndex][product].Count > 0;
        }

        internal static IEnumerable<ProductMatchResult> Parse(String text,
                                                              IEnumerable<Product> builtInCharacterProducts,
                                                              IEnumerable<Product> products) {
            return new Parser(text, builtInCharacterProducts, products)._results;
        }

        private void AddProductMatch(ProductMatchResult productMatch) {
            int startingInputIndex = productMatch.StartingInputIndex;
            Product product = productMatch.Product;
            _pendingProductMatches[startingInputIndex].Remove(product);
            MakeProductCollections(startingInputIndex, product);
            _completedProductMatches[startingInputIndex][product].Add(productMatch);
            foreach (SequenceMatchState dependentSequence in _dependentSequences[startingInputIndex][product]) {
                dependentSequence.DependencyFulfilled(productMatch);
            }
        }

        void MakeProductCollections(int inputIndex, Product product) {
            if (!_completedProductMatches[inputIndex].ContainsKey(product)) {
                _completedProductMatches[inputIndex][product] = new List<ProductMatchResult>();
                _dependentSequences[inputIndex][product] = new HashSet<SequenceMatchState>();
            }
        }
    }
}
