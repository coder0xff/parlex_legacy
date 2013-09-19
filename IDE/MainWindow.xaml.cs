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

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            var testFile = File.ReadAllText(@"C:\Users\Brent\Dropbox\parlex\test.parlex");
            var grammarDocument = GrammarDocument.FromString(testFile);
            var compiledGrammar = new CompiledGrammar(grammarDocument);
            var aProduct = compiledGrammar.UserProducts["codePoint000041"];
            var bProduct = compiledGrammar.UserProducts["codePoint000042"];
            var cProduct = compiledGrammar.UserProducts["codePoint000043"];
            var dProduct = compiledGrammar.UserProducts["codePoint000044"];
            var eProduct = compiledGrammar.UserProducts["codePoint000045"];
            var fProduct = compiledGrammar.UserProducts["codePoint000046"];
            var gProduct = compiledGrammar.UserProducts["codePoint000047"];
            var hProduct = compiledGrammar.UserProducts["codePoint000048"];
            var iProduct = compiledGrammar.UserProducts["codePoint000049"];
            var state0 = new Nfa<Product, int>.State(0);
            var state1 = new Nfa<Product, int>.State(1);
            var state2 = new Nfa<Product, int>.State(2);
            var state3 = new Nfa<Product, int>.State(3);
            var state4 = new Nfa<Product, int>.State(4);
            var nfa = new Nfa<Product, int>();
            nfa.States.Add(state0);
            nfa.States.Add(state1);
            nfa.States.Add(state2);
            nfa.States.Add(state3);
            nfa.States.Add(state4);
            nfa.StartStates.Add(state0);
            nfa.AcceptStates.Add(state4);
            nfa.TransitionFunction[state0][aProduct].Add(state1);
            nfa.TransitionFunction[state1][bProduct].Add(state2);
            nfa.TransitionFunction[state2][cProduct].Add(state3);
            //nfa.TransitionFunction[state3][dProduct].Add(state4);
            //nfa.TransitionFunction[state3][eProduct].Add(state1);
            nfa.TransitionFunction[state1][cProduct].Add(state3);
            nfa.TransitionFunction[state2][dProduct].Add(state0);
            nfa.TransitionFunction[state3][eProduct].Add(state0);
            //nfa.TransitionFunction[state2][fProduct].Add(state4);
            //nfa.TransitionFunction[state3][gProduct].Add(state4);
            //nfa.TransitionFunction[state0][hProduct].Add(state2);
            //nfa.TransitionFunction[state4][iProduct].Add(state1);
            var graph = new RecognizerGraph(nfa, new Typeface("Verdana"), 12);
            Close();
        }
    }
}
