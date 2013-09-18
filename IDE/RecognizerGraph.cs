using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Common;
using parlex;
using Nfa = IDE.Nfa<parlex.Product, int>;
using State = IDE.Nfa<parlex.Product, int>.State;
using Transition = IDE.Nfa<parlex.Product, int>.Transition;

namespace IDE {
    /// <summary>
    /// Like a visualization of an NFA, except the states are just points where lines come together, and the transitions have labeled boxes
    /// </summary>
    public class RecognizerGraph {
        public readonly Dictionary<State, Point> StatePositions = new Dictionary<State, Point>();
        public readonly Dictionary<Transition, Rect> TransitionRects = new Dictionary<Transition, Rect>();
        public double lineThickness = 2;
        public double precedenceMargin = 50;
        public double transitionMargin = 50;
        public Typeface Font;
        public double EmSize;
        const double childScaleFactor = 0.95;

        public struct Line {
            public Transition Transition;
            public State State;
            public bool Direction; //true indicates state to transition
            public List<Point> PathPoints;

            public IEnumerable<Point> GetModifiedPathPoints(Rect transitionRect, double transitionHeight) {
                var rectCenterY = transitionRect.Top + transitionRect.Height * 0.5;
                if (Direction) {
                    PathPoints[PathPoints.Count - 1] = new Point(transitionRect.Left, rectCenterY);
                    var secondToLastPoint = PathPoints[PathPoints.Count - 2];
                    bool goUp = secondToLastPoint.Y < rectCenterY;
                    if (secondToLastPoint.X > transitionRect.Left - transitionHeight) {
                        return PathPoints.Take(PathPoints.Count - 1).Concat(new Point[] { new Point(transitionRect.Left - transitionHeight, rectCenterY + (goUp ? -transitionHeight : transitionHeight)), PathPoints[PathPoints.Count - 1] });
                    }
                } else {
                    PathPoints[0] = new Point(transitionRect.Right, rectCenterY);
                    var secondPoint = PathPoints[1];
                    bool goUp = secondPoint.Y < rectCenterY;
                    if (secondPoint.X < transitionRect.Right + transitionHeight) {
                        return PathPoints.Take(1).Concat(new Point[] { new Point(transitionRect.Right + transitionHeight, rectCenterY + (goUp ? -transitionHeight : transitionHeight)) }).Concat(PathPoints.Skip(1));
                    }
                }
                return PathPoints;
            } 

            public PathGeometry GetWidenedPathGeometry(Rect transitionRect, double transitionHeight, bool excludeFirst, bool excludeLast, double lineThickness) {
                var p = new Pen(Brushes.Black, lineThickness);
                var temp0 = GetModifiedPathPoints(transitionRect, transitionHeight);
                var temp1 = excludeLast ? temp0.Take(PathPoints.Count - 1) : temp0;
                var temp2 = excludeFirst ? temp1.Skip(1) : temp1;
                return new PathGeometry(new[] {new PathFigure(temp2.First(), new PathSegment[] {new PolyLineSegment(temp2.Skip(1), true)}, false)}).GetWidenedPathGeometry(p).GetFlattenedPathGeometry();
            }

            public PathGeometry GetPathGeometry(Rect transitionRect, double transitionHeight) {
                var temp = GetModifiedPathPoints(transitionRect, transitionHeight);
                return new PathGeometry(new[] {new PathFigure(temp.First(), new PathSegment[] {new PolyLineSegment(temp.Skip(1), true)}, false)});
            }

            public double GetPathLength(Rect transitionRect, double transitionHeight) {
                return GetPathGeometry(transitionRect, transitionHeight).GetLength();
            }

