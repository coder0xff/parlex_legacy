using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parlex {
    class Parser {
        internal class SequenceMatchResult {
            public readonly Dictionary<int, List<ProductMatchResult>> SubMatches;
            public readonly HashSet<int> ExitProductIndices = new HashSet<int>();

            public SequenceMatchResult() {
                SubMatches = new Dictionary<int, List<ProductMatchResult>>();
            }
        }

        internal class ProductMatchResult {
            public readonly List<SequenceMatchResult> SubMatches;
            public readonly HashSet<int> ExitProductIndicies = new HashSet<int>();
            public readonly Product Product;

            public ProductMatchResult(Product product) {
                Product = product;
                SubMatches = new List<SequenceMatchResult>();
            }
        }

        internal List<ProductMatchResult> Parse(String text, Dictionary<Int32, Product> codePointProducts, IEnumerable<Product> products) {
            var textAsCodePointProducts = text.GetUtf32CodePoints().Select(x => codePointProducts[x]).ToArray();
            var result = new List<ProductMatchResult>();
            foreach (Product product in products) {
                if (product.CodePoint.HasValue) {
                    continue;
                }
                var match = MatchProduct(product, textAsCodePointProducts, 0);
                if (match != null) result.Add(match);
            }
            return result;
        }

        ProductMatchResult MatchProduct(Product product, IList<Product> products, int index, HashSet<Product> leftRecursionBreakers = null) {
            if (index >= products.Count) return null;
            if (leftRecursionBreakers == null) leftRecursionBreakers = new HashSet<Product>();
            System.Diagnostics.Debug.Assert(leftRecursionBreakers.Add(product));
            try {
                if (product == products[index]) {
                    var result = new ProductMatchResult(product);
                    result.ExitProductIndicies.Add(index + 1);
                    return result;
                } else {
                    var result = new ProductMatchResult(product);
                    bool matched = false;
                    foreach (Analyzer.NfaSequence sequence in product.Sequences) {
                        var sequenceMatch = MatchSequence(sequence, products, index, leftRecursionBreakers);
                        if (sequenceMatch != null) {
                            matched = true;
                            result.SubMatches.Add(sequenceMatch);
                            foreach (int sequenceMatchExitProductIndex in sequenceMatch.ExitProductIndices) {
                                result.ExitProductIndicies.Add(sequenceMatchExitProductIndex);
                            }
                        }
                    }
                    return matched ? result : null;
                }
            } finally {
                leftRecursionBreakers.Remove(product);
            }
        }

        struct NextStepInfo {


            public readonly int StartingSequenceCounter;
            public readonly int StartingProductIndex;

            public NextStepInfo(int startingSequenceCounter, int startingProductIndex) : this() {
                StartingSequenceCounter = startingSequenceCounter;
                StartingProductIndex = startingProductIndex;
            }
        }

        SequenceMatchResult MatchSequence(Analyzer.NfaSequence sequence, IList<Product> products, int index, HashSet<Product> leftRecursionBreakers) {
            var result = new SequenceMatchResult();
            var pendingSteps = new Queue<NextStepInfo>();
            var completedSubMatches = new HashSet<NextStepInfo>();
            pendingSteps.Enqueue(new NextStepInfo(sequence.SpanStart, index));
            while(pendingSteps.Count > 0) {
                NextStepInfo currentStep = pendingSteps.Dequeue();
                completedSubMatches.Add(currentStep);
                var possibleBranches = sequence.RelationBranches[currentStep.StartingSequenceCounter - sequence.SpanStart];
                foreach (var branch in possibleBranches) {
                    bool leftMost = currentStep.StartingProductIndex == 0;
                    if (leftMost && (branch.Product == sequence.OwnerProduct || leftRecursionBreakers.Contains(branch.Product))) {
                        continue; //break out of left recursion
                    }
                    var productMatch = MatchProduct(branch.Product, products, currentStep.StartingProductIndex, leftMost ? leftRecursionBreakers : new HashSet<Product>());
                    if (productMatch != null) {
                        if (!result.SubMatches.ContainsKey(currentStep.StartingProductIndex)) {
                            result.SubMatches[currentStep.StartingProductIndex] = new List<ProductMatchResult>();
                        }
                        result.SubMatches[currentStep.StartingProductIndex].Add(productMatch);
                        foreach (int matchExitProductIndex in productMatch.ExitProductIndicies) {
                            if (branch.ExitSequenceCounter >= sequence.SpanStart + sequence.SpanLength) {
                                if (branch.IsRepititious) {
                                    var nsi = new NextStepInfo(currentStep.StartingSequenceCounter,
                                                               matchExitProductIndex);
                                    if (!completedSubMatches.Contains(nsi) && !pendingSteps.Contains(nsi)) {
                                        pendingSteps.Enqueue(nsi);
                                    }
                                } else {
                                    result.ExitProductIndices.Add(matchExitProductIndex);
                                }
                            } else {
                                var nsi = new NextStepInfo(branch.ExitSequenceCounter,
                                                           matchExitProductIndex);
                                if (!completedSubMatches.Contains(nsi) && !pendingSteps.Contains(nsi)) {
                                    pendingSteps.Enqueue(nsi);
                                }
                                if (branch.IsRepititious) {
                                    nsi = new NextStepInfo(currentStep.StartingSequenceCounter,
                                                           matchExitProductIndex);
                                    if (!completedSubMatches.Contains(nsi) && !pendingSteps.Contains(nsi)) {
                                        pendingSteps.Enqueue(nsi);
                                    }
                                    
                                }
                            }
                        }
                    } else if (branch.IsRepititious) {
                        if (branch.ExitSequenceCounter >= sequence.SpanStart + sequence.SpanLength) {
                            result.ExitProductIndices.Add(currentStep.StartingProductIndex);
                        } else {
                            var nsi = new NextStepInfo(branch.ExitSequenceCounter,
                                                       currentStep.StartingProductIndex);
                            if (!completedSubMatches.Contains(nsi) && !pendingSteps.Contains(nsi)) {
                                pendingSteps.Enqueue(nsi);
                            }
                            if (branch.IsRepititious) {
                                nsi = new NextStepInfo(currentStep.StartingSequenceCounter,
                                                       currentStep.StartingProductIndex);
                                if (!completedSubMatches.Contains(nsi) && !pendingSteps.Contains(nsi)) {
                                    pendingSteps.Enqueue(nsi);
                                }
                            }
                        }
                    }
                }
            }
            return result.ExitProductIndices.Count > 0 ? result : null;
        }

    }
}
