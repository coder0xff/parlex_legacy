using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Common;

namespace parlex {
    public class CompiledGrammar : IReadOnlyDictionary<string, Product> {
        private readonly Dictionary<Int32, Product> _codePointProducts = new Dictionary<Int32, Product>();
        private readonly List<CharacterClassCharacterProduct> _characterClassProducts = new List<CharacterClassCharacterProduct>();
        internal readonly Dictionary<String, Product> AllProducts;
        internal readonly StrictPartialOrder<Product> _precedences;

        public Dictionary<String, Product> GetAllProducts() {
            return AllProducts.Values.ToDictionary(product => product.Title);
        }

        public IEnumerable<Product> NonCharacterProducts {
            get {
                return AllProducts.Values.Where(product => !(product is ICharacterProduct));
            }
        }

        public CompiledGrammar(GrammarDocument document) {
            InitializeBuiltInProducts();
            CreateCustomCharacterSets(document);
            AllProducts = _codePointProducts.ToDictionary(x => x.Value.Title, x=>x.Value);
            foreach (var product in _characterClassProducts) {
                AllProducts.Add(product.Title, product);
            }
            var exemplars = GetExemplars(document.ExemplarSources, AllProducts);
            Analyze(exemplars);
            CreateIsARelations(document);
            _precedences = CreatePrecedesEdges(document);
        }

        private void CreateCustomCharacterSets(GrammarDocument document) {
            foreach (var characterSetSource in document.CharacterSetSources) {
                switch (characterSetSource.Type) {
                    case GrammarDocument.CharacterSetEntry.Types.List:
                        var items = new List<int>();
                        foreach (var source in characterSetSource.Params.Skip(1)) {
                            if (source.Length == "codePoint".Length + 6 && source.StartsWith("codePoint")) {
                                try {
                                    items.Add(Convert.ToInt32(source.Substring("codePoint".Length), 16));
                                } catch (FormatException) {
                                    items.AddRange(source.GetUtf32CodePoints());
                                }
                            } else {
                                items.AddRange(source.GetUtf32CodePoints());
                            }
                        }
                        _characterClassProducts.Add(new CharacterClassCharacterProduct(characterSetSource.Params[0], items, characterSetSource));
                        break;
                    case GrammarDocument.CharacterSetEntry.Types.Inversion: {
                        var source = _characterClassProducts.Find(x => x.Title == characterSetSource.Params[1]);
                        _characterClassProducts.Add(new CharacterClassCharacterProduct(characterSetSource.Params[0], Unicode.All.Except(source.CodePoints), characterSetSource));
                    }
                        break;
                    case GrammarDocument.CharacterSetEntry.Types.Union: {
                        IEnumerable<int> current = new int[0];
                        foreach (var param in characterSetSource.Params.Skip(1)) {
                            var source = _characterClassProducts.Find(x => x.Title == param);
                            current = current.Union(source.CodePoints);
                        }
                        _characterClassProducts.Add(new CharacterClassCharacterProduct(characterSetSource.Params[0], current, characterSetSource));
                    }
                        break;
                    case GrammarDocument.CharacterSetEntry.Types.Intersection: {
                        IEnumerable<int> current = Unicode.All;
                        foreach (var param in characterSetSource.Params.Skip(1)) {
                            var source = _characterClassProducts.Find(x => x.Title == param);
                            current = current.Intersect(source.CodePoints);
                        }
                        _characterClassProducts.Add(new CharacterClassCharacterProduct(characterSetSource.Params[0], current, characterSetSource));
                    }
                        break;
                }
            }
        }

