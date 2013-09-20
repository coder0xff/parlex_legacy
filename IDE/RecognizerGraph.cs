using Common;
using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Nfa = IDE.Nfa<parlex.Product, int>;
using State = IDE.Nfa<parlex.Product, int>.State;
using Transition = IDE.Nfa<parlex.Product, int>.Transition;

namespace IDE {
    public class RecognizerGraph {
        private Nfa _productNfa;
        private Dictionary<String, Geometry> _formattedTexts;
        private List<Object>[] _columns;
        private JaggedAutoDictionary<int, Object, HashSet<Object>> _columnConnectionsRight = new JaggedAutoDictionary<int, Object, HashSet<Object>>((i, o) => new HashSet<object>());
        private List<Object[]> _lines = new List<Object[]>();
        private int _objectCount;
        private DiscreteProbabilityDistribution<int> _mutateColumnSelectionProbabilityDistribution; //each column should have a weight for its likelihood for being the target of a mutate
        private int _columnCount;

        private Typeface _font;
        private double _emSize;

        class LineWayPoint {
        }

        public RecognizerGraph(Nfa productNfa, Typeface font, double emSize) {
            _font = font;
            _emSize = emSize;
            _productNfa = productNfa;
            Create();
            Arrange();
        }

        private Dictionary<String, Geometry> ComputeFormattedTexts() {
            var allSymbols = _productNfa.TransitionFunction.SelectMany(fromStateAndInputSymbolsAndToStates => fromStateAndInputSymbolsAndToStates.Value).Select(inputSymbolAndToStates => inputSymbolAndToStates.Key).Distinct();
            return allSymbols.Select(s => s.Title).Distinct().ToDictionary(title => title, title => new FormattedText(title, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _font, _emSize, Brushes.Black).BuildGeometry(new Point()));
        }

        private void Create() {
            _formattedTexts = ComputeFormattedTexts();
            var stateColumnAssignments = _productNfa.GetLayerAssignments().ToDictionary(kvp => kvp.Key, kvp => kvp.Value * 2 + 1); // * 2, because we need to leave columns in between and at the ends
            _columnCount = stateColumnAssignments.Values.Max() + 3; //1 for left of the first state, 2 for right of the last state (space for a transition and a feedback line)
            _columns = new List<Object>[_columnCount]; 
            for (int initColumns = 0; initColumns < _columnCount; initColumns++) _columns[initColumns] = new List<object>();
            foreach (var stateColumnAssignment in stateColumnAssignments) {
                _columns[stateColumnAssignment.Value].Add(stateColumnAssignment.Key);
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
                        _columns[transitionColumn].Add(transition);
                        CreateLine(fromStateColumn, fromState, transitionColumn, transition);
                        CreateLine(transitionColumn, transition, toStateColumn, toState);
                    }
                }
            }

            _mutateColumnSelectionProbabilityDistribution = new DiscreteProbabilityDistribution<int>(Enumerable.Range(0, _columnCount).Select(i => new Tuple<int, double>(i, _columns[i].Count * _columns[i].Count)));
            _objectCount = _columns.Sum(c => c.Count);

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
                _columns[fromColumn + 1].Add(fromWayPoint);
                CreateConnection(fromColumn, fromObject, fromWayPoint);
                var toWayPoint = new LineWayPoint();
                _columns[toColumn - 1].Add(toWayPoint);
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
                    _columns[i].Add(wayPoint);
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