            public Line Mutate(double strength, double maxHorizontalShift, double maxVerticalShift, Dictionary<State, Point> statePositions, Dictionary<Transition, Rect> transitionRects) {
                Line result = new Line();
                result.Transition = Transition;
                result.State = State;
                result.Direction = Direction;
                result.PathPoints = new List<Point>(PathPoints);
                var transitionRect = transitionRects[Transition];
                result.PathPoints[0] = Direction ? statePositions[State] : new Point(transitionRect.Left, transitionRect.Top + transitionRect.Height * 0.5);
                result.PathPoints[result.PathPoints.Count - 1] = !Direction ? statePositions[State] : new Point(transitionRect.Right, transitionRect.Top + transitionRect.Height * 0.5);
                if (Rng.NextDouble() < strength * 10) { //add a point
                    int pointIndex = Rng.Next(result.PathPoints.Count - 1);
                    result.PathPoints.Insert(pointIndex, result.PathPoints[pointIndex]);
                }
                if (Rng.NextDouble() < strength * 10 && result.PathPoints.Count > 2) { //remove a point
                    int pointIndex = Rng.Next(1, result.PathPoints.Count - 1); //don't remove the first or last point
                    result.PathPoints.RemoveAt(pointIndex);
                }
                for (int i = 1; i < result.PathPoints.Count - 1; i++) {
                    var temp = result.PathPoints[i];
                    temp = new Point(temp.X * childScaleFactor + Rng.NextDouble(-maxHorizontalShift, maxHorizontalShift), temp.Y + Rng.NextDouble(-maxVerticalShift, maxVerticalShift));
                    result.PathPoints[i] = temp;
                }
                return result;
            }
        }

        public readonly List<Line> Lines = new List<Line>();

        #region fitness functions
        private double GetFitnessReciprocal(double transitionHeight, bool printStats) {
            const float lineIntersectLineCost = 10;
            const float emptyAreaRatioCostMultiplier = 5;
            const float lineIntersectBoxCost = 250;
            const float boxIntersectsBoxCost = 100;
            const float pointCost = 1;
            const float lineLengthCost = .5f;
            const float badPrecedenceCost = 100;
            const float lopSidednessCostMultiplier = 3;
            var fullLinePaths = new AutoDictionary<Line, Geometry>(line => line.GetWidenedPathGeometry(TransitionRects[line.Transition], transitionHeight, false, false, lineThickness));
            var excludeFirstLinePaths = new AutoDictionary<Line, Geometry>(line => line.GetWidenedPathGeometry(TransitionRects[line.Transition], transitionHeight, true, false, lineThickness));
            var excludeLastLinePaths = new AutoDictionary<Line, Geometry>(line => line.GetWidenedPathGeometry(TransitionRects[line.Transition], transitionHeight, false, true, lineThickness));
            var transitionRectPaths = new AutoDictionary<Transition, Geometry>(transition => new RectangleGeometry(Rect.Inflate(TransitionRects[transition], transitionMargin, transitionMargin)).GetOutlinedPathGeometry());
            var transitionRectShrunkPaths = new AutoDictionary<Transition, Geometry>(transition => new RectangleGeometry(Rect.Inflate(TransitionRects[transition], -2, -2)).GetOutlinedPathGeometry()); //used for testing connected lines
            var lineIntersectLineTotal = LineIntersectLineCount(excludeFirstLinePaths, excludeLastLinePaths, fullLinePaths) * lineIntersectLineCost;
            var lineIntersectBoxTotal = LineIntersectBoxCount(fullLinePaths, transitionRectPaths, transitionRectShrunkPaths) * lineIntersectBoxCost;
            var boxIntersectBoxTotal = BoxIntersectBoxCount(transitionRectPaths) * boxIntersectsBoxCost;
            var pointTotal = PointCount() * pointCost;
            var lineLengthTotal = LineLengths(transitionHeight) * lineLengthCost;
            var badPrecedenceTotal = BadPrecedenceCount() * badPrecedenceCost;
            var emptyAreaTotal = GetDrawingEmptyRatio(fullLinePaths, transitionRectPaths) * emptyAreaRatioCostMultiplier;
            var lopSidednessTotal = GetTransitionLopSidedness(transitionHeight) * lopSidednessCostMultiplier;
            var total = lineIntersectLineTotal + lineIntersectBoxTotal + boxIntersectBoxTotal + pointTotal + lineLengthTotal + badPrecedenceTotal + emptyAreaTotal + lopSidednessTotal;
            if (printStats) {
                System.Diagnostics.Debug.WriteLine("");
                System.Diagnostics.Debug.WriteLine("Score: " + total);
                System.Diagnostics.Debug.WriteLine("LineIntersectLineCount = " + lineIntersectLineTotal / lineIntersectLineCost + " (" + lineIntersectLineTotal + ")");
                System.Diagnostics.Debug.WriteLine("LineIntersectBoxCount = " + lineIntersectBoxTotal / lineIntersectBoxCost + " (" + lineIntersectBoxTotal + ")");
                System.Diagnostics.Debug.WriteLine("BoxIntersectBoxCount = " + boxIntersectBoxTotal / boxIntersectsBoxCost + " (" + boxIntersectBoxTotal + ")");
                System.Diagnostics.Debug.WriteLine("PointCount = " + pointTotal / pointCost + " (" + pointTotal + ")");
                System.Diagnostics.Debug.WriteLine("LineLengths = " + lineLengthTotal / lineLengthCost + " (" + lineLengthTotal + ")");
                System.Diagnostics.Debug.WriteLine("BadPrecedenceCount = " + badPrecedenceTotal / badPrecedenceCost + " (" + badPrecedenceTotal + ")");
                System.Diagnostics.Debug.WriteLine("DrawingEmptyRatio = " + emptyAreaTotal / emptyAreaRatioCostMultiplier + " (" + emptyAreaTotal + ")");
            }
            return total;
        }

