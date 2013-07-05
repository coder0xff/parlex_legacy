using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace parlex {
    class Document {
        public class ExemplarSource {
            public String Text;
            public class ProductDeclaration {
                public String Name;
                public int StartPosition;
                public int Length;

                public ProductDeclaration(String name, int startPosition, int length) {
                    Name = name;
                    StartPosition = startPosition;
                    Length = length;
                }
            }

            public List<ProductDeclaration> ProductDeclarations = new List<ProductDeclaration>();
        }

        public List<ExemplarSource> ExemplarSources = new List<ExemplarSource>();

        public struct IsASource {
            public readonly String LeftProduct;
            public readonly String RightProduct;

            public IsASource(string leftProduct, string rightProduct) : this() {
                LeftProduct = leftProduct;
                RightProduct = rightProduct;
            }
        }

        public List<IsASource> IsASources = new List<IsASource>();

        public static Document FromText(String source) {
            var result = new Document();
            var lines = Regex.Split(source, "\r\n|\r|\n");
            ExemplarSource currentExemplarSource = null;
            foreach (var line in lines) {
                if (line.Trim().Length == 0) {
                    currentExemplarSource = null;
                } else {
                    if (currentExemplarSource == null) {
                        if (line.Trim() == "exemplar:") {
                        } else if (line.Trim() == "relation:") {
                            currentExemplarSource = new ExemplarSource { Text = "" };
                            result.ExemplarSources.Add(currentExemplarSource);
                        } else if (line.Contains(" is a ") || line.Contains(" is an ")) {
                            int isALength = " is a ".Length;
                            int isAIndex = line.IndexOf(" is a ");
                            if (isAIndex == -1) {
                                isALength = " is an ".Length;
                                isAIndex = line.IndexOf(" is an ");
                            }
                            string leftProduct = line.Substring(0, isAIndex).Trim();
                            string rightProduct = line.Substring(isAIndex + isALength).Trim();
                            result.IsASources.Add(new IsASource(leftProduct, rightProduct));
                        } else {
                            currentExemplarSource = new ExemplarSource { Text = line };
                            result.ExemplarSources.Add(currentExemplarSource);
                        }                
                    } else {
                        var productDeclarationParts = line.Split(':');
                        int startPosition = productDeclarationParts[0].IndexOf('|');
                        int length = productDeclarationParts[0].LastIndexOf('|') - startPosition + 1;
                        currentExemplarSource.ProductDeclarations.Add(
                            new ExemplarSource.ProductDeclaration(
                                productDeclarationParts[1].Trim(), startPosition, length
                            )
                         );
                    }
                }
            }
            return result;
        }

        internal IEnumerable<Exemplar> GetExemplars(Dictionary<string, Product> inOutProducts) {
            var results = new List<Exemplar>();
            foreach (ExemplarSource exemplarSource in ExemplarSources) {
                var result = new Exemplar(exemplarSource.Text);
                results.Add(result);
                foreach (ExemplarSource.ProductDeclaration productDeclaration in exemplarSource.ProductDeclarations) {
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
    }
}
