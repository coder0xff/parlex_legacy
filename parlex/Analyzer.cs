using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace parlex {
    internal class Analyzer {
        //The NFA subset created for a particular example of a particular product.
        //While the sub-products it utilizes will be NFAs all their own, this one follows
        //a particular sequence, and each element must be satisfied

        private readonly Dictionary<Int32, Product> _builtInProducts = new Dictionary<Int32, Product>();
        //         private Product lower_letter_product = new Product("lower_letter");
        //         private Product upper_letter_product = new Product("upper_letter");
        //         private Product letter_product = new Product("letter");
        //         private Product digit_product = new Product("digit");
        //         private Product letter_or_digit_product = new Product("letter_or_digit");

        /// <summary>
        ///     We only want to make single-character products for characters we actually see
        ///     since, with Unicode support, maybe an exhaustive table would be foolish
        /// </summary>
        /// <param name="text"></param>
        private void CreateBuiltInProducts(String text) {
            int[] codePoints = text.GetUtf32CodePoints();
            foreach (Int32 codePoint in codePoints) {
                if (!_builtInProducts.ContainsKey(codePoint)) {
                    _builtInProducts.Add(codePoint, new Product("codePoint" + codePoint.ToString("X6")));
                }
            }
        }

        private static void CreateRelations(Exemplar entry) {
            int length = entry.Text.Length;
            var sequencesByStartIndex = new List<NfaSequence>[length];
            for (int init = 0; init < length; init++) {
                sequencesByStartIndex[init] = new List<NfaSequence>();
            }
            foreach (ProductSpan span in entry.ProductSpans) {
                var sequence = new NfaSequence(span.SpanStart,
                                               span.SpanLength,
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

        private void CreateRelations(IEnumerable<Exemplar> entries) {
            foreach (Exemplar entry in entries) {
                CreateRelations(entry);
            }
        }

        private void AddBuiltInProducts(IEnumerable<Exemplar> exemplars) {
            foreach (Exemplar exemplar in exemplars) {
                CreateBuiltInProducts(exemplar.Text);
                TextElementEnumerator elementEnumerator = StringInfo.GetTextElementEnumerator(exemplar.Text);
                while(elementEnumerator.MoveNext()) {
                    string textElement = elementEnumerator.GetTextElement();
                    int elementLength = textElement.Length;
                    int codePoint = char.ConvertToUtf32(textElement, 0);
                    Product product = _builtInProducts[codePoint];
                    int elementPosition = elementEnumerator.ElementIndex;
                    var elementSpan = new ProductSpan(product, elementPosition, elementLength);
                    exemplar.ProductSpans.Add(elementSpan);
                }
            }
        }

        internal IEnumerable<Product> Analyze(IEnumerable<Exemplar> sourceEntries) {
            List<Exemplar> entries = sourceEntries.Select(x => (Exemplar) x.Clone()).ToList();
            AddBuiltInProducts(entries);
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

            public NfaSequence(int spanStart, int spanLength, Product ownerProduct) {
                SpanStart = spanStart;
                RelationBranches = new List<Product>[spanLength + 1]; //+1 to find what comes after this
                for (int initBranches = 0; initBranches < spanLength + 1; initBranches++) {
                    RelationBranches[initBranches] = new List<Product>();
                }
                OwnerProduct = ownerProduct;
            }
        }
    }
}