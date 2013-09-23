﻿using Common;
using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using parlex;
using Nfa = IDE.Nfa<parlex.Product, int>;
using State = IDE.Nfa<parlex.Product, int>.State;
using Transition = IDE.Nfa<parlex.Product, int>.Transition;

namespace IDE {
    public class NfaVisualizer {
        private Nfa _productNfa;
        private Dictionary<String, Geometry> _formattedTexts = new Dictionary<string, Geometry>();
        internal List<List<Object>> Columns = new List<List<object>>();
        private JaggedAutoDictionary<int, Object, HashSet<Object>> _columnConnectionsRight = new JaggedAutoDictionary<int, Object, HashSet<Object>>((i, o) => new HashSet<object>());
        private List<Object[]> _lines = new List<Object[]>();
        private DiscreteProbabilityDistribution<int> _mutateColumnSelectionProbabilityDistribution; //each column should have a weight for its likelihood for being the target of a mutate

        private Typeface _font;
        private double _emSize;

        class LineWayPoint {
        }

        public NfaVisualizer(Nfa productNfa, Typeface font, double emSize) {
            _font = font;
            _emSize = emSize;
            _productNfa = productNfa;
            if (productNfa.States.Count > 0) {
                Create();
                Arrange();
            }
        }

        private Dictionary<String, Geometry> ComputeFormattedTexts() {
            var allSymbols = _productNfa.TransitionFunction.SelectMany(fromStateAndInputSymbolsAndToStates => fromStateAndInputSymbolsAndToStates.Value).Select(inputSymbolAndToStates => inputSymbolAndToStates.Key).Distinct();
            return allSymbols.Distinct().ToDictionary(product => product.ToString(), product => new FormattedText(product.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _font, _emSize, Brushes.Black).BuildGeometry(new Point()));
        }

        private void Create() {
            _formattedTexts = ComputeFormattedTexts();
            var stateColumnAssignments = _productNfa.GetLayerAssignments().ToDictionary(kvp => kvp.Key, kvp => kvp.Value * 2 + 1); // * 2, because we need to leave columns in between and at the ends
            var columnCount = stateColumnAssignments.Values.Max() + 3; //1 for left of the first state, 2 for right of the last state (space for a transition and a feedback line)
            Columns = Enumerable.Range(0, columnCount).Select(ci => new List<Object>()).ToList(); 
            for (int initColumns = 0; initColumns < columnCount; initColumns++) Columns[initColumns] = new List<object>();
            foreach (var stateColumnAssignment in stateColumnAssignments) {
                Columns[stateColumnAssignment.Value].Add(stateColumnAssignment.Key);
            }
            foreach (var fromStateAndInputSymbols in _productNfa.TransitionFunction) {
                var fromState = fromStateAndInputSymbols.Key;
                foreach (var inputSymbolAndToStates in fromStateAndInputSymbols.Value) {
                    var inputSymbol = inputSymbolAndToStates.Key;
                    foreach (var toState in inputSymbolAndToStates.Value) {
                        var fromStateColumn = stateColumnAssignments[fromState];
                        var toStateColumn = stateColumnAssignments[toState];
                        var transitionColumn = fromStateColumn < toStateColumn ? (fromStateColumn + toStateColumn) / 2 : fromStateColumn + 1;
                        var transition = new Transition(fromState, inputSymbol, toState);
                        Columns[transitionColumn].Add(transition);
                        CreateLine(fromStateColumn, fromState, transitionColumn, transition);
                        CreateLine(transitionColumn, transition, toStateColumn, toState);
                    }
                }
            }

            _mutateColumnSelectionProbabilityDistribution = new DiscreteProbabilityDistribution<int>(Enumerable.Range(0, columnCount).Select(i => new Tuple<int, double>(i, Columns[i].Count * Columns[i].Count)));

            //_rowCount = _columns.Max(c => c.Count);
            //foreach (var column in _columns) {
            //    while (column.Count < _rowCount) column.Add(null);
            //}
        }

