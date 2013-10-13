using parlex;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace IDE {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        private String _loadedFileName;
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
            var tempGrammarDocument = nfa.ToGrammarDocument(product.Title, _compiledGrammar.GetAllProducts());
            Editor.Sources = tempGrammarDocument.ExemplarSources.ToArray();
            var tempGrammar = new CompiledGrammar(tempGrammarDocument);
            try {
                product.ReplaceSequences(tempGrammar.GetAllProducts()[product.Title]);
            } catch (KeyNotFoundException) {
                //the NFA isn't valid at the moment, and that's ok
            }

        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void MenuItem_New_Click(object sender, RoutedEventArgs e) {
            _document = new GrammarDocument();
            _loadedFileName = null;
            Populate();
        }

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e) {
            var openDialog = new Microsoft.Win32.OpenFileDialog {DefaultExt = ".parlex", Filter = "Parlex grammars (*.parlex)|*.parlex|All files (*.*)|*.*"};
            var result = openDialog.ShowDialog(this);
            if (result == true) {
                _document = GrammarDocument.FromString(File.ReadAllText(openDialog.FileName));
                _loadedFileName = openDialog.FileName;
                Populate();
            }
        }

        private void Button_AddProduct_Click(object sender, RoutedEventArgs e) {
            var exemplar = new GrammarDocument.ExemplarSource(" ") {new GrammarDocument.ProductSpanSource("Untitled", 0, 1)};
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
            if (String.IsNullOrEmpty(_loadedFileName)) {
                MenuItem_SaveAs_OnClick(sender, e);
            } else {
                SaveToFile(_loadedFileName);
            }
        }

        private void MenuItem_SaveAs_OnClick(object sender, RoutedEventArgs e) {
            var saveDialog = new Microsoft.Win32.SaveFileDialog {DefaultExt = ".parlex", Filter = "Parlex grammars (*.parlex)|*.parlex|All files (*.*)|*.*"};
            var result = saveDialog.ShowDialog(this);
            if (result == true) {
                SaveToFile(saveDialog.FileName);
                _loadedFileName = saveDialog.FileName;
            }
        }

        private void SaveToFile(string fileName) {
            var text = _compiledGrammar.ToGrammarDocument().ToString();
                File.WriteAllText(fileName, text);
        }
    }
}
