using System.Windows.Controls;
using parlex;

namespace IDE {
    /// <summary>
    /// Interaction logic for ExemplarEditor.xaml
    /// </summary>
    public partial class ExemplarEditor : UserControl {
        private GrammarDocument.ExemplarSource _exemplar;

        public ExemplarEditor(GrammarDocument.ExemplarSource exemplar) {
            InitializeComponent();
            Exemplar = exemplar;
        }
        
        public GrammarDocument.ExemplarSource Exemplar {
            get { return _exemplar; }
            set {
                _exemplar = value; 
                Populate();
            }
        }

        private void Populate() {
            TextLabel.Content = _exemplar.Text;
            foreach (GrammarDocument.ProductSpanSource productSpanSource in _exemplar) {
                ProductSpanEditors.Children.Add(new ProductSpanEditor(_exemplar, productSpanSource));
            }
        }
    }
}
