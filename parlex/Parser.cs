using System;
using System.Collections.Generic;
using System.Linq;

namespace parlex {
    public class Parser {
        private class SubMatchChain : IEquatable<SubMatchChain> {
            public bool Equals(SubMatchChain other) {
                if (ReferenceEquals(null, other)) {
                    return false;
                }
                if (ReferenceEquals(this, other)) {
                    return true;
                }
                if (_hashCache != other._hashCache) return false;
                return SubMatches.SequenceEqual(other.SubMatches);
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
                return Equals((SubMatchChain) obj);
            }

            public override int GetHashCode() {
                return _hashCache;
            }

            public static bool operator ==(SubMatchChain left, SubMatchChain right) {
                return Equals(left, right);
            }

            public static bool operator !=(SubMatchChain left, SubMatchChain right) {
                return !Equals(left, right);
            }

            internal class Entry : IEquatable<Entry> {
                public bool Equals(Entry other) {
                    if (ReferenceEquals(null, other)) {
                        return false;
                    }
                    if (ReferenceEquals(this, other)) {
                        return true;
                    }
                    return Product.Equals(other.Product) && LengthInParsedText == other.LengthInParsedText;
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
                    return Equals((Entry) obj);
                }

                public override int GetHashCode() {
                    unchecked {
                        return (Product.GetHashCode() * 397) ^ LengthInParsedText;
                    }
                }

                public static bool operator ==(Entry left, Entry right) {
                    return Equals(left, right);
                }

                public static bool operator !=(Entry left, Entry right) {
                    return !Equals(left, right);
                }

                internal readonly Product Product;
                internal readonly int LengthInParsedText;

                internal Entry(Product product, int lengthInParsedText) {
                    Product = product;
                    LengthInParsedText = lengthInParsedText;
                }
            }

            internal readonly List<Entry> SubMatches;
            internal readonly int LengthInParsedText;
            private readonly int _hashCache;

            internal SubMatchChain(List<Entry> subMatches, int lengthInParsedText) {
                System.Diagnostics.Debug.Assert(lengthInParsedText > 0);
                SubMatches = subMatches;
                LengthInParsedText = lengthInParsedText;
                _hashCache = ComputeHash();
            }

            int ComputeHash() {
                return SubMatches.Aggregate(0, (current, subMatch) => current*397 ^ subMatch.GetHashCode());
            }
        }

        private readonly Dictionary<Product, Dictionary<int /*length*/, SubMatchChain>>[] _completedProductMatches;
        private readonly Int32[] _textCodePoints;
        private readonly StrictPartialOrder<Product> _precedence;
        private readonly ParseResult _result;

        private Parser(String text, Product toMatch, StrictPartialOrder<Product> precedence) {
            _textCodePoints = text.GetUtf32CodePoints();
            _completedProductMatches = new Dictionary<Product, Dictionary<int, SubMatchChain>>[_textCodePoints.Length];
            for (int initCollections = 0; initCollections < _textCodePoints.Length; initCollections++) {
                _completedProductMatches[initCollections] = new Dictionary<Product, Dictionary<int, SubMatchChain>>();
            }
            _precedence = precedence;
            MatchProduct(toMatch, 0, new Dictionary<Product, DependencyMediator>());
            _result = PrepareResult(toMatch, text.Length);
        }

        static public ParseResult Parse(String text, String productName, CompiledGrammar grammar) {
            var toMatch = grammar.UserProducts[productName];
            return new Parser(text, toMatch, grammar.Precedences)._result;
        }

        private class DependencyMediator {
            internal readonly Dictionary<int, HashSet<SubMatchChain>> CompletedSoFar = new Dictionary<int, HashSet<SubMatchChain>>();
            private readonly List<SequenceMatchState> _dependents = new List<SequenceMatchState>();

            internal void AddMatchChain(SubMatchChain subMatchChain) {
                int length = subMatchChain.LengthInParsedText;
                bool newLength = !CompletedSoFar.ContainsKey(length);
                if (newLength) {
                    CompletedSoFar.Add(length, new HashSet<SubMatchChain>());
                }
                if (CompletedSoFar[length].Add(subMatchChain)) {
                    if (newLength) {
                        foreach (var sequenceMatchState in _dependents) {
                            sequenceMatchState.DependencyFulfilled(length);
                        }
                    }
                }
            }

            internal void AddDependent(SequenceMatchState sequenceMatchState) {
                _dependents.Add(sequenceMatchState);
                if (CompletedSoFar.Count > 0) {
                    foreach (var length in new List<int>(CompletedSoFar.Keys)) {
                        sequenceMatchState.DependencyFulfilled(length);
                    }
                }
            }

            internal void MatchingFinished() {
                if (CompletedSoFar.Count == 0) {
                    foreach (var unfulfilledDependent in _dependents) {
                        unfulfilledDependent.DependencyUnfulfilled();
                    }
                }
            }
        }

