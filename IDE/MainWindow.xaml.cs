using System;
using System.Collections.Generic;
using System.IO;
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
using Common;
using parlex;

namespace IDE {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            var exemplar = new parlex.GrammarDocument.ExemplarSource("a = b + c");
            var productSpan = new parlex.GrammarDocument.ProductSpanSource("addition", 4, 5);
            exemplar.Add(productSpan);
            var productSpanEditor = new ProductSpanEditor(exemplar, productSpan);
        }

        private void NfaEditor_NfaChanged() {
            var nfa = GraphEditor.Nfa;
            Editor.Sources = nfa.ToExemplarSources("test", CompiledGrammar.GetBuiltInProducts());
        }
    }
}
