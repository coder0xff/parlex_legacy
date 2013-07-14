﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace parlex {
    internal class Analyzer {
        //The NFA subset created for a particular example of a particular product.
        //While the sub-products it utilizes will be NFAs all their own, this one follows
        //a particular sequence, and each element must be satisfied

        private readonly Dictionary<Int32, Product> _codePointProducts = new Dictionary<Int32, Product>();
        private readonly List<CharacterClassCharacterProduct> _characterClassProducts = new List<CharacterClassCharacterProduct>();
        private readonly Dictionary<String, Product> _builtInCharacterProducts = new Dictionary<string, Product>();
//         private readonly Product _lowerLetterProduct = new Product("lower_letter");
//         private readonly Product _upperLetterProduct = new Product("upper_letter");
//         private readonly Product _letterProduct = new Product("letter");
//         private readonly Product _digitProduct = new Product("digit");
//         private readonly Product _letterOrDigitProduct = new Product("letter_or_digit");
        private readonly Dictionary<String, Product> _userProducts;

        public IEnumerable<Product> Products { get { return _userProducts.Values; } }

        public Analyzer(Document document) {
            InitializeBuiltInProducts();
            _userProducts = new Dictionary<string, Product>(_builtInCharacterProducts);
            var exemplars = document.GetExemplars(_userProducts);
            Analyze(exemplars);
            CreateIsARelations(document);
        }

        private void CreateIsARelations(Document document) {
            foreach (var isASource in document.IsASources) {
                var leftProduct = _userProducts[isASource.LeftProduct];
                var rightProduct = _userProducts[isASource.RightProduct];
                var sequence = new NfaSequence(0, 1, false, rightProduct, true);
                sequence.RelationBranches[0].Add(new NfaSequence.ProductReference(leftProduct, false, 1));
                _userProducts[isASource.RightProduct].Sequences.Add(sequence);
            }
        }

        private void InitializeBuiltInProducts() {
            foreach (var codePoint in Unicode.All) {
                CreateCodePointProduct(codePoint);
            }
            _characterClassProducts.Add(new CharacterClassCharacterProduct("lower_letter", Unicode.LowercaseLetters));
            _characterClassProducts.Add(new CharacterClassCharacterProduct("upper_letter", Unicode.UppercaseLetters));
            _characterClassProducts.Add(new CharacterClassCharacterProduct("letter", Unicode.Letters));
            _characterClassProducts.Add(new CharacterClassCharacterProduct("digit", Unicode.DecimalDigitNumbers));
            _characterClassProducts.Add(new CharacterClassCharacterProduct("letter_or_digit", Unicode.AlphaNumerics));
            foreach (var characterClassProduct in _characterClassProducts) {
                _builtInCharacterProducts.Add(characterClassProduct.Title, characterClassProduct);
            }
        }

        private void CreateCodePointProduct(Int32 codePoint) {
            if (!_codePointProducts.ContainsKey(codePoint)) {
                var codePointProduct = new CodePointCharacterProduct(codePoint);
                _codePointProducts.Add(codePoint, codePointProduct);
                _builtInCharacterProducts.Add(codePointProduct.Title, codePointProduct);
            }
        }

        private static void CreateRelations(Exemplar entry) {
            int length = entry.Text.Length;
            if (length == 0) {
                length = entry.ProductSpans.Max(x => x.SpanStart + x.SpanLength);
            }
            var sequencesByStartIndex = new List<NfaSequence>[length];
            for (int init = 0; init < length; init++) {
                sequencesByStartIndex[init] = new List<NfaSequence>();
            }
            bool isExplicitCodePointSequence = entry.ProductSpans.Count == 1; //if this exemplar/relation is nothing but characters, make sure its one sequence is explicitly kept
            foreach (ProductSpan span in entry.ProductSpans) {
                var sequence = new NfaSequence(span.SpanStart,
                                               span.SpanLength,
                                               span.IsRepititious,
                                               span.Product,
                                               isExplicitCodePointSequence);
                sequencesByStartIndex[span.SpanStart].Add(sequence);
                span.Product.Sequences.Add(sequence);
            }
            var currentlyEnteredSequences = new HashSet<NfaSequence>();
            for (int startIndex = 0; startIndex < length; startIndex++) {
                foreach (NfaSequence node in sequencesByStartIndex[startIndex]) {
                    if (!(node.OwnerProduct is IBuiltInCharacterProduct)) {
                        currentlyEnteredSequences.Add(node);
                    }
                }
                var toRemove = new HashSet<NfaSequence>();
                foreach (NfaSequence node in currentlyEnteredSequences) {
                    if (node.SpanStart + node.RelationBranches.Length <= startIndex) {
                        toRemove.Add(node);
                    }
                }
                foreach (NfaSequence node in toRemove) {
                    currentlyEnteredSequences.Remove(node);
                }
                foreach (NfaSequence node in currentlyEnteredSequences) {
                    foreach (NfaSequence sequence in sequencesByStartIndex[startIndex]) {
                        int lastCharacterIndexOfSequence = sequence.SpanStart + sequence.RelationBranches.Length - 2;
                        int lastCharacterIndexOfNode = node.SpanStart + node.RelationBranches.Length - 2;
                        bool sequenceIsNestedInNode = (startIndex > node.SpanStart &&
                                                       lastCharacterIndexOfSequence <= lastCharacterIndexOfNode) ||
                                                      (lastCharacterIndexOfSequence < lastCharacterIndexOfNode
                                                      /*&& already know that startIndex >= node.SpanStart*/) ||
                                                      (sequence.OwnerProduct is IBuiltInCharacterProduct); //if not strictly nested, in this case we can assume an "is a" relationship
                        bool isTrailingRelation = (startIndex - node.SpanStart) > lastCharacterIndexOfNode;
                        if (sequenceIsNestedInNode || isTrailingRelation) {
                            node.RelationBranches[startIndex - node.SpanStart].Add(new NfaSequence.ProductReference(sequence.OwnerProduct, sequence.IsRepitious, sequence.SpanStart + sequence.SpanLength));
                        }
                    }
                }
            }
        }

        private void CreateRelations(IEnumerable<Exemplar> entries) {
            foreach (Exemplar entry in entries) {
                CreateRelations(entry);
            }
            foreach (Product product in _userProducts.Values) {

                foreach (var nfaSequence in product.Sequences) {
                    RemoveUnneededCodePointBranches(nfaSequence);
                }

                bool hasAnyNonCodePointsInSequences;
                hasAnyNonCodePointsInSequences = RemoveBranchRedundenciesAndCullEmptySequences(product, out hasAnyNonCodePointsInSequences);

                if (hasAnyNonCodePointsInSequences) {
                    RemoveUnneededCodePointSequences(product);
                }
            }
        }

        private static bool RemoveBranchRedundenciesAndCullEmptySequences(Product product, out bool hasAnyNonCodePointsInSequences) {
            hasAnyNonCodePointsInSequences = false;
            int sequenceIndex = 0;
            var sequenceIndicesToRemove = new List<int>();
            foreach (NfaSequence sequence in product.Sequences) {
                bool sequenceContainsNonCodePoints = false;
                for (int index = 0; index < sequence.RelationBranches.Length; index++) {
                    sequence.RelationBranches[index] = sequence.RelationBranches[index].Distinct().ToList();
                    sequenceContainsNonCodePoints |= sequence.RelationBranches[index].Any(x => !(x.Product is CodePointCharacterProduct));
                }
                sequence.RelationBranches[0].RemoveAll(x => x.Product == product && x.IsRepetitious == false);
                if (sequence.RelationBranches[0].Count == 0) {
                    sequenceIndicesToRemove.Add(sequenceIndex);
                } else {
                    hasAnyNonCodePointsInSequences |= sequenceContainsNonCodePoints;
                }
                sequenceIndex++;
            }
            sequenceIndicesToRemove.Reverse();
            foreach (var sequenceToRemoveIndex in sequenceIndicesToRemove) {
                product.Sequences.RemoveAt(sequenceToRemoveIndex);
            }
            return hasAnyNonCodePointsInSequences;
        }

        /// <summary>
        /// If the product has any sequences that aren't just code points, then remove all sequences that ARE just code points
        /// </summary>
        /// <param name="product"></param>
        private static void RemoveUnneededCodePointSequences(Product product) {
            var sequenceIndicesToRemove = new List<int>();
            int sequenceIndex = 0;
                //we have some sequence that contains more than just code points, so delete any sequences that ARE just code points that are not marked explicit
                foreach (NfaSequence sequence in product.Sequences) {
                    if (sequence.WasExplicitCharacterSequence) {
                        continue;
                    }
                    bool sequenceContainsNonCodePoints = false;
                    foreach (List<NfaSequence.ProductReference> t in sequence.RelationBranches) {
                        sequenceContainsNonCodePoints |= t.Any(x => !(x.Product is CodePointCharacterProduct));
                        if (sequenceContainsNonCodePoints) {
                            break;
                        }
                    }
                    if (!sequenceContainsNonCodePoints) {
                        sequenceIndicesToRemove.Add(sequenceIndex);
                    }
                    sequenceIndex++;
                }

            sequenceIndicesToRemove.Reverse();
            foreach (var sequenceToRemoveIndex in sequenceIndicesToRemove) {
                product.Sequences.RemoveAt(sequenceToRemoveIndex);
            }
        }

        private static void RemoveUnneededCodePointBranches(NfaSequence sequence) {
            var hasNonCodePointSubProductAtOffset = new bool[sequence.SpanLength];
            for (int counter = 0; counter < sequence.SpanLength; counter++) {
                foreach (var productReference in sequence.RelationBranches[counter]) {
                    if (productReference.Product is CodePointCharacterProduct) continue;
                    for (int setRange = counter; setRange < productReference.ExitSequenceCounter - sequence.SpanStart; setRange++) {
                        hasNonCodePointSubProductAtOffset[setRange] = true;
                    }
                }
            }
            for (int clearUnneeded = 0; clearUnneeded < sequence.SpanLength; clearUnneeded++) {
                if (hasNonCodePointSubProductAtOffset[clearUnneeded]) {
                    var relationBranches = sequence.RelationBranches[clearUnneeded];
                    for (int branchIndex = 0; branchIndex < relationBranches.Count;) {
                        if (relationBranches[branchIndex].Product is CodePointCharacterProduct) {
                            relationBranches.RemoveAt(branchIndex);
                        } else {
                            branchIndex++;
                        }
                    }
                }
            }
        }

        private void AddCodePointProducts(IEnumerable<Exemplar> exemplars) {
            foreach (Exemplar exemplar in exemplars) {
                TextElementEnumerator elementEnumerator = StringInfo.GetTextElementEnumerator(exemplar.Text);
                while (elementEnumerator.MoveNext()) {
                    string textElement = elementEnumerator.GetTextElement();
                    int elementLength = textElement.Length;
                    int codePoint = char.ConvertToUtf32(textElement, 0);
                    Product product = _codePointProducts[codePoint];
                    int elementPosition = elementEnumerator.ElementIndex;
                    var elementSpan = new ProductSpan(product, elementPosition, elementLength, false);
                    exemplar.ProductSpans.Add(elementSpan);
                }
            }
        }

        private void Analyze(IEnumerable<Exemplar> sourceEntries) {
            List<Exemplar> entries = sourceEntries.Select(x => (Exemplar)x.Clone()).ToList();
            AddCodePointProducts(entries);
            List<Product> products = entries.SelectMany(x => x.ProductSpans).Select(x => x.Product).ToList();
            foreach (Product product in products) {
                if (!(product is IBuiltInCharacterProduct))
                    product.Sequences.Clear();
            }
            CreateRelations(entries);
        }

        public class NfaSequence {
            public class ProductReference {
                public readonly Product Product;
                public readonly bool IsRepetitious;
                public readonly int ExitSequenceCounter;
                public ProductReference(Product product, bool isRepetitious, int exitSequenceCounter) {
                    IsRepetitious = isRepetitious;
                    Product = product;
                    ExitSequenceCounter = exitSequenceCounter;
                }

                public override string ToString() {
                    return Product.Title + (IsRepetitious ? "*" : "");
                }
            }
            public readonly List<ProductReference>[] RelationBranches;
            public readonly Product OwnerProduct;
            public readonly int SpanStart;
            public readonly bool IsRepitious;
            public readonly bool WasExplicitCharacterSequence;

            public NfaSequence(int spanStart, int spanLength, bool isRepitious, Product ownerProduct, bool wasExplicitCharacterSequence) {
                SpanStart = spanStart;
                RelationBranches = new List<ProductReference>[spanLength + 1]; //+1 to find what comes after this
                for (int initBranches = 0; initBranches < spanLength + 1; initBranches++) {
                    RelationBranches[initBranches] = new List<ProductReference>();
                }
                IsRepitious = isRepitious;
                OwnerProduct = ownerProduct;
                WasExplicitCharacterSequence = wasExplicitCharacterSequence;
            }

            public int SpanLength { get { return RelationBranches.Length - 1; } }

            public override string ToString() {
                return OwnerProduct.Title + (IsRepitious ? "*" : "");
            }
        }
    }
}