        private int LineIntersectLineCount(AutoDictionary<Line, Geometry> excludeFirstLinePaths, AutoDictionary<Line, Geometry> excludeLastLinePaths, AutoDictionary<Line, Geometry> fullLinePaths) {
            int lineIntersectionCount = 0;
            for (int i = 0; i < Lines.Count; i++) {
                var line1 = Lines[i];
                for (int j = i + 1; j < Lines.Count; j++) {
                    var line2 = Lines[j];
                    bool sameState = line1.State == line2.State;
                    bool sameTransition = false; // line1.Transition == line2.Transition;
                    bool exclude1First = line1.Direction ? sameState : sameTransition;
                    bool exclude1Last = line1.Direction ? sameTransition : sameState;
                    bool exclude2First = line2.Direction ? sameState : sameTransition;
                    bool exclude2Last = line2.Direction ? sameTransition : sameState;
                    var geometry1 = exclude1First ? excludeFirstLinePaths[line1] : exclude1Last ? excludeLastLinePaths[line1] : fullLinePaths[line1];
                    var geometry2 = exclude2First ? excludeFirstLinePaths[line2] : exclude2Last ? excludeLastLinePaths[line2] : fullLinePaths[line2];
                    if (geometry1.FillContainsWithDetail(geometry2) != IntersectionDetail.Empty) {
                        lineIntersectionCount++;
                    }
                }
            }
            return lineIntersectionCount;
        }
        private int LineIntersectBoxCount(AutoDictionary<Line, Geometry> fullLinePaths, AutoDictionary<Transition, Geometry> transitionRectPaths, AutoDictionary<Transition, Geometry> transitionRectShrunkPaths) {
            int boxIntersectionCount = 0;
            for (int i = 0; i < Lines.Count; i++) {
                var line1 = Lines[i];
                foreach (var transition in TransitionRects.Keys) {
                    if (transition == line1.Transition) {
                        if (fullLinePaths[line1].FillContainsWithDetail(transitionRectShrunkPaths[transition]) != IntersectionDetail.Empty) {
                            boxIntersectionCount++;
                        }
                    } else {
                        if (fullLinePaths[line1].FillContainsWithDetail(transitionRectPaths[transition]) != IntersectionDetail.Empty) {
                            boxIntersectionCount++;
                        }
                    }
                }
            }
            return boxIntersectionCount;
        }

        private int BoxIntersectBoxCount(AutoDictionary<Transition, Geometry> transitionRectPaths) {
            var orderedTransitions = TransitionRects.Keys.ToArray();
            int boxIntersectBoxCount = 0;
            for (int i = 0; i < orderedTransitions.Length; i++) {
                for (int j = i + 1; j < orderedTransitions.Length; j++) {
                    if (transitionRectPaths[orderedTransitions[i]].FillContainsWithDetail(transitionRectPaths[orderedTransitions[j]]) != IntersectionDetail.Empty) {
                        boxIntersectBoxCount++;
                    }
                }
            }
            return boxIntersectBoxCount;
        }

