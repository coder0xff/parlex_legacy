using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace parlex {
    public class GrammarDocument {
        public class ExemplarSource {
            public String Text;

            internal class ProductSpanSource {
                public String Name;
                public int StartPosition;
                public int Length;

                public ProductSpanSource(String name, int startPosition, int length) {
                    Name = name;
                    StartPosition = startPosition;
                    Length = length;
                }

                public ProductSpanSource() {}

                public override string ToString() {
                    var resultBuilder = new StringBuilder();
                    resultBuilder.Append(' ', StartPosition);
                    resultBuilder.Append('|');
                    if (Length > 1) {
                        resultBuilder.Append(' ', Length - 2);
                        resultBuilder.Append('|');
                    }
                    resultBuilder.Append(" : ");
                    resultBuilder.AppendLine(Name);
                    return resultBuilder.ToString();
                }
            }

            internal readonly List<ProductSpanSource> ProductDeclarations = new List<ProductSpanSource>();

            public override string ToString() {
                var resultBuilder = new StringBuilder();
                if (String.IsNullOrWhiteSpace(Text)) {
                    resultBuilder.AppendLine("relation:");
                } else {
                    resultBuilder.AppendLine("exemplar:");
                    if (Text.IndexOfAny(new[] {'\r', '\n'}) != -1) {
                        throw new ArgumentException("The text of an exemplar cannot contain either the line feed character or the carriage return character. The text representation cannot be generated.");
                    }
                    resultBuilder.AppendLine(Text);
                }
                foreach (var productDeclaration in ProductDeclarations) {
                    resultBuilder.Append(productDeclaration);
                }
                resultBuilder.AppendLine("");
                return resultBuilder.ToString();
            }
        }

        public readonly List<ExemplarSource> ExemplarSources = new List<ExemplarSource>();

        public struct IsA {
            public String LeftProduct;
            public String RightProduct;

            public IsA(string leftProduct, string rightProduct) : this() {
                LeftProduct = leftProduct;
                RightProduct = rightProduct;
            }

            public override string ToString() {
                var resultBuilder = new StringBuilder();
                resultBuilder.Append(LeftProduct);
                resultBuilder.Append(" is ");
                if (RightProduct.IndexOfAny("AEIOUaeiou".ToCharArray()) == 0) {
                    resultBuilder.Append("an ");
                } else {
                    resultBuilder.Append("a ");
                }
                resultBuilder.AppendLine(RightProduct);
                return resultBuilder.ToString();
            }
        }

        public readonly List<IsA> IsASources = new List<IsA>();

        public struct Precedes {
            public String LeftProduct;
            public String RightProduct;

            public Precedes(string leftProduct, string rightProduct)
                : this() {
                LeftProduct = leftProduct;
                RightProduct = rightProduct;
            }

            public override string ToString() {
                var resultBuilder = new StringBuilder();
                resultBuilder.Append(LeftProduct);
                resultBuilder.Append(" precedes ");
                resultBuilder.AppendLine(RightProduct);
                return resultBuilder.ToString();
            }
        }

        public readonly List<Precedes> PrecedesSources = new List<Precedes>();

        public struct CharacterSetEntry {
            public List<string> Params;

            public enum Types {
                List,
                Inversion,
                Union,
                Intersection
            }

            public readonly Types Type;

            public CharacterSetEntry(List<string> @params, Types type) {
                Params = @params;
                Type = type;
            }

            public CharacterSetEntry(Types type) {
                Params = new List<string>();
                Type = type;
            }

            public override string ToString() {
                switch (Type) {
                    case Types.List:
                        return "character set: " + String.Join(" ", Params) + Environment.NewLine;
                    case Types.Inversion:
                        return "character set inverted: " + String.Join(" ", Params.Take(2)) + Environment.NewLine;
                    case Types.Union:
                        return "character set union: " + String.Join(" ", Params) + Environment.NewLine;
                    case Types.Intersection:
                        return "character set intersection: " + String.Join(" ", Params) + Environment.NewLine;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public readonly List<CharacterSetEntry> CharacterSetSources = new List<CharacterSetEntry>();

        public static GrammarDocument FromString(String source) {
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
                            result.ExemplarSources.Add(currentExemplarSource);
                            nextLineStartsExemplar = false;
                        } else if (line.Trim() == "exemplar:") {
                            nextLineStartsExemplar = true;
                        } else if (line.Trim() == "relation:") {
                            currentExemplarSource = new ExemplarSource {Text = ""};
                            result.ExemplarSources.Add(currentExemplarSource);
                        } else if (line.Contains(" is a ") || line.Contains(" is an ")) {
                            int isALength = " is a ".Length;
                            int isAIndex = line.IndexOf(" is a ", StringComparison.Ordinal);
                            if (isAIndex == -1) {
                                isALength = " is an ".Length;
                                isAIndex = line.IndexOf(" is an ", StringComparison.Ordinal);
                            }
                            string leftProduct = line.Substring(0, isAIndex).Trim();
                            string rightProduct = line.Substring(isAIndex + isALength).Trim();
                            result.IsASources.Add(new IsA(leftProduct, rightProduct));
                        } else if (line.Contains(" precedes ")) {
                            int precedesLength = " precedes ".Length;
                            int precedesIndex = line.IndexOf(" precedes ", StringComparison.Ordinal);
                            string leftProduct = line.Substring(0, precedesIndex).Trim();
                            string rightProduct = line.Substring(precedesIndex + precedesLength).Trim();
                            result.PrecedesSources.Add(new Precedes(leftProduct, rightProduct));
                        } else if (line.Trim().StartsWith("character set:")) {
                            var names = line.Trim().Substring("character set:".Length).Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                            result.CharacterSetSources.Add(new CharacterSetEntry(names.ToList(), CharacterSetEntry.Types.List));
                        } else if (line.Trim().StartsWith("character set inverted:")) {
                            var names = line.Trim().Substring("character set inverted:".Length).Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                            result.CharacterSetSources.Add(new CharacterSetEntry(names.ToList(), CharacterSetEntry.Types.Inversion));
                        } else if (line.Trim().StartsWith("character set union:")) {
                            var names = line.Trim().Substring("character set union:".Length).Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                            result.CharacterSetSources.Add(new CharacterSetEntry(names.ToList(), CharacterSetEntry.Types.Union));
                        } else if (line.Trim().StartsWith("character set intersection:")) {
                            var names = line.Trim().Substring("character set intersection:".Length).Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                            result.CharacterSetSources.Add(new CharacterSetEntry(names.ToList(), CharacterSetEntry.Types.Intersection));
                        }
                    } else {
                        var productDeclarationParts = line.Split(':');
                        int startPosition = productDeclarationParts[0].IndexOf('|');
                        int length = productDeclarationParts[0].LastIndexOf('|') - startPosition + 1;
                        currentExemplarSource.ProductDeclarations.Add(new ExemplarSource.ProductSpanSource(productDeclarationParts[1].Trim(), startPosition, length));
                    }
                }
            }
            return result;
        }

        public override string ToString() {
            var resultBuilder = new StringBuilder();
            resultBuilder.Append(String.Join("", CharacterSetSources));
            if (CharacterSetSources.Count > 0) resultBuilder.AppendLine();
            resultBuilder.Append(String.Join("", ExemplarSources));
            resultBuilder.Append(String.Join("", IsASources));
            if (IsASources.Count > 0) resultBuilder.AppendLine("");
            resultBuilder.Append(String.Join("", PrecedesSources));
            return resultBuilder.ToString();
        }
    }
}