        private static IEnumerable<Exemplar> GetExemplars(IEnumerable<GrammarDocument.ExemplarSource> exemplarSources, Dictionary<string, Product> inOutProducts) {
            var results = new List<Exemplar>();
            foreach (GrammarDocument.ExemplarSource exemplarSource in exemplarSources) {
                var result = new Exemplar(exemplarSource.Text);
                results.Add(result);
                foreach (GrammarDocument.ProductSpanSource productDeclaration in exemplarSource) {
                    bool isRepititious = productDeclaration.Name.EndsWith("*");
                    string properName = productDeclaration.Name.Replace("*", "");
                    if (!inOutProducts.ContainsKey(properName)) {
                        inOutProducts.Add(properName, new Product(properName));
                    }
                    result.ProductSpans.Add(new ProductSpan(
                        inOutProducts[properName],
                        productDeclaration.StartPosition,
                        productDeclaration.Length,
                        isRepititious)
                    );
                }
            }
            return results;
        }
        private void CreateIsARelations(GrammarDocument document) {
            foreach (var isASource in document.IsASources) {
                if (!AllProducts.ContainsKey(isASource.LeftProduct)) {
                    AllProducts[isASource.LeftProduct] = new Product(isASource.LeftProduct);
                }
                var leftProduct = AllProducts[isASource.LeftProduct];
                if (!AllProducts.ContainsKey(isASource.RightProduct)) {
                    AllProducts[isASource.RightProduct] = new Product(isASource.RightProduct);
                }
                var rightProduct = AllProducts[isASource.RightProduct];
                var sequence = new NfaSequence(0, 1, false, rightProduct, true);
                sequence.RelationBranches[0].Add(new NfaSequence.ProductReference(leftProduct, false, 1));
                AllProducts[isASource.RightProduct].Sequences.Add(sequence);
            }
        }

        private StrictPartialOrder<Product> CreatePrecedesEdges(GrammarDocument document) {
            var edges = document.PrecedesSources.Select(x => new StrictPartialOrder<Product>.Edge(AllProducts[x.LeftProduct], AllProducts[x.RightProduct]));
            return new StrictPartialOrder<Product>(edges);
        }

        private void InitializeBuiltInProducts() {
            foreach (var codePoint in Unicode.All) {
                CreateCodePointProduct(codePoint);
            }
            _characterClassProducts.Add(new CharacterClassCharacterProduct("lower_letter", Unicode.LowercaseLetters, default(GrammarDocument.CharacterSetEntry)));
            _characterClassProducts.Add(new CharacterClassCharacterProduct("upper_letter", Unicode.UppercaseLetters, default(GrammarDocument.CharacterSetEntry)));
            _characterClassProducts.Add(new CharacterClassCharacterProduct("letter", Unicode.Letters, default(GrammarDocument.CharacterSetEntry)));
            _characterClassProducts.Add(new CharacterClassCharacterProduct("digit", Unicode.DecimalDigitNumbers, default(GrammarDocument.CharacterSetEntry)));
            _characterClassProducts.Add(new CharacterClassCharacterProduct("letter_or_digit", Unicode.AlphaNumerics, default(GrammarDocument.CharacterSetEntry)));
            _characterClassProducts.Add(new CharacterClassCharacterProduct("white_space", Unicode.WhiteSpace, default(GrammarDocument.CharacterSetEntry)));
        }

        static public Dictionary<String, Product> GetBuiltInProducts() {
            return new CompiledGrammar(new GrammarDocument()).GetAllProducts();
        } 

        private void CreateCodePointProduct(Int32 codePoint) {
            if (!_codePointProducts.ContainsKey(codePoint)) {
                var codePointProduct = new CodePointCharacterProduct(codePoint);
                _codePointProducts.Add(codePoint, codePointProduct);
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
                    if (!(node.OwnerProduct is ICharacterProduct)) {
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
                        int lastCharacterIndexOfSequence = sequence.SpanStart + sequence.RelationBranches.Length - 1;
                        int lastCharacterIndexOfNode = node.SpanStart + node.RelationBranches.Length - 1;
                        bool sequenceIsNestedInNode = (startIndex > node.SpanStart &&
                                                       lastCharacterIndexOfSequence <= lastCharacterIndexOfNode) ||
                                                      (lastCharacterIndexOfSequence < lastCharacterIndexOfNode
                                                      /*&& already know that startIndex >= node.SpanStart*/) ||
                                                      (sequence.OwnerProduct is ICharacterProduct); //if not strictly nested, in this case we can assume an "is a" relationship
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

                foreach (var nfaSequence in product.Sequences) {
                    RemoveUnneededCodePointBranches(nfaSequence);
                }

                bool hasAnyNonCodePointsInSequences = RemoveBranchRedundenciesAndCullEmptySequences(product);

                if (hasAnyNonCodePointsInSequences) {
                    RemoveUnneededCodePointSequences(product);
                }
            }
        }

        private static bool RemoveBranchRedundenciesAndCullEmptySequences(Product product) {
            bool hasAnyNonCodePointsInSequences = false;
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
                if (!(product is ICharacterProduct))
                    product.Sequences.Clear();
            }
            CreateRelations(entries);
        }

