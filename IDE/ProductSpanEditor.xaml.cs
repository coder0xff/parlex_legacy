using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using parlex;

namespace IDE {
    /// <summary>
    /// Interaction logic for ProductSpanEditor.xaml
    /// </summary>
    public partial class ProductSpanEditor : UserControl, IDisposable {
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
            if (_productSpan.StartPosition > 0) {
                var run = new Run(_exemplar.Text.Substring(0, _productSpan.StartPosition));
                run.Foreground = Brushes.LightGray;
                ExemplarText.Inlines.Add(run);
            }
            {
                var border = new Border();
                border.BorderThickness = new System.Windows.Thickness(1);
                border.BorderBrush = Brushes.Black;
                var run = new Run(_exemplar.Text.Substring(_productSpan.StartPosition, _productSpan.Length));
                var inlineUIContainer = new InlineUIContainer(border);
                border.Child = new TextBlock(run);
                ExemplarText.Inlines.Add(inlineUIContainer);
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
            Name.Content = _productSpan.Name;
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
