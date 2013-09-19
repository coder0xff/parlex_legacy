using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Collections.Generic.More;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Shapes;
using Common;
using parlex;
using Nfa = IDE.Nfa<parlex.Product, int>;
using State = IDE.Nfa<parlex.Product, int>.State;
using Transition = IDE.Nfa<parlex.Product, int>.Transition;

namespace IDE {
    public class RecognizerGraph {
        private Nfa _productNfa;
        private Dictionary<String, Geometry> _formattedTexts;
        private List<Object>[] _layers;
        private JaggedAutoDictionary<int, Object, HashSet<Object>> _layerConnectionsRight = new JaggedAutoDictionary<int, Object, HashSet<Object>>((i, o) => new HashSet<object>());
        private List<Object[]> _lines = new List<Object[]>();

        private Typeface _font;
        private double _emSize;

        class LineWayPoint {
            public LineWayPoint Right;
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
            var stateLayerAssignments = _productNfa.GetLayerAssignments().ToDictionary(kvp => kvp.Key, kvp => kvp.Value * 2 + 1); // * 2, because we need to leave layers in between and at the ends
            _layers = new List<Object>[stateLayerAssignments.Values.Max() + 3]; //1 for left of the first state, 2 for right of the last state (space for a transition and a feedback line)
            for (int initLayers = 0; initLayers < _layers.Length; initLayers++) _layers[initLayers] = new List<object>();
            foreach (var stateLayerAssignment in stateLayerAssignments) {
                _layers[stateLayerAssignment.Value].Add(stateLayerAssignment.Key);
            }
            foreach (var fromStateAndInputSymbols in _productNfa.TransitionFunction) {
                var fromState = fromStateAndInputSymbols.Key;
                foreach (var inputSymbolAndToStates in fromStateAndInputSymbols.Value) {
                    var inputSymbol = inputSymbolAndToStates.Key;
                    foreach (var toState in inputSymbolAndToStates.Value) {
                        var fromStateLayer = stateLayerAssignments[fromState];
                        var toStateLayer = stateLayerAssignments[toState];
                        var transitionLayer = fromStateLayer < toStateLayer ? (fromStateLayer + toStateLayer) / 2 : fromStateLayer + 1;
                        var transition = new Transition(fromState, inputSymbol, toState);
                        _layers[transitionLayer].Add(transition);
                        CreateLine(fromStateLayer, fromState, transitionLayer, transition);
                        CreateLine(transitionLayer, transition, toStateLayer, toState);
                    }
                }
            }
            var positionCount = (int)(_layers.Max(layer => layer.Count) * 1.5);
            foreach (var layer in _layers) {
                while (layer.Count < positionCount) layer.Add(null);
            }
        }

        private void CreateLine(int fromLayer, Object fromObject, int toLayer, Object toObject) {
            if (fromLayer == toLayer - 1) {
                CreateConnection(fromLayer, fromObject, toObject);
                _lines.Add(new[] {fromObject, toObject});
            } else if (fromLayer >= toLayer) {
                var fromWayPoint = new LineWayPoint();
                _layers[fromLayer + 1].Add(fromWayPoint);
                CreateConnection(fromLayer, fromObject, fromWayPoint);
                var toWayPoint = new LineWayPoint();
                _layers[toLayer - 1].Add(toWayPoint);
                CreateConnection(toLayer - 1, toWayPoint, toObject);
                CreateLine(toLayer - 1, toWayPoint, fromLayer + 1, fromWayPoint);
                var lineNumber = _lines.Count - 1;
                var changeLines = _lines[lineNumber].Reverse().ToList();
                changeLines.Insert(0, fromObject);
                changeLines.Add(toObject);
                _lines[lineNumber] = changeLines.ToArray();
            } else {
                var objectList = new List<Object>();
                objectList.Add(fromObject);
                var previousObject = fromObject;
                for (var i = fromLayer + 1; i < toLayer; i++) {
                    var wayPoint = new LineWayPoint();
                    objectList.Add(wayPoint);
                    _layers[i].Add(wayPoint);
                    CreateConnection(i - 1, previousObject, wayPoint);
                    previousObject = wayPoint;
                }
                CreateConnection(toLayer - 1, previousObject, toObject);
                objectList.Add(toObject);
                _lines.Add(objectList.ToArray());
            }
        }

        private void CreateConnection(int leftLayer, Object leftObject, Object rightObject) {
            var temp1 = leftObject as LineWayPoint;
            var temp2 = rightObject as LineWayPoint;
            if (temp1 != null && temp2 != null) {
                temp1.Right = temp2;
            }
            _layerConnectionsRight[leftLayer][leftObject].Add(rightObject);
            //_layerConnectionsLeft[leftLayer + 1][rightObject].Add(leftObject);
        }