        private double GetFitnessReciprocal(int leftColumnIndex = -1, int rightColumnIndex = -1) {
            if (leftColumnIndex == -1) leftColumnIndex = 0;
            if (rightColumnIndex == -1) rightColumnIndex = _columnCount - 1;
            double result = 0;
            var objectToIndexLookup = _columns.Select(list => Enumerable.Range(0, list.Count).Where(index => list[index] != null).ToDictionary(index => list[index], index => index)).ToArray();
            for (int columnIndex = leftColumnIndex; columnIndex < rightColumnIndex; columnIndex++) {
                var connections = _columnConnectionsRight[columnIndex];
                var leftLookup = objectToIndexLookup[columnIndex];
                var rightLookup = objectToIndexLookup[columnIndex + 1];
                var orderedLineSegments = connections.Select(fromObjectAndToObjects => new Tuple<int, int[]>(leftLookup[fromObjectAndToObjects.Key], fromObjectAndToObjects.Value.Select(toObject => rightLookup[toObject]).OrderBy(i => i).ToArray())).OrderBy(t => t.Item1).ToArray();
                for (int i = 0; i < orderedLineSegments.Length - 1; i++) {
                    var crossCountTable = new int[_columns[columnIndex + 1].Count]; //the number of 'line1s' that are crossed where line2's right row is the index
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

        private RecognizerGraph(RecognizerGraph other) {
            _productNfa = other._productNfa;
            _formattedTexts = other._formattedTexts;
            _columns = other._columns.Select(list => list.ToList()).ToArray();
            _columnConnectionsRight = other._columnConnectionsRight;
            _columnCount = other._columnCount;
            //_rowCount = other._rowCount;
            _mutateColumnSelectionProbabilityDistribution = other._mutateColumnSelectionProbabilityDistribution;
            _lines = other._lines;
            _font = other._font;
            _emSize = other._emSize;
        }

        private void Mutate(int count) {
            for (int i = 0; i < count; i++) {
                var column = _columns[_mutateColumnSelectionProbabilityDistribution.Next()];
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

        private void Arrange() {
            const double mutatesPerObject = 1;
            const double decayMax = 0.9999999;
            var decayMin = Math.Pow(0.95, 1.0 / _objectCount);
            var decayDecay = Math.Pow(0.95, 1.0 / (_objectCount * _objectCount));

            double mutateStrength = 1;
            var currentBest = this;
            var currentBestCount = GetFitnessReciprocal();
            SavePng("C:\\Users\\Brent\\Desktop\\RG\\Test_" + currentBestCount + ".png");
            var mutateCount = (int)Math.Ceiling(_objectCount * mutatesPerObject * mutateStrength);
            double decay = 0.9999;
            while (true) {
                System.Diagnostics.Debug.WriteLine(mutateStrength);
                var next = new RecognizerGraph(currentBest);
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
                mutateCount = (int)Math.Ceiling(_objectCount * mutatesPerObject * mutateStrength);
                if (mutateCount == 1) {
                    break;
                }
            }
            _columns = currentBest._columns;
            SavePng("C:\\Users\\Brent\\Desktop\\RG\\Test_" + currentBestCount + ".png");
        }

        private Geometry ToGeometry() {
            const double margin = 5;
            var widestProductWidth = _formattedTexts.Values.Max(g => g.Bounds.Width);
            var highestProductHeight = _formattedTexts.Values.Max(g => g.Bounds.Height);
            var columnStride = widestProductWidth * 2;
            var rowStride = highestProductHeight * 4;
            Func<int, int, Point> getPos = (columnIndex, row) => new Point(columnStride * columnIndex, rowStride * row);

            var gg = new GeometryGroup();
            var objectToColumnLookup = Enumerable.Range(0, _columnCount).SelectMany(columnIndex => _columns[columnIndex].Where(o => o != null).Select(o => new Tuple<Object, int>(o, columnIndex))).ToDictionary(t => t.Item1, t => t.Item2);
            var objectToRowLookup = _columns.Select(list => Enumerable.Range(0, list.Count).Where(index => list[index] != null).ToDictionary(index => list[index], index => index)).ToArray();

            for (int columnIndex = 0; columnIndex < _columnCount; columnIndex++) {
                var column = _columns[columnIndex];
                //draw objects
                for (int row = 0; row < column.Count; row++) {
                    var o = column[row];
                    var center = getPos(columnIndex, row);
                    if (o is State) {
                        gg.Children.Add(new EllipseGeometry(new Point(columnStride * columnIndex, rowStride * row), margin, margin));
                    } else if (o is Transition) {
                        var g = _formattedTexts[((Transition) o).InputSymbol.Title];
                        var outlineRect = new Rect(center.X - g.Bounds.Width * 0.5, center.Y - g.Bounds.Height * 0.5, g.Bounds.Width, g.Bounds.Height);
                        outlineRect.Inflate(margin, margin);
                        gg.Children.Add(new RectangleGeometry(outlineRect));
                        var translatedText = new GeometryGroup();
                        translatedText.Children.Add(g);
                        translatedText.Transform = new TranslateTransform(center.X - g.Bounds.Width * 0.5, center.Y - g.Bounds.Height * 0.5);
                        gg.Children.Add(translatedText);
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
                    return center + new Vector(_formattedTexts[transition.InputSymbol.Title].Bounds.Width * 0.5 + margin, 0) * (side ? 1 : -1);
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
                gg.Children.Add(new PathGeometry(new[] { new PathFigure(polyBezierPoints[0], new[] { new PolyBezierSegment(polyBezierPoints.Skip(1), true) }, false) }));
                //gg.Children.Add(new PathGeometry(new[] { new PathFigure(polyLinePoints[0], new[] { new PolyLineSegment(polyLinePoints.Skip(1), true) }, false) }));
            }


            gg.Transform = new TranslateTransform(-gg.Bounds.Left, -gg.Bounds.Top);
            return gg;
        }

        public void SavePng(String filename) {
            var graphGeometry = ToGeometry();
            var bmp = new RenderTargetBitmap((int)Math.Ceiling(graphGeometry.Bounds.Size.Width), (int)Math.Ceiling(graphGeometry.Bounds.Size.Height), 96, 96, PixelFormats.Pbgra32);
            // The light-weight visual element that will draw the geometries
            var viz = new DrawingVisual();
            using (var dc = viz.RenderOpen()) { // The DC lets us draw to the DrawingVisual directly
                dc.DrawGeometry(null, new Pen(Brushes.Black, 2), graphGeometry);
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
    }
}
