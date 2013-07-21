using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace parlex {
    class GrammarDocument {
        private class ExemplarSource {
            internal String Text;

            internal class ProductDeclaration {
                internal readonly String Name;
                internal readonly int StartPosition;
                internal readonly int Length;

                internal ProductDeclaration(String name, int startPosition, int length) {
                    Name = name;
                    StartPosition = startPosition;
                    Length = length;
                }
            }

            internal readonly List<ProductDeclaration> ProductDeclarations = new List<ProductDeclaration>();
        }

        private readonly List<ExemplarSource> _exemplarSources = new List<ExemplarSource>();

        internal struct IsASource {
            internal readonly String LeftProduct;
            internal readonly String RightProduct;

            internal IsASource(string leftProduct, string rightProduct) : this() {
                LeftProduct = leftProduct;
                RightProduct = rightProduct;
            }
        }

        internal readonly List<IsASource> IsASources = new List<IsASource>();
        internal readonly List<StrictPartialOrder<String>.Edge> PrecedesSources = new List<StrictPartialOrder<String>.Edge>();

        internal struct CharacterSetEntry {
            internal readonly string[] Params;

            internal enum Types {
                List,
                Inversion,
                Union,
                Intersection
            }

            internal readonly Types Type;

            internal CharacterSetEntry(string[] @params, Types type) : this() {
                Params = @params;
                Type = type;
            }
        }

        internal readonly List<CharacterSetEntry> CharacterSetSources = new List<CharacterSetEntry>();

        private static GrammarDocument FromText(String source) {
            var result = new GrammarDocument();
            var lines = Regex.Split(source, "\r\n|\r|\n");
            ExemplarSource currentExemplarSource = null;
            bool nextLineStartsExemplar = false;
            for (int index = 0; index < lines.Length; index++) {
                var line = lines[index];
                if (line.Trim().Length == 0) {
                    if (nextLineStartsExemplar) {
                        throw new FormatException("'Exemplar:' must be followed by a non-empty line");
                    }
                    currentExemplarSource = null;
                } else {
                    if (currentExemplarSource == null) {
                        if (nextLineStartsExemplar) {
                            currentExemplarSource = new ExemplarSource {Text = line};
                            result._exemplarSources.Add(currentExemplarSource);
                            nextLineStartsExemplar = false;
                        } else if (line.Trim() == "exemplar:") {
                            nextLineStartsExemplar = true;
                        } else if (line.Trim() == "relation:") {
                            currentExemplarSource = new ExemplarSource {Text = ""};
                            result._exemplarSources.Add(currentExemplarSource);
                        } else if (line.Contains(" is a ") || line.Contains(" is an ")) {
                            int isALength = " is a ".Length;
                            int isAIndex = line.IndexOf(" is a ", StringComparison.Ordinal);
                            if (isAIndex == -1) {
                                isALength = " is an ".Length;
                                isAIndex = line.IndexOf(" is an ", StringComparison.Ordinal);
                            }
                            string leftProduct = line.Substring(0, isAIndex).Trim();
                            string rightProduct = line.Substring(isAIndex + isALength).Trim();
                            result.IsASources.Add(new IsASource(leftProduct, rightProduct));
                        } else if (line.Contains(" precedes ")) {
                            int precedesLength = " precedes ".Length;
                            int precedesIndex = line.IndexOf(" precedes ", StringComparison.Ordinal);
                            string leftProduct = line.Substring(0, precedesIndex).Trim();
                            string rightProduct = line.Substring(precedesIndex + precedesLength).Trim();
                            result.PrecedesSources.Add(new StrictPartialOrder<String>.Edge(leftProduct, rightProduct));
                        } else if (line.Trim().StartsWith("character set:")) {
                            var names = line.Trim().Substring("character set:".Length).Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                            result.CharacterSetSources.Add(new CharacterSetEntry(names, CharacterSetEntry.Types.List));
                        } else if (line.Trim().StartsWith("character set inverted:")) {
                            var names = line.Trim().Substring("character set inverted:".Length).Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                            result.CharacterSetSources.Add(new CharacterSetEntry(names, CharacterSetEntry.Types.Inversion));
                        } else if (line.Trim().StartsWith("character set union:")) {
                            var names = line.Trim().Substring("character set union:".Length).Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                            result.CharacterSetSources.Add(new CharacterSetEntry(names, CharacterSetEntry.Types.Union));
                        } else if (line.Trim().StartsWith("character set intersection:")) {
                            var names = line.Trim().Substring("character set intersection:".Length).Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                            result.CharacterSetSources.Add(new CharacterSetEntry(names, CharacterSetEntry.Types.Intersection));
                        }
                    } else {
                        var productDeclarationParts = line.Split(':');
                        int startPosition = productDeclarationParts[0].IndexOf('|');
                        int length = productDeclarationParts[0].LastIndexOf('|') - startPosition + 1;
                        currentExemplarSource.ProductDeclarations.Add(new ExemplarSource.ProductDeclaration(productDeclarationParts[1].Trim(), startPosition, length));
                    }
                }
            }
            return result;
        }

        internal IEnumerable<Exemplar> GetExemplars(Dictionary<string, Product> inOutProducts) {
            var results = new List<Exemplar>();
            foreach (ExemplarSource exemplarSource in _exemplarSources) {
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