        private double GetFitnessReciprocal() {
            double result = 0;
            var objectToIndexLookup = _layers.Select(list => Enumerable.Range(0, list.Count).Where(index => list[index] != null).ToDictionary(index => list[index], index => index)).ToArray();
            for (int layerIndex = 0; layerIndex < _layers.Length - 1; layerIndex++) {
                var connections = _layerConnectionsRight[layerIndex];
                var leftLookup = objectToIndexLookup[layerIndex];
                var rightLookup = objectToIndexLookup[layerIndex + 1];
                var orderedLineSegments = connections.Select(fromObjectAndToObjects => new Tuple<int, int[]>(leftLookup[fromObjectAndToObjects.Key], fromObjectAndToObjects.Value.Select(toObject => rightLookup[toObject]).OrderBy(i => i).ToArray())).OrderBy(t => t.Item1).ToArray();
                for (int i = 0; i < orderedLineSegments.Length - 1; i++) {
                    var crossCountTable = new int[_layers[layerIndex + 1].Count]; //the number of 'line1s' that are crossed where line2's right position is the index
                    {
                        var line1Group = orderedLineSegments[i];
                        var line1LeftPosition = line1Group.Item1;
                        var crossCount = line1Group.Item2.Length;
                        int tableIndex = 0;
                        foreach (var line1RightPosition in line1Group.Item2) {
                            result += Math.Abs(line1LeftPosition - line1RightPosition) * 0.1; //slightly favor vertical line segments instead of diagnals
                            for (; tableIndex < line1RightPosition; tableIndex++) {
                                crossCountTable[tableIndex] = crossCount;
                            }
                            crossCount--;
                        }
                    }
                    for (int j = i + 1; j < orderedLineSegments.Length; j++) {
                        var line2Group = orderedLineSegments[j];
                        foreach (var line2RightPosition in line2Group.Item2) {
                            result += crossCountTable[line2RightPosition] * 1 + (_layers.Length - layerIndex) * 0.1; //favor left side being untangled
                        }
                    }
                }
            }
            return result;
        }

        private RecognizerGraph(RecognizerGraph other) {
            _productNfa = other._productNfa;
            _formattedTexts = other._formattedTexts;
            _layers = other._layers.Select(list => list.ToList()).ToArray();
            _layerConnectionsRight = other._layerConnectionsRight;
            _lines = other._lines;
            _font = other._font;
            _emSize = other._emSize;
        }

        private void Mutate(int count) {
            var blockWidth = Rng.Next(_layers.Length - 1) + 1;
            var blockLeft = Rng.Next(0, _layers.Length - blockWidth);
            var blockRight = blockLeft + blockWidth;
            count = count * blockWidth / _layers.Length + 1;
            for (int i = 0; i < count; i++) {
                var layer = _layers[Rng.Next(blockLeft, blockRight + 1)];
                if (layer.Count < 2) {
                    continue;
                }
                var j = Rng.Next(layer.Count);
                var k = Rng.Next(layer.Count - 1);
                if (k >= j) k++;
                var temp = layer[j];
                layer[j] = layer[k];
                layer[k] = temp;
            }
            for (int smoothLayer = blockLeft; smoothLayer < _layers.Length - 1; smoothLayer++) {
                for (int position = _layers[0].Count - 1; position >= 0; position--) {
                    var obj = _layers[smoothLayer][position];
                    var asLineWayPoint = obj as LineWayPoint;
                    if (asLineWayPoint != null && asLineWayPoint.Right != null && _layers[smoothLayer + 1][position] == null) {
                        _layers[smoothLayer + 1].Remove(asLineWayPoint.Right);
                        _layers[smoothLayer + 1].Insert(position, asLineWayPoint.Right);
                    }
                }
            }
        }

        private RecognizerGraph[] MakeGeneration(int generationSize, int mutateCount) {
            return Enumerable.Range(0, generationSize).Select(i => {
                var child = new RecognizerGraph(this);
                child.Mutate(mutateCount);
                return child;
            }).ToArray();
        }

        private RecognizerGraph ChooseBest(IEnumerable<RecognizerGraph> generation) {
            return generation.Select(rg => new Tuple<double, RecognizerGraph>(rg.GetFitnessReciprocal(), rg)).OrderBy(t => t.Item1).First().Item2;
        }

