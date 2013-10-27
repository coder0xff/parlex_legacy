using System.Windows.Controls;
using parlex;

namespace IDE {
    /// <summary>
    /// Interaction logic for ProductEditor.xaml
    /// </summary>
    public partial class ProductEditor : UserControl {
        private GrammarDocument.ExemplarSource[] _sources;
        public GrammarDocument.ExemplarSource[] Sources {
            get { return _sources; }
            set {
                _sources = value; 
                Populate();
            }
        }

        public ProductEditor() {
            InitializeComponent();
        }

        private void Populate() {
            ExemplarStack.Children.Clear();
            foreach (var exemplarSource in _sources) {
                ExemplarStack.Children.Add(new ExemplarEditor(exemplarSource));
            }
        }
    }
}
