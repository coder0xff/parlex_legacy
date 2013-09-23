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
        private GrammarDocument _document;
        private CompiledGrammar _compiledGrammar;

        public MainWindow() {
            InitializeComponent();
            _document = new GrammarDocument();
            Populate();
        }

        private void Populate() {
            _compiledGrammar = new CompiledGrammar(_document);
            ProductList.DataContext = _compiledGrammar;
        }

        private void NfaEditor_NfaChanged() {
            var product = (Product)ProductList.SelectedItem;
            var nfa = GraphEditor.Nfa;
            var exemplarSources = nfa.ToExemplarSources(product.Title, _compiledGrammar.GetAllProducts());
            Editor.Sources = exemplarSources;
            GrammarDocument tempGrammarDocument = new GrammarDocument();
            tempGrammarDocument.ExemplarSources.AddRange(exemplarSources);
            var tempGrammar = new CompiledGrammar(tempGrammarDocument);
            product.ReplaceSequences(tempGrammar.GetAllProducts()[product.Title]);
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void MenuItem_New_Click(object sender, RoutedEventArgs e) {
            _document = new GrammarDocument();
            Populate();
        }

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e) {
            var openDialog = new Microsoft.Win32.OpenFileDialog();
            openDialog.DefaultExt = ".parlex";
            openDialog.Filter = "Parlex grammars (*.parlex)|*.parlex|All files (*.*)|*.*";
            var result = openDialog.ShowDialog(this);
            if (result == true) {
                _document = GrammarDocument.FromString(File.ReadAllText(openDialog.FileName));
                Populate();
            }
        }

        private void Button_AddProduct_Click(object sender, RoutedEventArgs e) {
            var exemplar = new GrammarDocument.ExemplarSource(" ");
            exemplar.Add(new GrammarDocument.ProductSpanSource("Untitled", 0, 1));
            _document.ExemplarSources.Add(exemplar);
            Populate();
        }

        private void ProductList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var product = (Product)ProductList.SelectedItem;
            if (product == null) {
                GraphEditor.Visibility = Visibility.Collapsed;
                return;
            }
            GraphEditor.Visibility = Visibility.Visible;
            GraphEditor.Nfa = product.ToNfa();
        }

        private void MenuItem_Save_Click(object sender, RoutedEventArgs e) {
            var text = _compiledGrammar.ToGrammarDocument().ToString();
            var saveDialog = new Microsoft.Win32.SaveFileDialog();
            saveDialog.DefaultExt = ".parlex";
            saveDialog.Filter = "Parlex grammars (*.parlex)|*.parlex|All files (*.*)|*.*";
            var result = saveDialog.ShowDialog(this);
            if (result == true) {
                File.WriteAllText(saveDialog.FileName, text);
            }
        }
    }
}