        private class SequenceMatchState {
            private readonly Parser _parser;
            private readonly CompiledGrammar.NfaSequence _sequence;
            private readonly int _counter;
            private readonly int _textToParseIndex;
            private readonly int _sequenceStartingTextToParseIndex;
            private readonly CompiledGrammar.NfaSequence.ProductReference _neededProduct;
            private readonly List<SubMatchChain.Entry> _matchesThusFar;
            private readonly DependencyMediator _dependencyMediator;

            internal SequenceMatchState(Parser parser, CompiledGrammar.NfaSequence sequence, int counter, int textToParseIndex, int sequenceStartingTextToParseIndex, CompiledGrammar.NfaSequence.ProductReference neededProduct, List<SubMatchChain.Entry> matchesThusFar, DependencyMediator dependencyMediator) {
                _parser = parser;
                _sequence = sequence;
                _counter = counter;
                _textToParseIndex = textToParseIndex;
                _sequenceStartingTextToParseIndex = sequenceStartingTextToParseIndex;
                _neededProduct = neededProduct;
                _matchesThusFar = matchesThusFar;
                _dependencyMediator = dependencyMediator;
            }

            internal void DependencyFulfilled(int length) {
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

            internal void DependencyUnfulfilled() {
                if (_neededProduct.IsRepetitious) {
                    if (_neededProduct.ExitSequenceCounter == _sequence.SpanStart + _sequence.SpanLength) {
                        _dependencyMediator.AddMatchChain(new SubMatchChain(_matchesThusFar, _textToParseIndex - _sequenceStartingTextToParseIndex));
                    } else {
                        CreateNextStates(_neededProduct.ExitSequenceCounter, _textToParseIndex, _matchesThusFar, new Dictionary<Product, DependencyMediator>());
                    }
                } else {
                    if (_matchesThusFar.Count > 0) {
                        System.Diagnostics.Debug.WriteLine("Expected a " + _neededProduct.Product + " at index:" + _textToParseIndex + " to continue a " + _sequence.OwnerProduct.Title + " sequence that started at index:" + _sequenceStartingTextToParseIndex + ".");
                    }
                }
            }

            internal void CreateNextStates(int nextCounter, int nextTextToParseIndex, List<SubMatchChain.Entry> nextMatchesThusFar, Dictionary<Product, DependencyMediator> dependencyMediators) {
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
                        if (dependencyMediators.ContainsKey(nextProduct)) {
                            dependencyMediators[nextProduct].AddDependent(nextSequenceMatchState);
                        } else {
                            nextSequenceMatchState.DependencyUnfulfilled();
                        }
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
                return new HashSet<int>();
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

            //bool hadNoResults = dependencyMediators[product].CompletedSoFar.Count == 0;

            if (isTopRecursionLevel) {
                foreach (var mediator in dependencyMediators) {
                    mediator.Value.MatchingFinished();
                }

                foreach (var mediator in dependencyMediators) {
                    foreach (var length in mediator.Value.CompletedSoFar.Keys) {
                        GetSubMatchChainFromCompletedProductsOrDependencyMediators(new SubMatchChain.Entry(mediator.Key, length), textToParseIndex, dependencyMediators, new Stack<SubMatchChain.Entry>());
                    }
                }

                ////If results where only finally made by calling DependencyUnfulfilled, then we assume we had some repetitious recursion. In this case, only the longest match is valid.
                //if (hadNoResults && dependencyMediators[product].CompletedSoFar.Count > 0) {
                //    int selectedLength = _completedProductMatches[textToParseIndex][product].Keys.Max();
                //    SubMatchChain singularResult = _completedProductMatches[textToParseIndex][product][selectedLength];
                //    _completedProductMatches[textToParseIndex][product].Clear();
                //    _completedProductMatches[textToParseIndex][product].Add(selectedLength, singularResult);
                //}

                dependencyMediators.Clear();

                if (_completedProductMatches[textToParseIndex].ContainsKey(product)) {
                    return new HashSet<int>(_completedProductMatches[textToParseIndex][product].Keys);
                }
            }

            return null;
        }

        /// <summary>
        /// Based on several possible matches of the same length, which one is best. This is decided by repeatedly calling ChooseBestSubMatchChain, which may recursively call this function
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="indexInTextToParse"></param>
        /// <param name="dependencyMediators"></param>
        /// <param name="stack"></param>
        /// <returns></returns>
        private SubMatchChain GetSubMatchChainFromCompletedProductsOrDependencyMediators(SubMatchChain.Entry entry, int indexInTextToParse, Dictionary<Product, DependencyMediator> dependencyMediators, Stack<SubMatchChain.Entry> stack) {
            if (_completedProductMatches[indexInTextToParse].ContainsKey(entry.Product)) {
                if (_completedProductMatches[indexInTextToParse][entry.Product].ContainsKey(entry.LengthInParsedText)) {
                    return _completedProductMatches[indexInTextToParse][entry.Product][entry.LengthInParsedText];
                }
            }
            var candidates = dependencyMediators[entry.Product].CompletedSoFar[entry.LengthInParsedText];
            var candidatesList = new List<SubMatchChain>(candidates);
            for (int indexA = 0; indexA < candidatesList.Count - 1; indexA++) {
                for (int indexB = indexA + 1; indexB < candidatesList.Count;) {
                    var better = ChooseBestSubMatchChain(candidatesList[indexA], candidatesList[indexA + 1], indexInTextToParse, dependencyMediators, stack);
                    if (better == candidatesList[indexA]) {
                        candidatesList.RemoveAt(indexB);
                    } else if (better == candidatesList[indexB]) {
                        candidatesList[indexA] = candidatesList[indexB];
                        candidatesList.RemoveAt(indexB);
                        indexB = indexA + 1;
                    } else {
                        indexB++;
                    }
                }
            }
            candidates.Clear();
            foreach (var subMatchChain in candidatesList) {
                candidates.Add(subMatchChain);
            }
            if (candidates.Count == 1) {
                if (!_completedProductMatches[indexInTextToParse].ContainsKey(entry.Product)) {
                    _completedProductMatches[indexInTextToParse].Add(entry.Product, new Dictionary<int, SubMatchChain>());
                }
                _completedProductMatches[indexInTextToParse][entry.Product].Add(entry.LengthInParsedText, candidates.First());
                return candidates.First();
            }
            return null;
        }

        private int GetOrder(Product a, Product b) {
            return _precedence.Compare(a, b);
        }

        private SubMatchChain ChooseBestSubMatchChain(SubMatchChain a, SubMatchChain b, int indexInTextToParse, Dictionary<Product, DependencyMediator> dependencyMediators, Stack<SubMatchChain.Entry> stack) {
            bool aIsBuiltIn = a.SubMatches.Count == 0;
            bool bIsBuiltIn = b.SubMatches.Count == 0;
            if (aIsBuiltIn && bIsBuiltIn) throw new ApplicationException();
            if (aIsBuiltIn) return b;
            if (bIsBuiltIn) return a;
            for (int subMatchIndex = 0; subMatchIndex < a.SubMatches.Count; subMatchIndex++) {
                Product aSubProduct = a.SubMatches[subMatchIndex].Product;
                Product bSubProduct = b.SubMatches[subMatchIndex].Product;
                int precedence = GetOrder(aSubProduct, bSubProduct);
                if (precedence < 0) return b;
                if (precedence > 0) return a;
                bool aSubProductIsBuiltIn = aSubProduct is IBuiltInCharacterProduct;
                bool bSubProductIsBuiltIn = bSubProduct is IBuiltInCharacterProduct;
                if (aSubProductIsBuiltIn && !bSubProductIsBuiltIn) return b;
                if (!aSubProductIsBuiltIn && bSubProductIsBuiltIn) return a;
                if (aSubProductIsBuiltIn) continue;
                int aSubLength = a.SubMatches[subMatchIndex].LengthInParsedText;
                int bSubLength = b.SubMatches[subMatchIndex].LengthInParsedText;
                if (aSubLength > bSubLength) {
                    return a;
                }
                if (bSubLength > aSubLength) {
                    return b;
                }
            }

            var firstEntryA = a.SubMatches[0];
            if (stack.Contains(firstEntryA)) throw new ApplicationException();
            stack.Push(firstEntryA);
            SubMatchChain firstEntryChainA = GetSubMatchChainFromCompletedProductsOrDependencyMediators(firstEntryA, indexInTextToParse, dependencyMediators, stack);
            stack.Pop();

            var firstEntryB = b.SubMatches[0];
            if (stack.Contains(firstEntryB)) throw new ApplicationException();
            stack.Push(firstEntryB);
            SubMatchChain firstEntryChainB = GetSubMatchChainFromCompletedProductsOrDependencyMediators(firstEntryB, indexInTextToParse, dependencyMediators, stack);
            stack.Pop();

            var subBest = ChooseBestSubMatchChain(firstEntryChainA, firstEntryChainB, indexInTextToParse, dependencyMediators, stack);
            if (subBest == firstEntryChainA) return a;
            if (subBest == firstEntryChainB) return b;

            return null;
        }

        private ParseResult Resultify(int indexInParsedText, Product product, int length, Dictionary<Product, Dictionary<int, ParseResult>>[] converted) {
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

        private ParseResult PrepareResult(Product toMatch, int length) {
            var converted = new Dictionary<Product, Dictionary<int, ParseResult>>[_textCodePoints.Length];
            if (_completedProductMatches[0].ContainsKey(toMatch)) {
                if (_completedProductMatches[0][toMatch].ContainsKey(length)) {
                    return Resultify(0, toMatch, length, converted);
                }
            }
            return null;
        }
    }
}
