using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace parlex {
    public class GrammarDocument {
        public class ProductSpanSource : IPropertyChangedNotifier {
            private String _name;
            private int _startPosition;
            private int _length;

            public event Action<Object, String> PropertyChanged = delegate { };

            public ProductSpanSource(String name, int startPosition, int length) {
                _name = name;
                _startPosition = startPosition;
                _length = length;
            }

            public ProductSpanSource() {}

            public string Name {
                get { return _name; }
                set {
                    if (_name == value) {
                        return;
                    }
                    _name = value;
                    PropertyChanged(this, "Name");
                }
            }

            public int StartPosition {
                get { return _startPosition; }
                set {
                    if (_startPosition == value) {
                        return;
                    }
                    _startPosition = value;
                    PropertyChanged(this, "StartPosition");
                }
            }

            public int Length {
                get { return _length; }
                set {
                    if (_length != value) {
                        _length = value;
                        PropertyChanged(this, "Length");
                    }
                }
            }

            public override string ToString() {
                var resultBuilder = new StringBuilder();
                resultBuilder.Append(' ', _startPosition);
                resultBuilder.Append('|');
                if (_length > 1) {
                    resultBuilder.Append(' ', _length - 2);
                    resultBuilder.Append('|');
                }
                resultBuilder.Append(" : ");
                resultBuilder.AppendLine(_name);
                return resultBuilder.ToString();
            }
        }

        public class ExemplarSource : ObservableList<ProductSpanSource>, IPropertyChangedNotifier {
            private String _text;

            public ExemplarSource() {
            }

            public ExemplarSource(String text) {
                _text = text;
            }

            public string Text {
                get { return _text; }
                set {
                    if (_text == value) {
                        return;
                    }
                    _text = value;
                    PropertyChanged(this, "Text");
                }
            }

            public event Action<Object, String> PropertyChanged = delegate { };

            public override string ToString() {
                var resultBuilder = new StringBuilder();
                if (String.IsNullOrWhiteSpace(_text)) {
                    resultBuilder.AppendLine("relation:");
                } else {
                    resultBuilder.AppendLine("exemplar:");
                    if (_text.IndexOfAny(new[] {'\r', '\n'}) != -1) {
                        throw new ArgumentException("The text of an exemplar cannot contain either the line feed character or the carriage return character. The text representation cannot be generated.");
                    }
                    resultBuilder.AppendLine(_text);
                }
                foreach (var productDeclaration in this) {
                    resultBuilder.Append(productDeclaration);
                }
                resultBuilder.AppendLine("");
                return resultBuilder.ToString();
            }

            public bool TryExemplify(Dictionary<String, Product> products) {
                if (!String.IsNullOrEmpty(_text)) {
                    return true;
                }
                StringBuilder builder = new StringBuilder("");
                var sequenceLength = this.Max(span => span.StartPosition + span.Length);
                var remap = new List<int>();
                for (int position = 0; position < sequenceLength; ) {
                    while(remap.Count <= position) {
                        remap.Add(builder.Length);
                    }
                    var selectedSpan = this.Where(span => span.StartPosition == position && span.StartPosition + span.Length > position).OrderBy(span => span.Length).FirstOrDefault();
                    if (selectedSpan == null) return false;
                    Product product;
                    if (products.TryGetValue(selectedSpan.Name, out product)) {
                        builder.Append(product.GetExample());
                    } else {
                        return false;
                    }
                    position += selectedSpan.Length;
                }
                while (remap.Count <= sequenceLength) {
                    remap.Add(builder.Length);
                }
                foreach (var span in this) {
                    var spanEnd = remap[span.StartPosition + span.Length];
                    span.StartPosition = remap[span.StartPosition];
                    span.Length = spanEnd - span.StartPosition;
                }
                Text = builder.ToString();
                return true;
            }
        }

        public readonly List<ExemplarSource> ExemplarSources = new List<ExemplarSource>();

        public class IsA : IPropertyChangedNotifier {
            private String _leftProduct;
            private String _rightProduct;

            public IsA() {
            }

            public IsA(string leftProduct, string rightProduct) {
                _leftProduct = leftProduct;
                _rightProduct = rightProduct;
            }

            public string LeftProduct {
                get { return _leftProduct; }
                set {
                    if (_leftProduct == value) {
                        return;
                    }
                    _leftProduct = value;
                    PropertyChanged(this, "LeftProduct");
                }
            }

            public string RightProduct {
                get { return _rightProduct; }
                set {
                    if (_rightProduct == value) {
                        return;
                    }
                    _rightProduct = value;
                    PropertyChanged(this, "RightProduct");
                }
            }

            public event Action<Object, String> PropertyChanged = delegate { };

            public override string ToString() {
                var resultBuilder = new StringBuilder();
                resultBuilder.Append(_leftProduct);
                resultBuilder.Append(" is ");
                resultBuilder.Append(_rightProduct.IndexOfAny("AEIOUaeiou".ToCharArray()) == 0 ? "an " : "a ");
                resultBuilder.AppendLine(_rightProduct);
                return resultBuilder.ToString();
            }
        }

        public readonly ObservableList<IsA> IsASources = new ObservableList<IsA>();

        public class Precedes : IPropertyChangedNotifier {
            private String _leftProduct;
            private String _rightProduct;

            public Precedes(string leftProduct, string rightProduct) {
                _leftProduct = leftProduct;
                _rightProduct = rightProduct;
            }

            public string LeftProduct {
                get { return _leftProduct; }
                set {
                    if (_leftProduct == value) {
                        return;
                    }
                    _leftProduct = value;
                    PropertyChanged(this, "LeftProduct");
                }
            }

            public string RightProduct {
                get { return _rightProduct; }
                set {
                    if (_rightProduct == value) {
                        return;
                    }
                    _rightProduct = value;
                    PropertyChanged(this, "RightProduct");
                }
            }

            public event Action<Object, String> PropertyChanged = delegate { };

            public override string ToString() {
                var resultBuilder = new StringBuilder();
                resultBuilder.Append(_leftProduct);
                resultBuilder.Append(" precedes ");
                resultBuilder.AppendLine(_rightProduct);
                return resultBuilder.ToString();
            }
        }

        public readonly ObservableList<Precedes> PrecedesSources = new ObservableList<Precedes>();

        public class CharacterSetList : ObservableListOfImmutable<int>, IPropertyChangedNotifier {
            private String _name;

            public event Action<Object, String> PropertyChanged;

            public string Name {
                get { return _name; }
                set {
                    if (_name != value) {
                        _name = value;
                        PropertyChanged(this, "Name");
                    }
                }
            }
        }

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
                            currentExemplarSource = new ExemplarSource(line);
                            result.ExemplarSources.Add(currentExemplarSource);
                            nextLineStartsExemplar = false;
                        } else if (line.Trim() == "exemplar:") {
                            nextLineStartsExemplar = true;
                        } else if (line.Trim() == "relation:") {
                            currentExemplarSource = new ExemplarSource("");
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
                        currentExemplarSource.Add(new ProductSpanSource(productDeclarationParts[1].Trim(), startPosition, length));
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
