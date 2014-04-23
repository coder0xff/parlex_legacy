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