        private int PointCount() {
            return Lines.Sum(line => line.PathPoints.Count);
        }

        private int BadPrecedenceCount() {            
            int badPrecedenceCount = 0;
            foreach (var transitionRect in TransitionRects) {
                var transition = transitionRect.Key;
                var rect = transitionRect.Value;
                var leftLimit = StatePositions[transition.FromState].X + precedenceMargin;
                var rightLimit = StatePositions[transition.ToState].X - precedenceMargin;
                if (rightLimit > leftLimit && (rect.Left < leftLimit || rect.Right > rightLimit)) {
                    badPrecedenceCount++;
                } else if (leftLimit > rightLimit && rect.Left < leftLimit) { //feedback arcs should be to the right of their from state
                    badPrecedenceCount++;
                }
            }
            return badPrecedenceCount;
        }

        private double LineLengths(double transitionHeight) {
            return Lines.Sum(line => line.GetPathLength(TransitionRects[line.Transition], transitionHeight));
        }

        private static double GetDrawingEmptyRatio(AutoDictionary<Line, Geometry> fullLinePaths, AutoDictionary<Transition, Geometry> transitionRectPaths) {
            PathGeometry allGeometry = new PathGeometry();
            var p = new Pen(Brushes.Black, 1);
            allGeometry = fullLinePaths.Values.Aggregate(allGeometry, (current, geometry) => Geometry.Combine(current, geometry, GeometryCombineMode.Union, null));
            allGeometry = transitionRectPaths.Values.Aggregate(allGeometry, (current, geometry) => Geometry.Combine(current, geometry, GeometryCombineMode.Union, null));
            var boundsSize = allGeometry.Bounds.Size;
            return 1 - (allGeometry.GetArea() / (boundsSize.Width * boundsSize.Height));
        }

        private double GetTransitionLopSidedness(double transitionHeight) {
            var minLengths = new AutoDictionary<Transition, double>(t => double.PositiveInfinity);
            var maxLengths = new AutoDictionary<Transition, double>(t => double.NegativeInfinity);
            foreach (var line in Lines) {
                var pathLength = line.GetPathLength(TransitionRects[line.Transition], transitionHeight);
                minLengths[line.Transition] = Math.Min(minLengths[line.Transition], pathLength);
                maxLengths[line.Transition] = Math.Max(maxLengths[line.Transition], pathLength);
            }
            double result = maxLengths.Sum(keyValuePair => keyValuePair.Value / minLengths[keyValuePair.Key]);
            result += maxLengths.Values.Max() / minLengths.Values.Min();
            return result;
        }

        #endregion
        Size GetSize() {
            double left, top, right, bottom;
            left = right = StatePositions.First().Value.X;
            top = bottom = StatePositions.First().Value.Y;
            foreach (var statePosition in StatePositions.Values) {
                left = Math.Min(left, statePosition.X);
                top = Math.Min(top, statePosition.Y);
                right = Math.Max(right, statePosition.X);
                bottom = Math.Max(bottom, statePosition.Y);
            }
            foreach (var rect in TransitionRects.Values) {
                left = Math.Min(left, rect.Left - transitionMargin);
                top = Math.Min(top, rect.Top - transitionMargin);
                right = Math.Max(right, rect.Right + transitionMargin);
                bottom = Math.Max(bottom, rect.Bottom + transitionMargin);
            }
            return new Size(right - left, bottom - top);
        }

