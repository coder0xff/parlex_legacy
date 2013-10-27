using System;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using parlex;

namespace IDE {
    /// <summary>
    /// Interaction logic for ProductSpanEditor.xaml
    /// </summary>
    public partial class ProductSpanEditor : IDisposable {
        public ProductSpanEditor(GrammarDocument.ExemplarSource exemplar, GrammarDocument.ProductSpanSource productSpan) {
            _exemplar = exemplar;
            _productSpan = productSpan;
            _productSpan.PropertyChanged += PropertiesChanged;
            _exemplar.PropertyChanged += PropertiesChanged;
            InitializeComponent();
            SetText();
        }

        private void PropertiesChanged(object sender, String name) {
            SetText();
        }

        void SetText() {
            ExemplarText.Inlines.Clear();
            var paddedText = _exemplar.Text + new string(' ', Math.Max(0, (_productSpan.StartPosition + _productSpan.Length) - _exemplar.Text.Length));
            if (_productSpan.StartPosition > 0) {
                var run = new Run(paddedText.Substring(0, _productSpan.StartPosition));
                run.Foreground = Brushes.LightGray;
                ExemplarText.Inlines.Add(run);
            }
            {
                var border = new Border();
                border.BorderThickness = new System.Windows.Thickness(1);
                border.BorderBrush = Brushes.Black;
                var run = new Run(paddedText.Substring(_productSpan.StartPosition, _productSpan.Length));
                var inlineUiContainer = new InlineUIContainer(border);
                border.Child = new TextBlock(run);
                ExemplarText.Inlines.Add(inlineUiContainer);
            }
            if (_productSpan.StartPosition + _productSpan.Length < _exemplar.Text.Length) {
                var run = new Run(_exemplar.Text.Substring(_productSpan.StartPosition + _productSpan.Length));
                run.Foreground = Brushes.LightGray;
                ExemplarText.Inlines.Add(run);
            }
            {
                var run = new Run(" ");
                run.Foreground = Brushes.LightSlateGray;
                ExemplarText.Inlines.Add(run);                
            }
            NameField.Content = _productSpan.Name;
        }

        private readonly GrammarDocument.ExemplarSource _exemplar;
        private readonly GrammarDocument.ProductSpanSource _productSpan;

        public GrammarDocument.ExemplarSource Exemplar {
            get { return _exemplar; }
        }

        public void Dispose() {
            _productSpan.PropertyChanged -= PropertiesChanged;
        }
    }
}