        private void CreateLine(int fromColumn, Object fromObject, int toColumn, Object toObject) {
            if (fromColumn == toColumn - 1) {
                CreateConnection(fromColumn, fromObject, toObject);
                _lines.Add(new[] {fromObject, toObject});
            } else if (fromColumn >= toColumn) {
                var fromWayPoint = new LineWayPoint();
                Columns[fromColumn + 1].Add(fromWayPoint);
                CreateConnection(fromColumn, fromObject, fromWayPoint);
                var toWayPoint = new LineWayPoint();
                Columns[toColumn - 1].Add(toWayPoint);
                CreateConnection(toColumn - 1, toWayPoint, toObject);
                CreateLine(toColumn - 1, toWayPoint, fromColumn + 1, fromWayPoint);
                var lineNumber = _lines.Count - 1;
                var changeLines = _lines[lineNumber].Reverse().ToList();
                changeLines.Insert(0, fromObject);
                changeLines.Add(toObject);
                _lines[lineNumber] = changeLines.ToArray();
            } else {
                var objectList = new List<Object>();
                objectList.Add(fromObject);
                var previousObject = fromObject;
                for (var i = fromColumn + 1; i < toColumn; i++) {
                    var wayPoint = new LineWayPoint();
                    objectList.Add(wayPoint);
                    Columns[i].Add(wayPoint);
                    CreateConnection(i - 1, previousObject, wayPoint);
                    previousObject = wayPoint;
                }
                CreateConnection(toColumn - 1, previousObject, toObject);
                objectList.Add(toObject);
                _lines.Add(objectList.ToArray());
            }
        }

        private void CreateConnection(int leftColumn, Object leftObject, Object rightObject) {
            _columnConnectionsRight[leftColumn][leftObject].Add(rightObject);
        }

        private double GetFitnessReciprocal() {
            double result = 0;
            var objectToIndexLookup = Columns.Select(list => Enumerable.Range(0, list.Count).Where(index => list[index] != null).ToDictionary(index => list[index], index => index)).ToArray();
            for (int columnIndex = 0; columnIndex < Columns.Count - 1; columnIndex++) {
                var connections = _columnConnectionsRight[columnIndex];
                var leftLookup = objectToIndexLookup[columnIndex];
                var rightLookup = objectToIndexLookup[columnIndex + 1];
                var orderedLineSegments = connections.Select(fromObjectAndToObjects => new Tuple<int, int[]>(leftLookup[fromObjectAndToObjects.Key], fromObjectAndToObjects.Value.Select(toObject => rightLookup[toObject]).OrderBy(i => i).ToArray())).OrderBy(t => t.Item1).ToArray();
                for (int i = 0; i < orderedLineSegments.Length - 1; i++) {
                    var crossCountTable = new int[Columns[columnIndex + 1].Count]; //the number of 'line1s' that are crossed where line2's right row is the index
                    {
                        var line1Group = orderedLineSegments[i];
                        var line1LeftRow = line1Group.Item1;
                        var crossCount = line1Group.Item2.Length;
                        int tableIndex = 0;
                        foreach (var line1RightRow in line1Group.Item2) {
                            result += Math.Abs(line1LeftRow - line1RightRow) * 0.1; //slightly favor vertical line segments instead of diagonals
                            for (; tableIndex < line1RightRow; tableIndex++) {
                                crossCountTable[tableIndex] = crossCount;
                            }
                            crossCount--;
                        }
                    }
                    for (int j = i + 1; j < orderedLineSegments.Length; j++) {
                        var line2Group = orderedLineSegments[j];
                        foreach (var line2RightRow in line2Group.Item2) {
                            result += crossCountTable[line2RightRow]; // * 1 + (_columnCount - columnIndex) * 0.1; //favor left side being untangled
                        }
                    }
                }
            }
            return result;
        }

        private NfaVisualizer(NfaVisualizer other) {
            _productNfa = other._productNfa;
            _formattedTexts = other._formattedTexts;
            Columns = other.Columns.Select(list => list.ToList()).ToList();
            _columnConnectionsRight = other._columnConnectionsRight;
            //_rowCount = other._rowCount;
            _mutateColumnSelectionProbabilityDistribution = other._mutateColumnSelectionProbabilityDistribution;
            _lines = other._lines;
            _font = other._font;
            _emSize = other._emSize;
        }

        private void Mutate(int count) {
            for (int i = 0; i < count; i++) {
                var column = Columns[_mutateColumnSelectionProbabilityDistribution.Next()];
                if (column.Count < 2) {
                    continue;
                }
                var j = Rng.Next(column.Count);
                var k = Rng.Next(column.Count - 1);
                if (k >= j) k++;
                var temp = column[j];
                column[j] = column[k];
                column[k] = temp;
            }
        }

