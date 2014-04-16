using System;
using System.Collections.Concurrent.More;
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
using Common;
using parlex;
using WPFExtensions.Controls;
using Nfa = IDE.Nfa<parlex.Product, int>;
using State = IDE.Nfa<parlex.Product, int>.State;
using Transition = IDE.Nfa<parlex.Product, int>.Transition;

namespace IDE {
    /// <summary>
    /// Interaction logic for NfaEditor.xaml
    /// </summary>
    public partial class NfaEditor : UserControl {
        private Dictionary<String, Product> _products = CompiledGrammar.GetBuiltInProducts();
        private Nfa _nfa;
        private NfaVisualizer _visualizer;
        private Path _drawPath = new Path{ Fill = System.Windows.Media.Brushes.Black };
        private double columnStride, rowStride;
        private Border[,] borders;
        private bool resetOnArrange = false;

        public event Action NfaChanged = () => { }; 

        public NfaEditor() {
            InitializeComponent();
            _drawPath.IsHitTestVisible = false;
            Grid.Children.Add(_drawPath);
            Panel.SetZIndex(_drawPath, 999);
            Nfa = new Nfa();
            Update();
            //var grammarDocument = GrammarDocument.FromString("");
            //var compiledGrammar = new CompiledGrammar(grammarDocument);
            //_products = compiledGrammar.GetAllProducts();
            //var aProduct = _products["codePoint000041"];
            //var stateCount = 10;
            //var transitionCount = 30;

            //var states = Enumerable.Range(0, stateCount).Select(i => new State(i)).ToList();
            //var nfa = new Nfa();
            //foreach (var state in states) {
            //    nfa.States.Add(state);
            //}
            //nfa.StartStates.Add(states[0]);
            //nfa.AcceptStates.Add(states[stateCount - 1]);
            //for (int makeTransitions = 0; makeTransitions < transitionCount; makeTransitions++) {
            //    nfa.TransitionFunction[states[Rng.Next(stateCount)]][aProduct].Add(states[Rng.Next(stateCount)]);
            //}

            //Nfa = nfa;
        }

        public Nfa Nfa {
            get { return _nfa; }
            set {
                _nfa = value;
                _visualizer = new NfaVisualizer(_nfa, new Typeface("Verdana"), 12);
                Arrange();
                Update();
                NfaChanged();
            }
        }

        public Dictionary<string, Product> Products {
            get { return _products; }
            set { _products = value; }
        }

        private object GetGridObject(int column, int row) {
            if (column >= _visualizer.Columns.Count || _visualizer.Columns[column].Count <= row) {
                return null;
            }
            return _visualizer.Columns[column][row];
        }

        private void AddGridSquare(int column, int row) {
            var border = new Border();
            Grid.Children.Add(border);
            Grid.SetColumn(border, column);
            Grid.SetRow(border, row);
            Object o = GetGridObject(column, row);
            if (column % 2 == 1) {
                border.Style = Resources["OddGridCellStyle"] as Style;
            }
            borders[column, row] = border;
        }

        private void Update() {
            _drawPath.Data = _visualizer.ToGeometry(out columnStride, out rowStride);
            var columnCount = _visualizer.Columns.Count;
            var rowCount = columnCount == 0 ? 1 : _visualizer.Columns.Max(c => c.Count);
            if (columnCount == 0) columnCount = 1;
            if (borders != null) {
                foreach (var border in borders) {
                    Grid.Children.Remove(border);
                }
            }
            borders = new Border[columnCount,rowCount];
            Grid.ColumnDefinitions.Clear();
            Grid.RowDefinitions.Clear();
            while(Grid.ColumnDefinitions.Count < columnCount) {
                var columnDefinition = new ColumnDefinition();
                Grid.ColumnDefinitions.Add(columnDefinition);
                columnDefinition.Width = new GridLength(columnStride);
            }
            while(Grid.RowDefinitions.Count < rowCount) {
                var rowIndex = Grid.RowDefinitions.Count;
                var rowDefinition = new RowDefinition();
                rowDefinition.Height = new GridLength(rowStride);
                Grid.RowDefinitions.Add(rowDefinition);
                for (int columnIndex = 0; columnIndex < Grid.ColumnDefinitions.Count; columnIndex++) {
                    AddGridSquare(columnIndex, rowIndex);
                }
            }
            _drawPath.SetValue(Grid.ColumnSpanProperty, columnCount);
            _drawPath.SetValue(Grid.RowSpanProperty, rowCount);
        }

