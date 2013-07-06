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
        public readonly List<CharacterClassCharacterProduct> CharacterClassProducts = new List<CharacterClassCharacterProduct>();
        public readonly Dictionary<String, Product> BuiltInCharacterProducts = new Dictionary<string, Product>();
//         private readonly Product _lowerLetterProduct = new Product("lower_letter");
//         private readonly Product _upperLetterProduct = new Product("upper_letter");
//         private readonly Product _letterProduct = new Product("letter");
//         private readonly Product _digitProduct = new Product("digit");
//         private readonly Product _letterOrDigitProduct = new Product("letter_or_digit");
        private readonly Dictionary<String, Product> UserProducts;

        public IEnumerable<Product> Products { get { return UserProducts.Values; } }

        public Analyzer() {
            InitializeBuiltInProducts();
        }

        public Analyzer(Document document) {
            InitializeBuiltInProducts();
            UserProducts = new Dictionary<string, Product>(BuiltInCharacterProducts);
            var exemplars = document.GetExemplars(UserProducts);
            Analyze(exemplars);
            CreateIsARelations(document);
        }

        private void CreateIsARelations(Document document) {
            foreach (var isASource in document.IsASources) {
                var leftProduct = UserProducts[isASource.LeftProduct];
                var rightProduct = UserProducts[isASource.RightProduct];
                var sequence = new NfaSequence(0, 1, false, rightProduct);
                sequence.RelationBranches[0].Add(new NfaSequence.ProductReference(leftProduct, false, 1));
                UserProducts[isASource.RightProduct].Sequences.Add(sequence);
            }
        }

        private void InitializeBuiltInProducts() {
            foreach (var codePoint in Unicode.All) {
                CreateCodePointProduct(codePoint);
            }
            CharacterClassProducts.Add(new CharacterClassCharacterProduct("lower_letter", Unicode.LowercaseLetters));
            CharacterClassProducts.Add(new CharacterClassCharacterProduct("upper_letter", Unicode.UppercaseLetters));
            CharacterClassProducts.Add(new CharacterClassCharacterProduct("letter", Unicode.Letters));
            CharacterClassProducts.Add(new CharacterClassCharacterProduct("digit", Unicode.DecimalDigitNumbers));
            CharacterClassProducts.Add(new CharacterClassCharacterProduct("letter_or_digit", Unicode.AlphaNumerics));
            foreach (var characterClassProduct in CharacterClassProducts) {
                BuiltInCharacterProducts.Add(characterClassProduct.Title, characterClassProduct);
            }
        }

        private void CreateCodePointProduct(Int32 codePoint) {
            if (!CodePointProducts.ContainsKey(codePoint)) {
                var codePointProduct = new CodePointCharacterProduct(codePoint);
                CodePointProducts.Add(codePoint, codePointProduct);
                BuiltInCharacterProducts.Add(codePointProduct.Title, codePointProduct);
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
            foreach (Product product in UserProducts.Values) {
                int sequenceIndex = 0;
                List<int> sequenceIndicesToRemove = new List<int>();
                foreach (NfaSequence sequence in product.Sequences) {
                    for (int index = 0; index < sequence.RelationBranches.Length; index++) {
                        sequence.RelationBranches[index] = sequence.RelationBranches[index].Distinct().ToList();
                    }
                    sequence.RelationBranches[0].RemoveAll(x => x.Product == product && x.IsRepetitious == false);
                    if (sequence.RelationBranches[0].Count == 0) {
                        sequenceIndicesToRemove.Add(sequenceIndex);
                    }
                    sequenceIndex++;
                }
                sequenceIndicesToRemove.Reverse();
                foreach (var sequenceToRemoveIndex in sequenceIndicesToRemove) {
                    product.Sequences.RemoveAt(sequenceToRemoveIndex);
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
                if (!(product is IBuiltInCharacterProduct))
                    product.Sequences.Clear();
            }
            CreateRelations(entries);
            return products;
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