        private void RemoveEmpties() {
            foreach (var column in Columns) {
                for (int i = 0; i < column.Count; i++) {
                    if (column[i] == null) {
                        column.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        public void Arrange(TimeSpan maxProcessDuration = default(TimeSpan)) {
            RemoveEmpties();
            if (maxProcessDuration == default(TimeSpan)) maxProcessDuration = new TimeSpan(0, 0, 30);
            const double mutatesPerObject = 2;
            const double decayMax = 0.9999999;
            var objectCount = Columns.Sum(c => c.Count);
            var decayMin = Math.Pow(0.95, 1.0 / objectCount);
            var decayDecay = Math.Pow(0.95, 1.0 / (objectCount * objectCount));

            double mutateStrength = 1;
            var currentBest = this;
            var currentBestCount = GetFitnessReciprocal();
            //SavePng("C:\\Users\\Brent\\Desktop\\RG\\Test_" + currentBestCount + ".png");
            var mutateCount = (int)Math.Ceiling(objectCount * mutatesPerObject * mutateStrength);
            double decay = 0.9999;
            DateTime startTime = DateTime.Now;
            while (DateTime.Now - startTime < maxProcessDuration) {
                System.Diagnostics.Debug.WriteLine(mutateStrength);
                var next = new NfaVisualizer(currentBest);
                next.Mutate(mutateCount);
                var newCount = next.GetFitnessReciprocal();
                if (newCount < currentBestCount) {
                    currentBestCount = newCount;
                    currentBest = next;
                    //next.SavePng("C:\\Users\\Brent\\Desktop\\RG\\Test_" + newCount + ".png");
                    decay = decayMax;
                } else {
                    mutateStrength *= decay;
                    decay = Math.Max(decay * decayDecay, decayMin);
                }
                mutateCount = (int)(objectCount * mutatesPerObject * mutateStrength);
                if (mutateCount == 0) {
                    break;
                }
            }
            Columns = currentBest.Columns;
            //SavePng("C:\\Users\\Brent\\Desktop\\RG\\Test_" + currentBestCount + ".png");
        }

        public Geometry ToGeometry(out double columnStride, out double rowStride, double penThickness = 2) {
            var pen = new Pen(Brushes.Black, penThickness);
            const double margin = 5;
            var widestProductWidth = _formattedTexts.Count == 0 ? 30 : _formattedTexts.Values.Max(g => g.Bounds.Width);
            var highestProductHeight = _formattedTexts.Count == 0 ? 30 : _formattedTexts.Values.Max(g => g.Bounds.Height);
            columnStride = widestProductWidth * 2;
            rowStride = highestProductHeight * 4;
            double columnStrideCopy = columnStride;
            double rowStrideCopy = rowStride;
            Func<int, int, Point> getPos = (columnIndex, row) => new Point(columnStrideCopy * columnIndex, rowStrideCopy * row);

            var result = new PathGeometry();
            var objectToColumnLookup = Enumerable.Range(0, Columns.Count).SelectMany(columnIndex => Columns[columnIndex].Where(o => o != null).Select(o => new Tuple<Object, int>(o, columnIndex))).ToDictionary(t => t.Item1, t => t.Item2);
            var objectToRowLookup = Columns.Select(list => Enumerable.Range(0, list.Count).Where(index => list[index] != null).ToDictionary(index => list[index], index => index)).ToArray();

            for (int columnIndex = 0; columnIndex < Columns.Count; columnIndex++) {
                var column = Columns[columnIndex];
                //draw objects
                for (int row = 0; row < column.Count; row++) {
                    var o = column[row];
                    var center = getPos(columnIndex, row);
                    if (o is State) {
                        var state = o as State;
                        var pos = getPos(columnIndex, row);
                        var geometry = new EllipseGeometry(pos, margin, margin).GetWidenedPathGeometry(pen);
                        result = Geometry.Combine(result, geometry , GeometryCombineMode.Union, Transform.Identity);
                        if (_productNfa.StartStates.Contains(state)) {
                                var shiftLeft = -columnStride * (columnIndex == 1 ? 1.5 : 0.5);
                            var arrowGeometry = new PathGeometry(new PathFigure[] {
                                new PathFigure(pos + new Vector(shiftLeft + margin, 0), new PathSegment[] {new LineSegment(pos + new Vector(-margin, 0), true),}, false),
                                new PathFigure(pos + new Vector(shiftLeft, -margin), new PathSegment[] {new LineSegment(pos + new Vector(shiftLeft + margin, 0), true),}, false),
                                new PathFigure(pos + new Vector(shiftLeft, margin), new PathSegment[] {new LineSegment(pos + new Vector(shiftLeft + margin, 0), true),}, false),
                            }).GetWidenedPathGeometry(pen);
                            result = Geometry.Combine(result, arrowGeometry, GeometryCombineMode.Union, Transform.Identity);
                        }
                        if (_productNfa.AcceptStates.Contains(state)) {
                            var circleGeometry = new EllipseGeometry(pos, margin + 5, margin + 5).GetWidenedPathGeometry(pen);
                            result = Geometry.Combine(result, circleGeometry, GeometryCombineMode.Union, Transform.Identity);
                        }
                    } else if (o is Transition) {
                        var transition = (Transition) o;
                        var rectangleRadius = transition.InputSymbol is ICharacterProduct ? margin : 0;
                        var g = _formattedTexts[((Transition) o).InputSymbol.ToString()];
                        var outlineRect = new Rect(center.X - g.Bounds.Width * 0.5, center.Y - g.Bounds.Height * 0.5, g.Bounds.Width, g.Bounds.Height);
                        outlineRect.Inflate(margin, margin);
                        var geometry = new RectangleGeometry(outlineRect, rectangleRadius, rectangleRadius).GetWidenedPathGeometry(pen);
                        result = Geometry.Combine(result, geometry, GeometryCombineMode.Union, Transform.Identity);
                        var translatedText = new GeometryGroup();
                        translatedText.Children.Add(g);
                        translatedText.Transform = new TranslateTransform(center.X - g.Bounds.Width * 0.5, center.Y - g.Bounds.Height * 0.5);
                        result = Geometry.Combine(result, translatedText, GeometryCombineMode.Union, Transform.Identity);
                    }
                }
            }

            //draw lines           
            Func<Object, bool, Point> getObjSidePosFunc = (o, side) => {
                //side == true is right side
                var columnIndex = objectToColumnLookup[o];
                var center = getPos(columnIndex, objectToRowLookup[columnIndex][o]);
                if (o is State) {
                    return center + new Vector(margin, 0) * (side ? 1 : -1);
                }
                if (o is Transition) {
                    var transition = (Transition) o;
                    return center + new Vector(_formattedTexts[transition.InputSymbol.ToString()].Bounds.Width * 0.5 + margin, 0) * (side ? 1 : -1);
                }
                return center;
            };

            Func<Object, bool, Point> getObjApproacherPosFunc = (o, side) => {
                var columnIndex = objectToColumnLookup[o];
                var center = getPos(columnIndex, objectToRowLookup[columnIndex][o]);
                return center + new Vector(widestProductWidth * (side ? 0.7 : -0.7) + (side ? margin * 2 : margin * -2), 0);
            };

            var objectsThatHaveALineToTheRight = new HashSet<Object>(_columnConnectionsRight.Values.SelectMany(pairs => pairs.Keys));
            var objectsThatHaveALineToTheLeft = new HashSet<Object>(_columnConnectionsRight.Values.SelectMany(pairs => pairs.Values.SelectMany(s => s)));
            
            foreach (var objects in _lines) {
                var polyLinePoints = new List<Point>();
                var fromObject = objects[0];
                for (int toObjectIndex = 1; toObjectIndex < objects.Length; toObjectIndex++) {
                    var toObject = objects[toObjectIndex];
                    bool isFeedbackSegment = objectToColumnLookup[fromObject] > objectToColumnLookup[toObject];
                    if (toObjectIndex == 1) {
                        polyLinePoints.Add(getObjSidePosFunc(fromObject, !isFeedbackSegment));
                    }
                    polyLinePoints.Add(getObjApproacherPosFunc(fromObject, !isFeedbackSegment));
                    if (!(toObject is LineWayPoint) || isFeedbackSegment ? objectsThatHaveALineToTheLeft.Contains(toObject) : objectsThatHaveALineToTheRight.Contains(toObject)) {
                        polyLinePoints.Add(getObjApproacherPosFunc(toObject, isFeedbackSegment));
                        polyLinePoints.Add(getObjSidePosFunc(toObject, isFeedbackSegment));
                    }
                    fromObject = toObject;
                }

                var polyBezierPoints = new List<Point>();
                polyBezierPoints.Add(polyLinePoints[0]);
                polyBezierPoints.Add(polyLinePoints[1]);
                for (int guideSegmentIndex = 1; guideSegmentIndex < polyLinePoints.Count - 2; guideSegmentIndex++) {
                    polyBezierPoints.Add(polyLinePoints[guideSegmentIndex]);
                    polyBezierPoints.Add(polyLinePoints[guideSegmentIndex] + ((polyLinePoints[guideSegmentIndex + 1] - polyLinePoints[guideSegmentIndex]) * 0.5));
                    polyBezierPoints.Add(polyLinePoints[guideSegmentIndex + 1]);
                }
                polyBezierPoints.Add(polyLinePoints[polyLinePoints.Count - 2]);
                polyBezierPoints.Add(polyLinePoints[polyLinePoints.Count - 1]);
                var geometry = new PathGeometry(new[] {new PathFigure(polyBezierPoints[0], new[] {new PolyBezierSegment(polyBezierPoints.Skip(1), true)}, false)}).GetWidenedPathGeometry(pen);
                result = Geometry.Combine(result, geometry, GeometryCombineMode.Union, Transform.Identity);
                //gg.Children.Add(new PathGeometry(new[] { new PathFigure(polyLinePoints[0], new[] { new PolyLineSegment(polyLinePoints.Skip(1), true) }, false) }));
            }

            result = Geometry.Combine(result, Geometry.Empty, GeometryCombineMode.Union, new TranslateTransform(columnStride * 0.5, rowStride * 0.5));
            return result;
        }

        public void SavePng(String filename) {
            double dontCare0, dontCare1;
            var graphGeometry = ToGeometry(out dontCare0, out dontCare1);
            var bmp = new RenderTargetBitmap((int)Math.Ceiling(graphGeometry.Bounds.Size.Width), (int)Math.Ceiling(graphGeometry.Bounds.Size.Height), 96, 96, PixelFormats.Pbgra32);
            // The light-weight visual element that will draw the geometries
            var viz = new DrawingVisual();
            using (var dc = viz.RenderOpen()) { // The DC lets us draw to the DrawingVisual directly
                dc.DrawGeometry(Brushes.Black, null, graphGeometry);
            } // the DC is closed as it falls out of the using statement

            // draw the visual on the bitmap
            bmp.Render(viz);

            // instantiate an encoder to save the file
            var pngEncoder = new PngBitmapEncoder();
            // add this bitmap to the encoders set of frames
            pngEncoder.Frames.Add(BitmapFrame.Create(bmp));

            // save the bitmap as an .png file
            using (var file = new FileStream(filename, FileMode.Create))
                pngEncoder.Save(file);
        }

        private void RemoveObject(Object objectToRemove, Dictionary<object, int> objectToColumnLookup) {
            int columnIndex;
            if (objectToColumnLookup.TryGetValue(objectToRemove, out columnIndex)) {
                Columns[columnIndex][Columns[columnIndex].IndexOf(objectToRemove)] = null;
            } else {
                throw new ArgumentOutOfRangeException("The specified object was not found");
            }
        }

        private void RemoveLine(int lineIndex,  Dictionary<object, int> objectToColumnLookup) {
            var line = _lines[lineIndex];
            foreach (LineWayPoint lwp in line.Where(o => o is LineWayPoint)) {
                RemoveObject(lwp, objectToColumnLookup);
            }
            _lines.RemoveAt(lineIndex);
        }

        private void RemoveObjectAndConnectedLinesFromGrid(Object objectToRemove) {
            var objectToColumnLookup = Enumerable.Range(0, Columns.Count).SelectMany(ci => Columns[ci].Where(o => o != null).Select(o => new Tuple<Object, int>(o, ci))).ToDictionary(t => t.Item1, t => t.Item2);
            RemoveObject(objectToRemove, objectToColumnLookup);
            var fromLineIndices = Enumerable.Range(0, _lines.Count).Where(i => _lines[i][0].Equals(objectToRemove)).OrderByDescending(i => i);
            foreach (var fromLineIndex in fromLineIndices) {
                RemoveLine(fromLineIndex, objectToColumnLookup);
            }
            var toLineIndices = Enumerable.Range(0, _lines.Count).Where(i => _lines[i][_lines[i].Length - 1].Equals(objectToRemove)).OrderByDescending(i => i);
            foreach (var toLineIndex in toLineIndices) {
                RemoveLine(toLineIndex, objectToColumnLookup);
            }
        }

        public void RemoveTransition(Transition transition) {
            _productNfa.TransitionFunction[transition.FromState][transition.InputSymbol].Remove(transition.ToState);
            RemoveObjectAndConnectedLinesFromGrid(transition);
        }

        public void ChangeTransitionProduct(Transition transition, Product newProduct) {
            if (transition.InputSymbol == newProduct) {
                return;
            }
            if (_productNfa.TransitionFunction[transition.FromState][newProduct].Contains(transition.ToState)) {
                throw new InvalidOperationException("This transition already exists");
            }
            _productNfa.TransitionFunction[transition.FromState][transition.InputSymbol].Remove(transition.ToState);
            _productNfa.TransitionFunction[transition.FromState][newProduct].Add(transition.ToState);
            var objectToColumnLookup = Enumerable.Range(0, Columns.Count).SelectMany(ci => Columns[ci].Where(o => o != null).Select(o => new Tuple<Object, int>(o, ci))).ToDictionary(t => t.Item1, t => t.Item2);
            var columnIndex = objectToColumnLookup[transition];
            var rowIndex = Columns[columnIndex].IndexOf(transition);
            var newTransition = new Transition(transition.FromState, newProduct, transition.ToState);
            Columns[columnIndex][rowIndex] = newTransition;
            foreach (var line in _lines) {
                for (int i = 0; i < line.Length; i++) {
                    if (line[i].Equals(transition)) {
                        line[i] = newTransition;
                    }
                }
            }
            var rightConnections = _columnConnectionsRight[columnIndex][transition];
            _columnConnectionsRight[columnIndex].TryRemove(transition);
            _columnConnectionsRight[columnIndex][newTransition].UnionWith(rightConnections);
            if (columnIndex > 0) {
                var connectionsFromLeft = _columnConnectionsRight[columnIndex - 1].Where(kvp => kvp.Value.Contains(transition)).Select(kvp => kvp.Key);
                foreach (var o in connectionsFromLeft) {
                    _columnConnectionsRight[columnIndex - 1][o].Remove(transition);
                    _columnConnectionsRight[columnIndex - 1][o].Add(newTransition);
                }
            }
            _formattedTexts = ComputeFormattedTexts();
        }

        public void AddTransition(State fromState, State toState, Product product) {
            if (_productNfa.TransitionFunction[fromState][product].Contains(toState)) {
                throw new InvalidOperationException("This transition already exists");
            }
            _productNfa.TransitionFunction[fromState][product].Add(toState);
            var objectToColumnLookup = Enumerable.Range(0, Columns.Count).SelectMany(ci => Columns[ci].Where(o => o != null).Select(o => new Tuple<Object, int>(o, ci))).ToDictionary(t => t.Item1, t => t.Item2);
            var fromStateColumn = objectToColumnLookup[fromState];
            var toStateColumn = objectToColumnLookup[toState];
            var transitionColumn = fromStateColumn < toStateColumn ? (fromStateColumn + toStateColumn) / 2 : fromStateColumn + 1;
            var transition = new Transition(fromState, product, toState);
            Columns[transitionColumn].Add(transition);
            CreateLine(fromStateColumn, fromState, transitionColumn, transition);
            CreateLine(transitionColumn, transition, toStateColumn, toState);
            _formattedTexts = ComputeFormattedTexts();
        }

        public void InsertNewState(int columnIndex, int rowIndex) {
            columnIndex = columnIndex - (columnIndex % 2) + 1; //round up to odd
            while (Columns.Count <= columnIndex + 2) { //+2 for space for a transition and feedback line
                Columns.Add(new List<object>());
            }
            var column = Columns[columnIndex];
            while(column.Count < rowIndex) {
                column.Add(null);
            }
            var value = _productNfa.States.Count;
            while(_productNfa.States.Any(s => s.Value == value)) {
                value++;
            }
            var newState = new State(value);
            column.Insert(rowIndex, newState);
            _productNfa.States.Add(newState);
        }

        public void RemoveState(State state) {
            foreach (var transition in Columns.SelectMany(l => l).OfType<Transition>().Where(t => t.ToState == state || t.FromState == state).ToArray()) {
                RemoveTransition(transition);
            }
            _productNfa.States.Remove(state);
            _productNfa.StartStates.Remove(state);
            _productNfa.AcceptStates.Remove(state);
            _productNfa.TransitionFunction.TryRemove(state);
            foreach (var inputSymbolAndToStates in _productNfa.TransitionFunction.SelectMany(fromStateAndInputSymbols => fromStateAndInputSymbols.Value)) {
                inputSymbolAndToStates.Value.Remove(state);
            }
            RemoveObjectAndConnectedLinesFromGrid(state);
        }
    }
}