        public void Arrange() {
            _visualizer.Arrange();
            Update();
        }

        private void PointToColumnAndRow(Point pos, out int column, out int row) {
            column = (int)(pos.X / columnStride);
            row = (int)(pos.Y / rowStride);            
        }

        private void MouseEventArgsToColumnAndRow(MouseEventArgs mea, out int column, out int row) {
            PointToColumnAndRow(mea.GetPosition(Grid), out column, out row);
            
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e) {
            if (DrawLine.X1 != -1) {
                int columnIndex, rowIndex;
                MouseEventArgsToColumnAndRow(e, out columnIndex, out rowIndex);
                Object o = GetGridObject(columnIndex, rowIndex);
                int startColumnIndex, startRowIndex;
                PointToColumnAndRow(new Point(DrawLine.X1, DrawLine.Y1), out startColumnIndex, out startRowIndex);
                if (o is State && (startColumnIndex != columnIndex || startRowIndex != rowIndex)) {
                    DrawLine.X2 = (columnIndex + 0.5) * columnStride;
                    DrawLine.Y2 = (rowIndex + 0.5) * rowStride;
                } else {
                    DrawLine.X2 = e.GetPosition(Grid).X;
                    DrawLine.Y2 = e.GetPosition(Grid).Y;
                }
            }
        }

        private void SetSelection(int columnIndex, int rowIndex) {
            bool wasAlreadySelected = borders[columnIndex, rowIndex].Background == Brushes.Aqua;
            for (int ci = 0; ci <= borders.GetUpperBound(0); ci++) {
                for (int ri = 0; ri <= borders.GetUpperBound(1); ri++) {
                    borders[ci, ri].ClearValue(BackgroundProperty);
                }
            }
            Object o = GetGridObject(columnIndex, rowIndex);
            if (o is Transition || o is State) {
                if (o is Transition && wasAlreadySelected) StartEdit(columnIndex, rowIndex);
                if (o is State && wasAlreadySelected) {
                    var state = o as State;
                    if (_nfa.StartStates.Contains(state)) {
                        _nfa.StartStates.Remove(state);
                        _nfa.AcceptStates.Add(state);
                    } else if (_nfa.AcceptStates.Contains(state)) {
                        _nfa.AcceptStates.Remove(state);
                    } else {
                        _nfa.StartStates.Add(state);
                    }
                    Update();
                    NfaChanged();
                }
                borders[columnIndex, rowIndex].Background = Brushes.Aqua;
            }
        }

