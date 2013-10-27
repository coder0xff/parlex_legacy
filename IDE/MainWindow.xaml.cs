using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Input;
using Common;
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
        private CompiledGrammar _compiledGrammar;
        private int _nfaSequenceCounter;
        private int _documentSequenceCounter = 0;
        private int _compiledSequenceCounter = 0;
        private Nfa<OldProduction, int> Nfa {
            get {
                if (_compiledSequenceCounter > _nfaSequenceCounter || _documentSequenceCounter > _nfaSequenceCounter) {
                    GraphEditor.Nfa = (ProductList.SelectedItem as OldProduction).ToNfa();
                    _nfaSequenceCounter = _compiledSequenceCounter;
                }
                return GraphEditor.Nfa;
            }
            set {
                if (_compiledSequenceCounter > _nfaSequenceCounter || _documentSequenceCounter > _nfaSequenceCounter) {
                    GraphEditor.Nfa = (ProductList.SelectedItem as OldProduction).ToNfa();
                    _nfaSequenceCounter = _compiledSequenceCounter;
                }

                if (value.IsEquivalent(GraphEditor.Nfa)) return;
                _nfaSequenceCounter = _nfaSequenceCounter++;
                GraphEditor.Nfa = value;
            }
        }

        public MainWindow() {
            InitializeComponent();
            Populate();
        }

        private void Populate() {
            ProductList.DataContext = _compiledGrammar;
        }

        private void NfaEditor_NfaChanged() {
            var product = (OldProduction) ProductList.SelectedItem;
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
            _compiledGrammar = new CompiledGrammar(new GrammarDocument());
            _loadedFileName = null;
            Populate();
        }

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e) {
            var openDialog = new Microsoft.Win32.OpenFileDialog {DefaultExt = ".parlex", Filter = "Parlex grammars (*.parlex)|*.parlex|All files (*.*)|*.*"};
            var result = openDialog.ShowDialog(this);
            if (result == true) {
                _compiledGrammar = new CompiledGrammar(GrammarDocument.FromString(File.ReadAllText(openDialog.FileName)));
                _loadedFileName = openDialog.FileName;
                Populate();
            }
        }

        private void Button_AddProduct_Click(object sender, RoutedEventArgs e) {
            //_compiledGrammar.
            //Populate();
        }

        private void ProductList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var product = (OldProduction) ProductList.SelectedItem;
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

        private void UIElement_ProductName_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
            throw new NotImplementedException();
            //var textBox = sender as TextBox;
            //Debug.Assert(textBox != null, "textBox != null");
            //var bindingExpression = BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty);
            //Debug.Assert(bindingExpression != null, "bindingExpression != null");
            //var product = bindingExpression.ResolvedSource as OldProduction;
            //Debug.Assert(product != null, "product != null");
            //if (textBox.Text == product.Title) {
            //    return;
            //}
            //foreach (var productSpan in _document.ExemplarSources.SelectMany(exemplarSource => exemplarSource.Where(productSpan => productSpan.Name == product.Title))) {
            //    productSpan.Name = textBox.Text;
            //}
            //_compiledGrammar = new CompiledGrammar(_document);
            //Populate();
        }

        private void UIElement_ProductName_OnKeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                ProductList.Focus();
            } else if (e.Key == Key.Escape) {
                var textBox = sender as TextBox;
                Debug.Assert(textBox != null, "textBox != null");
                var bindingExpression = BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty);
                Debug.Assert(bindingExpression != null, "bindingExpression != null");
                var product = bindingExpression.ResolvedSource as OldProduction;
                Debug.Assert(product != null, "product != null");
                textBox.Text = product.Title;
            }
        }
    }
}