        public class NfaSequence {
            public class ProductReference {
                public readonly Product Product;
                public readonly bool IsRepetitious;
                public readonly int ExitSequenceCounter;

                internal ProductReference(Product product, bool isRepetitious, int exitSequenceCounter) {
                    IsRepetitious = isRepetitious;
                    Product = product;
                    ExitSequenceCounter = exitSequenceCounter;
                }

                public override string ToString() {
                    return Product.Title + (IsRepetitious ? "*" : "");
                }
            }

            public readonly List<ProductReference>[] RelationBranches;
            internal readonly Product OwnerProduct;
            public readonly int SpanStart;
            internal readonly bool IsRepitious;
            internal readonly bool WasExplicitCharacterSequence;

            internal NfaSequence(int spanStart, int spanLength, bool isRepitious, Product ownerProduct, bool wasExplicitCharacterSequence) {
                SpanStart = spanStart;
                RelationBranches = new List<ProductReference>[spanLength]; //+1 to find what comes after this
                for (int initBranches = 0; initBranches < spanLength; initBranches++) {
                    RelationBranches[initBranches] = new List<ProductReference>();
                }
                IsRepitious = isRepitious;
                OwnerProduct = ownerProduct;
                WasExplicitCharacterSequence = wasExplicitCharacterSequence;
            }

            internal int SpanLength { get { return RelationBranches.Length; } }

            public override string ToString() {
                return OwnerProduct.Title + (IsRepitious ? "*" : "");
            }

            public string GetExample() {
                var result = new StringBuilder();
                for (var sequenceCounter = 0; sequenceCounter < RelationBranches.Length; ) {
                    var untriedProductReferences = new HashSet<ProductReference>(RelationBranches[sequenceCounter]);
                    bool success = false;
                    while(untriedProductReferences.Count > 0) {
                        var productReference = untriedProductReferences.OrderBy(x => Rng.Next()).First(); //get a random branch
                        untriedProductReferences.Remove(productReference);
                        var repetitionCount = 1;
                        if (productReference.IsRepetitious) {
                            repetitionCount = Rng.Next(1, 4);
                        }
                        for (var repitition = 0; repitition < repetitionCount; repitition++) {
                            var productString = productReference.Product.GetExample();
                            if (productString != null) {
                                result.Append(productString);
                                success = true;
                                sequenceCounter = productReference.ExitSequenceCounter - SpanStart;
                                break;
                            }
                        }
                        if (success) break;
                    }
                    if (!success) return null;
                }
                return result.ToString();
            }
        }

        bool IReadOnlyDictionary<string, Product>.ContainsKey(string key) {
            return AllProducts.ContainsKey(key);
        }

        IEnumerable<string> IReadOnlyDictionary<string, Product>.Keys {
            get { return AllProducts.Keys; }
        }

        bool IReadOnlyDictionary<string, Product>.TryGetValue(string key, out Product value) {
            return AllProducts.TryGetValue(key, out value);
        }

        IEnumerable<Product> IReadOnlyDictionary<string, Product>.Values {
            get { return AllProducts.Values; }
        }

        Product IReadOnlyDictionary<string, Product>.this[string key] {
            get { return AllProducts[key]; }
        }

        int IReadOnlyCollection<KeyValuePair<string, Product>>.Count {
            get { return AllProducts.Count; }
        }

        public IEnumerable<Tuple<Product, Product>> Precedences {
            get { return _precedences.Select(edge => new Tuple<Product, Product>(edge.From, edge.To)); }
        }

        IEnumerator<KeyValuePair<string, Product>> IEnumerable<KeyValuePair<string, Product>>.GetEnumerator() {
            return AllProducts.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return AllProducts.GetEnumerator();
        }

        public static bool IsBuiltInProductName(string productName) {
            if (productName.Length == "codePoint000000".Length && productName.StartsWith("codePoint")) {
                Int32 codePoint;
                if (Int32.TryParse(productName.Substring("codePoint".Length), out codePoint)) {
                    if (Unicode.All.Contains(codePoint)) {
                        return true;
                    }
                }
            }
            return new[] {"lower_letter", "upper_letter", "letter", "digit", "letter_or_digit", "white_space"}.Contains(productName);
        }
    }
}