using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace parlex {
    internal class Analyzer {
        //The NFA subset created for a particular example of a particular product.
        //While the sub-products it utilizes will be NFAs all their own, this one follows
        //a particular sequence, and each element must be satisfied

        public readonly Dictionary<Int32, Product> CodePointProducts = new Dictionary<Int32, Product>();
        private readonly Product _lowerLetterProduct = new Product("lower_letter");
        private readonly Product _upperLetterProduct = new Product("upper_letter");
        private readonly Product _letterProduct = new Product("letter");
        private readonly Product _digitProduct = new Product("digit");
        private readonly Product _letterOrDigitProduct = new Product("letter_or_digit");
        public readonly Dictionary<String, Product> AllProducts = new Dictionary<string, Product>();

        public Analyzer() {
            InitializeBuiltInProducts();
        }

        public Analyzer(Document document) {
            InitializeBuiltInProducts();
            var exemplars = document.GetExemplars(AllProducts);
            Analyze(exemplars);
        }

        private void InitializeBuiltInProducts() {
            foreach (var codePoint in Unicode.All) {
                CreateCodePointProduct(codePoint);
            }
            GenerateCharacterCategoryProduct(_lowerLetterProduct, Unicode.LowercaseLetter);
            GenerateCharacterCategoryProduct(_upperLetterProduct, Unicode.UppercaseLetter);
            GenerateCharacterCategoryProduct(_letterProduct, Unicode.Letters);
            GenerateCharacterCategoryProduct(_digitProduct, Unicode.DecimalDigitNumber);
            GenerateCharacterCategoryProduct(_letterOrDigitProduct, Unicode.AlphaNumeric);
            var allProducts = new List<Product>(CodePointProducts.Values) {
                _lowerLetterProduct,
                _upperLetterProduct,
                _letterProduct,
                _digitProduct,
                _letterOrDigitProduct
            };
            foreach (Product product in allProducts) {
                product.IsBuiltIn = true;
                AllProducts.Add(product.Title, product);
            }
        }

        private void GenerateCharacterCategoryProduct(Product product, IEnumerable<int> codePoints) {
            foreach (int codePoint in codePoints) {
                var sequence = new NfaSequence(0, 1, false, product);
                sequence.RelationBranches[0].Add(new NfaSequence.ProductReference(CodePointProducts[codePoint], false, 1));
                product.Sequences.Add(sequence);
            }
        }

        private void CreateCodePointProduct(Int32 codePoint) {
            if (!CodePointProducts.ContainsKey(codePoint)) {
                CodePointProducts.Add(codePoint, new Product("codePoint" + codePoint.ToString("X6"), codePoint));
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
                    if (!node.OwnerProduct.IsBuiltIn) {
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
                                                      sequence.OwnerProduct.IsBuiltIn; //it's not strictly nested, but in this case, we know which is which 
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
            foreach (Product product in AllProducts.Values) {
                foreach (NfaSequence sequence in product.Sequences) {
                    for (int index = 0; index < sequence.RelationBranches.Length; index++) {
                        sequence.RelationBranches[index] = sequence.RelationBranches[index].Distinct().ToList();
                    }
                    sequence.RelationBranches[0].RemoveAll(x => x.Product == product && x.IsRepititious == false);
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
                    Product product = CodePointProducts[codePoint];
                    int elementPosition = elementEnumerator.ElementIndex;
                    var elementSpan = new ProductSpan(product, elementPosition, elementLength, false);
                    exemplar.ProductSpans.Add(elementSpan);
                }
            }
        }

        internal IEnumerable<Product> Analyze(IEnumerable<Exemplar> sourceEntries) {
            List<Exemplar> entries = sourceEntries.Select(x => (Exemplar)x.Clone()).ToList();
            AddCodePointProducts(entries);
            List<Product> products = entries.SelectMany(x => x.ProductSpans).Select(x => x.Product).ToList();
            foreach (Product product in products) {
                if (!product.IsBuiltIn)
                    product.Sequences.Clear();
            }
            CreateRelations(entries);
            return products;
        }

        public class NfaSequence {
            public struct ProductReference {
                public readonly Product Product;
                public readonly bool IsRepititious;
                public readonly int ExitSequenceCounter;
                public ProductReference(Product product, bool isRepititious, int exitSequenceCounter) {
                    IsRepititious = isRepititious;
                    Product = product;
                    ExitSequenceCounter = exitSequenceCounter;
                }
            }
            public readonly List<ProductReference>[] RelationBranches;
            public readonly Product OwnerProduct;
            public readonly int SpanStart;
            public readonly bool IsRepitious;

            public NfaSequence(int spanStart, int spanLength, bool isRepitious, Product ownerProduct) {
                SpanStart = spanStart;
                RelationBranches = new List<ProductReference>[spanLength + 1]; //+1 to find what comes after this
                for (int initBranches = 0; initBranches < spanLength + 1; initBranches++) {
                    RelationBranches[initBranches] = new List<ProductReference>();
                }
                IsRepitious = isRepitious;
                OwnerProduct = ownerProduct;
            }

            public int SpanLength { get { return RelationBranches.Length - 1; } }
        }
    }
}