        private void Arrange() {
            double mutateStrength = 1;
            const int generationSizePerObject = 10;

            var objectCount = _layers.Sum(layer => layer.Count);
            var generationSize = objectCount * generationSizePerObject;

            var noChangeCycleCounter = 0;
            var currentBest = this;
            var currentBestCount = GetFitnessReciprocal();
            SavePng("C:\\Users\\Brent\\Desktop\\RG\\Test_" + currentBestCount + ".png");
            var mutateCount = (int)Math.Ceiling(objectCount * mutateStrength * 0.5f);
            double decayMultiplier = 0.999;
            while (mutateStrength > 0.01) {
                var newBest = ChooseBest(MakeGeneration(generationSize, mutateCount));
                var newBestCount = newBest.GetFitnessReciprocal();
                if (newBestCount < currentBestCount) {
                    newBest.SavePng("C:\\Users\\Brent\\Desktop\\RG\\Test_" + newBestCount + ".png");
                    noChangeCycleCounter = 0;
                    currentBest = newBest;
                    currentBestCount = newBestCount;
                    decayMultiplier = 0.999;
                } else {
                    noChangeCycleCounter++;
                    System.Diagnostics.Debug.WriteLine(noChangeCycleCounter);
                    mutateStrength *= decayMultiplier;
                    decayMultiplier = Math.Max(decayMultiplier * 0.999, 0.99);
                }
                mutateCount = (int)Math.Ceiling(objectCount * mutateStrength * 0.5f);
            }
            _layers = currentBest._layers;
        }

        private Geometry ToGeometry() {
            const double margin = 5;
            var widestProductWidth = _formattedTexts.Values.Max(g => g.Bounds.Width);
            var highestProductHeight = _formattedTexts.Values.Max(g => g.Bounds.Height);
            var layerStride = widestProductWidth * 2;
            var positionStride = highestProductHeight * 4;
            Func<int, int, Point> getPos = (layerIndex, position) => new Point(layerStride * layerIndex, positionStride * position);

            var gg = new GeometryGroup();
            var objectToLayerLookup = Enumerable.Range(0, _layers.Length).SelectMany(layerIndex => _layers[layerIndex].Where(o => o != null).Select(o => new Tuple<Object, int>(o, layerIndex))).ToDictionary(t => t.Item1, t => t.Item2);
            var objectToPositionLookup = _layers.Select(list => Enumerable.Range(0, list.Count).Where(index => list[index] != null).ToDictionary(index => list[index], index => index)).ToArray();

            for (int layerIndex = 0; layerIndex < _layers.Length; layerIndex++) {
                var layer = _layers[layerIndex];
                //draw objects
                for (int position = 0; position < layer.Count; position++) {
                    var o = layer[position];
                    var center = getPos(layerIndex, position);
                    if (o is State) {
                        gg.Children.Add(new EllipseGeometry(new Point(layerStride * layerIndex, positionStride * position), margin, margin));
                        if (gg.Bounds == Rect.Empty) while(true) ;
                    } else if (o is Transition) {
                        var g = _formattedTexts[((Transition) o).InputSymbol.Title];
                        var outlineRect = new Rect(center.X - g.Bounds.Width * 0.5, center.Y - g.Bounds.Height * 0.5, g.Bounds.Width, g.Bounds.Height);
                        outlineRect.Inflate(margin, margin);
                        gg.Children.Add(new RectangleGeometry(outlineRect));
                        if (gg.Bounds == Rect.Empty) while (true) ;
                        var translatedText = new GeometryGroup();
                        translatedText.Children.Add(g);
                        translatedText.Transform = new TranslateTransform(center.X - g.Bounds.Width * 0.5, center.Y - g.Bounds.Height * 0.5);
                        //var translatedText = Geometry.Combine(g, Geometry.Empty, GeometryCombineMode.Union, new TranslateTransform(center.X - g.Bounds.Width * 0.5, center.Y - g.Bounds.Height * 0.5));
                        gg.Children.Add(translatedText);
                        if (gg.Bounds == Rect.Empty) while (true) ;
                    }
                }
            }

            //draw lines           
            Func<Object, bool, Point> getObjSidePosFunc = (o, side) => {
                //side == true is right side
                var layerIndex = objectToLayerLookup[o];
                var center = getPos(layerIndex, objectToPositionLookup[layerIndex][o]);
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
                var layerIndex = objectToLayerLookup[o];
                var center = getPos(layerIndex, objectToPositionLookup[layerIndex][o]);
                return center + new Vector(widestProductWidth * (side ? 0.7 : -0.7) + (side ? margin * 2 : margin * -2), 0);
            };

            var objectsThatHaveALineToTheRight = new HashSet<Object>(_layerConnectionsRight.Values.SelectMany(pairs => pairs.Keys));
            var objectsThatHaveALineToTheLeft = new HashSet<Object>(_layerConnectionsRight.Values.SelectMany(pairs => pairs.Values.SelectMany(s => s)));


            foreach (var objects in _lines) {
                var polyLinePoints = new List<Point>();
                var fromObject = objects[0];
                for (int toObjectIndex = 1; toObjectIndex < objects.Length; toObjectIndex++) {
                    var toObject = objects[toObjectIndex];
                    bool isFeedbackSegment = objectToLayerLookup[fromObject] > objectToLayerLookup[toObject];
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
