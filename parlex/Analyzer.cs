using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace parlex {
    internal class Analyzer {
        //The NFA subset created for a particular example of a particular product.
        //While the sub-products it utilizes will be NFAs all their own, this one follows
        //a particular sequence, and each element must be satisfied

        private readonly Dictionary<Int32, Product> _codePointProducts = new Dictionary<Int32, Product>();
        private readonly Product _lowerLetterProduct = new Product("lower_letter");
        private readonly Product _upperLetterProduct = new Product("upper_letter");
        private readonly Product _letterProduct = new Product("letter");
        private readonly Product _digitProduct = new Product("digit");
        private readonly Product _letterOrDigitProduct = new Product("letter_or_digit");
        public readonly Dictionary<String, Product> AllProducts = new Dictionary<string, Product>();
        public Analyzer(Document document) {
            foreach (var codePoint in Unicode.All) {
                CreateCodePointProduct(codePoint);
            }
            GenerateCharacterCategoryProduct(_lowerLetterProduct, Unicode.LowercaseLetter);
            GenerateCharacterCategoryProduct(_upperLetterProduct, Unicode.UppercaseLetter);
            GenerateCharacterCategoryProduct(_letterProduct, Unicode.Letters);
            GenerateCharacterCategoryProduct(_digitProduct, Unicode.DecimalDigitNumber);
            GenerateCharacterCategoryProduct(_letterOrDigitProduct, Unicode.AlphaNumeric);
            var allProducts = new List<Product>(_codePointProducts.Values) {
                _lowerLetterProduct,
                _upperLetterProduct,
                _letterProduct,
                _digitProduct,
                _letterOrDigitProduct
            };
            foreach (Product product in allProducts) {
                AllProducts.Add(product.Title, product);
            }
            var exemplars = document.GetExemplars(AllProducts);
            Analyze(exemplars);
        }

        private void GenerateCharacterCategoryProduct(Product product, IEnumerable<int> codePoints) {
            product.Sequences.AddRange(codePoints.Select(x => new NfaSequence(0, 1, false, _codePointProducts[x])));
        }

        private void CreateCodePointProduct(Int32 codePoint) {
            if (!_codePointProducts.ContainsKey(codePoint)) {
                _codePointProducts.Add(codePoint, new Product("codePoint" + codePoint.ToString("X6")));
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
            foreach (ProductSpan span in entry.ProductSpans) {
                var sequence = new NfaSequence(span.SpanStart,
                                               span.SpanLength,
                                               span.IsRepititious,
                                               span.Product);
                sequencesByStartIndex[span.SpanStart].Add(sequence);
                span.Product.Sequences.Add(sequence);
            }
            var currentlyEnteredSequences = new HashSet<NfaSequence>();
            for (int startIndex = 0; startIndex < length; startIndex++) {
                foreach (NfaSequence node in sequencesByStartIndex[startIndex]) {
                    currentlyEnteredSequences.Add(node);
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
                        bool sequenceIsNestedInNode = lastCharacterIndexOfNode >= lastCharacterIndexOfSequence;
                        bool isTrailingRelation = (startIndex - node.SpanStart) > lastCharacterIndexOfNode;
                        if (sequenceIsNestedInNode || isTrailingRelation) {
                            node.RelationBranches[startIndex - node.SpanStart].Add(sequence.OwnerProduct);
                        }
                    }
                }
            }
        }

        private static void CreateRelations(IEnumerable<Exemplar> entries) {
            foreach (Exemplar entry in entries) {
                CreateRelations(entry);
            }
            //TODO Remove redundant entries from NfaSequence.RelationBranches
        }

        private void AddCodePointProducts(IEnumerable<Exemplar> exemplars) {
            foreach (Exemplar exemplar in exemplars) {
                TextElementEnumerator elementEnumerator = StringInfo.GetTextElementEnumerator(exemplar.Text);
                while(elementEnumerator.MoveNext()) {
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

        internal IEnumerable<Product> Analyze(IEnumerable<Exemplar> sourceEntries) {
            List<Exemplar> entries = sourceEntries.Select(x => (Exemplar) x.Clone()).ToList();
            AddCodePointProducts(entries);
            List<Product> products = entries.SelectMany(x => x.ProductSpans).Select(x => x.Product).ToList();
            foreach (Product product in products) {
                product.Sequences.Clear();
            }
            CreateRelations(entries);
            return products;
        }

        public class NfaSequence {
            public readonly List<Product>[] RelationBranches;
            public Product OwnerProduct;
            public int SpanStart;
            public bool IsRepitious;

            public NfaSequence(int spanStart, int spanLength, bool isRepitious, Product ownerProduct) {
                SpanStart = spanStart;
                RelationBranches = new List<Product>[spanLength + 1]; //+1 to find what comes after this
                for (int initBranches = 0; initBranches < spanLength + 1; initBranches++) {
                    RelationBranches[initBranches] = new List<Product>();
                }
                IsRepitious = isRepitious;
                OwnerProduct = ownerProduct;
            }
        }
    }
}