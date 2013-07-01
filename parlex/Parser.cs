using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parlex {
    class Parser {
        internal class SequenceMatchResult {
            internal class SubMatchInfo {
                public readonly ProductMatchResult ProductMatchResult;
                public readonly int ExitSequenceCounter;

                public SubMatchInfo(ProductMatchResult productMatchResult, int exitSequenceCounter) {
                    ProductMatchResult = productMatchResult;
                    ExitSequenceCounter = exitSequenceCounter;
                }
            }
            public readonly Dictionary<int, Dictionary<int, List<SubMatchInfo>>> SubMatches =
                new Dictionary<int, Dictionary<int, List<SubMatchInfo>>>();

            public readonly HashSet<int> ExitProductIndices = new HashSet<int>();
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
                if (product.Title != "expression") continue;
                var match = MatchProduct(product, textAsCodePointProducts, 0, textAsCodePointProducts.Length, null);
                if (match != null) result.Add(match);
            }
            return result;
        }

        ProductMatchResult MatchProduct(Product product, IList<Product> products, int index, int? requiredExitProductIndex, RecursionTestingStack leftRecursionStack) {
            if (index >= products.Count) return null;
            if (leftRecursionStack == null) leftRecursionStack = new RecursionTestingStack();
            if (leftRecursionStack.Push(product)) {
                leftRecursionStack.Pop();
                return null;
            }
            try {
                if (product == products[index]) {
                    var result = new ProductMatchResult(product);
                    result.ExitProductIndicies.Add(index + 1);
                    return result;
                } else {
                    var result = new ProductMatchResult(product);
                    bool matched = false;
                    foreach (Analyzer.NfaSequence sequence in product.Sequences) {
                        var sequenceMatch = MatchSequence(sequence, products, index, requiredExitProductIndex, leftRecursionStack);
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
                leftRecursionStack.Pop();
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

        SequenceMatchResult MatchSequence(Analyzer.NfaSequence sequence, IList<Product> products, int index, int? requiredExitProductIndex, RecursionTestingStack leftRecursionBreakers) {
            var result = new SequenceMatchResult();
            var pendingSteps = new Queue<NextStepInfo>();
            var completedSubMatches = new HashSet<NextStepInfo>();
            //if requiredExitProductIndex has a value
            //there's only two situations in which a result.subMatches[i] may have entries
            //1: when i is contained in sequenceCounterValidationChains[requiredExitProductIndex]
            //2: when i is contained in sequenceCounterValidationChains(j) where j is another instance of i (this is a recursive algorithm)
            //basically, if i can be reached by jumping back through sequenceCounters starting from requiredExitProductIndex
            //var sequenceCounterValidationChains = CreateSequenceCounterValidationChains(sequence);
            pendingSteps.Enqueue(new NextStepInfo(sequence.SpanStart, index));
            while(pendingSteps.Count > 0) {
                NextStepInfo currentStep = pendingSteps.Dequeue();
                completedSubMatches.Add(currentStep);
                var possibleBranches = sequence.RelationBranches[currentStep.StartingSequenceCounter - sequence.SpanStart];
                foreach (var branch in possibleBranches) {
                    bool leftMost = currentStep.StartingProductIndex == index;
                    var productMatch = MatchProduct(branch.Product, products, currentStep.StartingProductIndex, null, leftMost ? leftRecursionBreakers : new RecursionTestingStack());
                    if (productMatch != null) {
                        //sequenceCounterValidationChains[branch.ExitSequenceCounter - sequence.SpanStart].Add(currentStep.StartingSequenceCounter);
                        AddSequenceSubMatch(result, currentStep, productMatch, branch.ExitSequenceCounter);
                        foreach (int matchExitProductIndex in productMatch.ExitProductIndicies) {
                            if (branch.ExitSequenceCounter >= sequence.SpanStart + sequence.SpanLength) {
                                if (branch.IsRepititious) {
                                    QueueNextStep(currentStep.StartingSequenceCounter,
                                                  matchExitProductIndex,
                                                  completedSubMatches,
                                                  pendingSteps);
                                } else {
                                    if (!requiredExitProductIndex.HasValue ||
                                        matchExitProductIndex == requiredExitProductIndex.Value) {
                                        result.ExitProductIndices.Add(matchExitProductIndex);
                                    }
                                }
                            } else {
                                QueueNextStep(branch.ExitSequenceCounter,
                                              matchExitProductIndex,
                                              completedSubMatches,
                                              pendingSteps);
                                if (branch.IsRepititious) {
                                    QueueNextStep(currentStep.StartingSequenceCounter,
                                                  matchExitProductIndex,
                                                  completedSubMatches,
                                                  pendingSteps);
                                }
                            }
                        }
                    } else if (branch.IsRepititious) {
                        if (branch.ExitSequenceCounter >= sequence.SpanStart + sequence.SpanLength) {
                            if (!requiredExitProductIndex.HasValue ||
                                currentStep.StartingProductIndex == requiredExitProductIndex.Value) {
                                result.ExitProductIndices.Add(currentStep.StartingProductIndex);
                            }
                        } else {
                            QueueNextStep(branch.ExitSequenceCounter,
                                          currentStep.StartingProductIndex,
                                          completedSubMatches,
                                          pendingSteps);
                            if (branch.IsRepititious) {
//                                 it's identical to the current step, so no need to add it
//                                 QueueNextStep(currentStep.StartingSequenceCounter,
//                                               currentStep.StartingProductIndex,
//                                               completedSubMatches,
//                                               pendingSteps);
                            }
                        }
                    }
                }
            }
            if (requiredExitProductIndex.HasValue) {
                var repi = requiredExitProductIndex.Value;
                if (result.ExitProductIndices.Contains(repi)) {
                    var validationFlags = new bool?[repi][];
                    for (int initValidationFlags = 0; initValidationFlags < repi; initValidationFlags++) {
                        validationFlags[initValidationFlags] = new bool?[sequence.SpanLength];
                    }
                    foreach (int sourceProductStartIndex in result.SubMatches.Keys) {
                        bool removeSequenceCounterStart = true;
                        foreach (int sequenceCounterStart in result.SubMatches[sourceProductStartIndex].Keys) {
                            if (!ValidateSequenceSubMatch(result,
                                                          validationFlags,
                                                          sourceProductStartIndex,
                                                          sequenceCounterStart,
                                                          sequence.SpanStart,
                                                          sequence.SpanLength,
                                                          repi)) {
                                result.SubMatches[sourceProductStartIndex][sequenceCounterStart].Clear();
                            } else {
                                removeSequenceCounterStart = false;
                            }
                        }
                        if (removeSequenceCounterStart) {
                            result.SubMatches[sourceProductStartIndex].Clear();
                        }
                    }
                } else {
                    result.ExitProductIndices.Clear();
                }
            }
            return result.ExitProductIndices.Count > 0 ? result : null;
        }

        private static bool ValidateSequenceSubMatch(SequenceMatchResult sequenceMatchResult,
                                                     bool?[][] validationStates,
                                                     int sourceProductIndex,
                                                     int sequenceCounter,
                                                     int sequenceStart,
                                                     int sequenceLength,
                                                     int requiredExitSourceProductsIndex) {
            bool endOfProduct = sourceProductIndex == requiredExitSourceProductsIndex;
            bool endOfSequence = sequenceCounter == sequenceStart + sequenceLength;
            if (endOfProduct || endOfSequence) {
                return endOfProduct && endOfSequence;
            }
            if (validationStates[sourceProductIndex][sequenceCounter - sequenceStart].HasValue) {
                return validationStates[sourceProductIndex][sequenceCounter - sequenceStart].Value;
            }
            var result = false;

            foreach (var subMatch in sequenceMatchResult.SubMatches[sourceProductIndex][sequenceCounter]) {
                var nextSequenceCounter = subMatch.ExitSequenceCounter;
                foreach (int nextProductIndex in subMatch.ProductMatchResult.ExitProductIndicies) {
                    if (ValidateSequenceSubMatch(sequenceMatchResult, validationStates, nextProductIndex, nextSequenceCounter, sequenceStart, sequenceLength, requiredExitSourceProductsIndex)) {
                        result = true;
                        break;
                    }
                }
                if (result) break;
            }
            validationStates[sourceProductIndex][sequenceCounter - sequenceStart] = result;
            return result;
        }

        private static void QueueNextStep(int nextSequenceCounter,
                                          int nextProductIndex,
                                          HashSet<NextStepInfo> completedSubMatches,
                                          Queue<NextStepInfo> pendingSteps) {
            var nsi = new NextStepInfo(nextSequenceCounter,
                                       nextProductIndex);
            if (!completedSubMatches.Contains(nsi) && !pendingSteps.Contains(nsi)) {
                pendingSteps.Enqueue(nsi);
            }
        }

        private static void AddSequenceSubMatch(SequenceMatchResult result,
                                                NextStepInfo currentStep,
                                                ProductMatchResult productMatch,
                                                int nextSequenceCounter) {
            if (!result.SubMatches.ContainsKey(currentStep.StartingProductIndex)) {
                result.SubMatches[currentStep.StartingProductIndex] = new Dictionary<int, List<SequenceMatchResult.SubMatchInfo>>();
            }
            if (!result.SubMatches[currentStep.StartingProductIndex].ContainsKey(currentStep.StartingSequenceCounter)) {
                result.SubMatches[currentStep.StartingProductIndex][currentStep.StartingSequenceCounter] = new List<SequenceMatchResult.SubMatchInfo>();
            }
            result.SubMatches[currentStep.StartingProductIndex][currentStep.StartingSequenceCounter].Add(new SequenceMatchResult.SubMatchInfo(productMatch, nextSequenceCounter));
        }

//         private static HashSet<int>[] CreateSequenceCounterValidationChains(Analyzer.NfaSequence sequence) {
//             var sequenceCounterValidationChains = new HashSet<int>[sequence.SpanLength + 1];
//             for (int initValidationChains = 0; initValidationChains < sequence.SpanLength + 1; initValidationChains++) {
//                 sequenceCounterValidationChains[initValidationChains] = new HashSet<int>();
//             }
//             return sequenceCounterValidationChains;
//         }
    }
}