        private void StartEdit(int columnIndex, int rowIndex) {
            var border = borders[columnIndex, rowIndex];
            var transition = (Transition) GetGridObject(columnIndex, rowIndex);
            var editText = transition.InputSymbol.Title;
            bool isCodePointProduct = transition.InputSymbol is parlex.CodePointCharacterProduct;
            if (isCodePointProduct) {
                editText = "'" + transition.InputSymbol.GetExample() + "'";
            }
            var textBox = new TextBox {Text = editText, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, MinWidth = columnStride / 3};
            Panel.SetZIndex(border, 1000);
            Action editFinished = () => {
                if (border.Child == null) return;
                Panel.SetZIndex(border, 0);
                border.Child = null;
                var productName = textBox.Text.Trim();
                if (productName == "" || productName.Any(char.IsWhiteSpace)) {
                    System.Media.SystemSounds.Beep.Play();
                    return;                
                }
                if (productName.StartsWith("'") && productName.EndsWith("'")) {
                    var codePoints = productName.GetUtf32CodePoints();
                    if (codePoints.Length == 3) {
                        productName = "codePoint" + codePoints[1].ToString("X6");
                    }
                }
                if (!_products.ContainsKey(productName)) {
                    _products.Add(productName, new Product(productName));
                }
                var product = _products[productName];
                _visualizer.ChangeTransitionProduct(transition, product);
                Update();
                NfaChanged();
            };
            textBox.KeyDown += (sender, args) => {
                if (args.Key == Key.Enter) {
                    editFinished();
                }
            };
            textBox.LostFocus += (sender, args) => editFinished();
            border.Child = textBox;
            if (isCodePointProduct) {
                textBox.Select(1, 1);
            } else {
                textBox.SelectAll();
            }
            textBox.Focus();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            int columnIndex, rowIndex;
            MouseEventArgsToColumnAndRow(e, out columnIndex, out rowIndex);
            Object o = GetGridObject(columnIndex, rowIndex);
            if (e.ClickCount == 2) {
                _visualizer.InsertNewState(columnIndex, rowIndex);
                Update();
                resetOnArrange = true;
                e.Handled = true;
                NfaChanged();
            } else if (o is State) {
                DrawLine.X1 = DrawLine.X2 = (columnIndex + 0.5) * columnStride;
                DrawLine.Y1 = DrawLine.Y2 = (rowIndex + 0.5) * rowStride;
                e.Handled = true;
            } else if (o is Transition) {
                e.Handled = true;
            }
            Grid.Focus();
        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            int columnIndex, rowIndex;
            MouseEventArgsToColumnAndRow(e, out columnIndex, out rowIndex);
            Object o = GetGridObject(columnIndex, rowIndex);
            bool controlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            if (DrawLine.X1 != -1) {
                int startColumnIndex, startRowIndex;
                PointToColumnAndRow(new Point(DrawLine.X1, DrawLine.Y1), out startColumnIndex, out startRowIndex);
                if (startColumnIndex != columnIndex || startRowIndex != rowIndex) {
                    Object oStart = GetGridObject(startColumnIndex, startRowIndex);
                    if (oStart is State) {
                        if (!(o is State)) {
                            o = oStart;
                        }
                        var existingNames = new HashSet<String>(_nfa.TransitionFunction[(State) oStart].Where(inputSymbolAndToStates => inputSymbolAndToStates.Value.Contains((State) o)).Select(inputSymbolAndToStates => inputSymbolAndToStates.Key.Title));
                        var newName = "Empty";
                        var nameCounter = 0;
                        while(existingNames.Contains(newName)) {
                            nameCounter++;
                            newName = "Empty" + nameCounter;
                        }
                        _visualizer.AddTransition((State)oStart, (State)o, new Product(newName));
                        Update();
                        resetOnArrange = true;
                        e.Handled = true;
                        NfaChanged();
                    }
                }
            }
            if (controlPressed) {
                if ((o is State || o is Transition)) {
                    if (borders[columnIndex, rowIndex].Background == Brushes.Aqua) {
                        borders[columnIndex, rowIndex].ClearValue(BackgroundProperty);
                    } else {
                        borders[columnIndex, rowIndex].Background = Brushes.Aqua;
                    }
                    e.Handled = true;
                }
            } else {
                SetSelection(columnIndex, rowIndex);
            }
            DrawLine.X1 = DrawLine.Y1 = DrawLine.X2 = DrawLine.Y2 = -1;
        }

        private void Grid_KeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.Delete) {
                List<Object> toDelete = new List<object>();
                for (int ci = 0; ci <= borders.GetUpperBound(0); ci++) {
                    for (int ri = 0; ri <= borders.GetUpperBound(1); ri++) {
                        if (borders[ci, ri].Background == Brushes.Aqua && borders[ci, ri].Child == null) {
                            toDelete.Add(GetGridObject(ci, ri));
                        }
                    }
                }
                if (toDelete.Count > 0) {
                    foreach (var o in toDelete) {
                        resetOnArrange = true;
                        if (o is Transition) {
                            _visualizer.RemoveTransition((Transition) o);
                        } else {
                            var state = o as State;
                            if (state != null) {
                                _visualizer.RemoveState(state);
                            }
                        }
                    }
                    Update();
                    NfaChanged();
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            if (resetOnArrange) {
                resetOnArrange = false;
                _visualizer = new NfaVisualizer(_nfa, new Typeface("Verdana"), 12);
            }
            Arrange();
            Update();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) {
            Nfa = Nfa.Minimized();
        }
    }
}