        RecognizerGraph Mutate(double strength) {
            Size currentSize = GetSize();
            double maxHorizontalShift = currentSize.Width * strength;
            double maxVerticalShift = currentSize.Height * strength;
            var result = new RecognizerGraph();
            foreach (var statePosition in StatePositions) {
                var newPoint = new Point(statePosition.Value.X + Rng.NextDouble(-maxHorizontalShift, maxHorizontalShift) * 0.02, statePosition.Value.Y + Rng.NextDouble(-maxVerticalShift, maxVerticalShift) * 0.02);
                result.StatePositions[statePosition.Key] = newPoint;
            }
            foreach (var transitionRect in TransitionRects) {
                double moveFactor = Rng.NextDouble() < 0.1 ? 0.4 : 0;
                var newRect = new Rect(new Point(transitionRect.Value.Left * childScaleFactor + Rng.NextDouble(-maxHorizontalShift, maxHorizontalShift) * moveFactor, transitionRect.Value.Top * childScaleFactor + Rng.NextDouble(-maxVerticalShift, maxVerticalShift) * moveFactor), transitionRect.Value.Size);
                result.TransitionRects[transitionRect.Key] = newRect;
            }
            result.lineThickness = lineThickness;
            result.precedenceMargin = precedenceMargin;
            result.transitionMargin = transitionMargin;
            foreach (var line in Lines) {
                result.Lines.Add(line.Mutate(strength, maxHorizontalShift, maxVerticalShift, result.StatePositions, result.TransitionRects));
            }
            return result;
        }

        RecognizerGraph[] NextGeneration(int count, double mutateStrength) {
            RecognizerGraph[] results = new RecognizerGraph[count];
            for (var i = 0; i < count; i++) {
                results[i] = Mutate(mutateStrength);
            }
            return results;
        }

        RecognizerGraph SelectFittestChildOrSelf(double transitionHeight, RecognizerGraph[] generation) {
            bool dontCare;
            int[] dontCare2;
            double currentBestScore = GetFitnessReciprocal(transitionHeight, false);
            var l = new ReaderWriterLockSlim();
            var currentBest = this; //if this is the best one, keep it!
            Parallel.ForEach(generation, graph => {
                double score = graph.GetFitnessReciprocal(transitionHeight, false);
                l.EnterUpgradeableReadLock();
                if (score < currentBestScore) { //low scores are desirable
                    l.EnterWriteLock();
                    currentBestScore = score;
                    currentBest = graph;
                    l.ExitWriteLock();
                }
                l.ExitUpgradeableReadLock();
            });
            return currentBest;
        }

        static RecognizerGraph Arrange(RecognizerGraph original, double transitionHeight) {
            original = original.Mutate(0);
            const int generationSize = 25;
            double currentMutateStrength = 1;
            RecognizerGraph currentFittest = original;
            double currentScore = original.GetFitnessReciprocal(transitionHeight, false);
            currentFittest.SavePng(transitionHeight, "C:\\Users\\Brent\\Desktop\\RG\\Test_" + (int)currentScore + ".png");
            double decayMultiplier = 0.995;
            while (currentMutateStrength > 0.01) {
                var generation = currentFittest.NextGeneration(generationSize, currentMutateStrength);
                currentFittest = currentFittest.SelectFittestChildOrSelf(transitionHeight, generation);
                double newScore = currentFittest.GetFitnessReciprocal(transitionHeight, false);
                if (newScore >= currentScore) {
                    // lower is better, so if we didn't find any better, lessen the strength
                    currentMutateStrength *= decayMultiplier;
                    decayMultiplier = Math.Max(decayMultiplier * .99, 0.8);
                } else {
                    decayMultiplier = 0.995;
                    foreach (var line in currentFittest.Lines) { //Remove any line points that aren't improving the score
                        for (int i = 1; i < line.PathPoints.Count - 1; i++) {
                            var savePoint = line.PathPoints[i];
                            line.PathPoints.RemoveAt(i);
                            double scoreWithPointRemoved = currentFittest.GetFitnessReciprocal(transitionHeight, false);
                            if (scoreWithPointRemoved < currentScore) {
                                i--;
                                currentScore = scoreWithPointRemoved;
                            } else {
                                line.PathPoints.Insert(i, savePoint);
                            }
                        }
                    }
                    currentFittest.SavePng(transitionHeight, "C:\\Users\\Brent\\Desktop\\RG\\Test_" + (int)newScore + ".png");
                    //currentFittest.GetFitnessReciprocal(true);
                }
                currentScore = newScore;
            }
            return currentFittest;
        }

        private RecognizerGraph() { }

        public RecognizerGraph(Nfa productNfa, Typeface font, double emSize) {
            Font = font;
            EmSize = emSize;
            var allSymbols = productNfa.TransitionFunction.SelectMany(fromStateAndInputSymbolsAndToStates => fromStateAndInputSymbolsAndToStates.Value).Select(inputSymbolAndToStates => inputSymbolAndToStates.Key).Distinct();
            var transitionTextSizes = allSymbols.ToDictionary(symbol => symbol, symbol => {
                var t = new FormattedText(symbol.Title, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, font, emSize, Brushes.Black);
                return new Size(t.Width, t.Height);
            });
            var greatestTransitionWidth = transitionTextSizes.Values.Select(v => v.Width).Max();
            var greatestTransitionHeight = transitionTextSizes.Values.Select(v => v.Height).Max();
            var layerAssignments = productNfa.GetLayerAssignments();
            var counterPerLayer = new AutoDictionary<int, int>(i => 0);
            foreach (var state in productNfa.States) {
                var layer = layerAssignments[state];
                StatePositions.Add(state, new Point(layer * greatestTransitionWidth * 3, (counterPerLayer[layer] = counterPerLayer[layer] + 1) * greatestTransitionHeight * 10));
            }
            foreach (var fromStateAndInputSymbolsAndToStates in productNfa.TransitionFunction) {
                var fromState = fromStateAndInputSymbolsAndToStates.Key;
                var inputSymbolsAndToStates = fromStateAndInputSymbolsAndToStates.Value;
                foreach (var inputSymbolAndToStates in inputSymbolsAndToStates) {
                    var inputSymbol = inputSymbolAndToStates.Key;
                    var textSize = transitionTextSizes[inputSymbol];
                    var toStates = inputSymbolAndToStates.Value;
                    foreach (var toState in toStates) {
                        var transition = new Transition(fromState, toState);
                        var temp = StatePositions[toState] + (StatePositions[fromState] - StatePositions[toState] - new Vector(textSize.Width, textSize.Height)) * 0.5;
                        TransitionRects[transition] = new Rect(temp.X, temp.Y, textSize.Width, textSize.Height);
                        Lines.Add(new Line {Direction = true, State = fromState, PathPoints = new List<Point>(new[] {new Point(), new Point()}), Transition = transition});
                        Lines.Add(new Line {Direction = false, State = toState, PathPoints = new List<Point>(new[] {new Point(), new Point()}), Transition = transition});
                    }
                }
            }
            var arrangedCopy = Arrange(this, greatestTransitionHeight);
            StatePositions = arrangedCopy.StatePositions;
            TransitionRects = arrangedCopy.TransitionRects;
            Lines = arrangedCopy.Lines;
        }

        public Geometry ToGeometry(double transitionHeight) {
            var result = new GeometryGroup();
            foreach (var geometry in Lines.Select(line => line.GetPathGeometry(TransitionRects[line.Transition], transitionHeight))) {
                result.Children.Add(geometry);
            }
            foreach (var transitionRect in TransitionRects.Values) {
                result.Children.Add(new RectangleGeometry(transitionRect));
            }
            foreach (var point in StatePositions.Values) {
                result.Children.Add(new EllipseGeometry(point, 4, 4));
            }
            result.Transform = new TranslateTransform(-result.Bounds.Left, -result.Bounds.Top);
            return result;
        }

        public void SavePng(double transitionHeight, String filename) {
            var graphGeometry = ToGeometry(transitionHeight);
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

    public static class GeometryExtensions {
        public static double GetLength(this System.Windows.Media.Geometry geo) {
            PathGeometry path = geo.GetFlattenedPathGeometry();

            double length = 0.0;

            foreach (PathFigure pf in path.Figures) {
                Point start = pf.StartPoint;

                foreach (PathSegment seg in pf.Segments) {
                    if (seg is LineSegment) {
                        length += Distance(start, ((LineSegment) seg).Point);
                    } else foreach (Point point in ((PolyLineSegment)seg).Points) {
                        length += Distance(start, point);
                        start = point;
                    }
                }
            }

            return length;
        }

        private static double Distance(Point p1, Point p2) {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }
    }
}
