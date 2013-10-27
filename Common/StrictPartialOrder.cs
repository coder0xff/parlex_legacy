using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic.More;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.More;
using System.Text;

namespace System.Collections.Generic.More {
    public class StrictPartialOrder<T> : IComparer<T>, IEnumerable<StrictPartialOrder<T>.LessThanPair> {
        private readonly BulkObservableCollection<LessThanPair> _definingEdges;
 
        private class Node {
            internal readonly HashSet<Node> SubsequentVertices = new HashSet<Node>();

            internal void ApplyTransitivity() {
                ApplyTransitivityRecursively(this);
            }

            private void ApplyTransitivityRecursively(Node node) {
                foreach (var adjacentNode in node.SubsequentVertices) {
                    SubsequentVertices.Add(adjacentNode);
                    ApplyTransitivityRecursively(adjacentNode);
                }
            }
        }

        public class LessThanPair : Tuple<T, T> {
            public LessThanPair(T item1, T item2) : base(item1, item2) {}
        }

        public ICollection<LessThanPair> DefiningEdges {
            get { return _definingEdges; }
        }

        private void CheckForCyclicity() {
            var ancestors = new Stack<Node>();
            foreach (var node in _valueToNode.Left) {
                CheckForCyclicityRecursively(node.Value, ancestors);
            }
        }

        private static void CheckForCyclicityRecursively(Node currentNode, Stack<Node> ancestors) {
            if (ancestors.Contains(currentNode)) {
                var nodeNames = new StringBuilder(currentNode.ToString());
                while(ancestors.Peek() != currentNode) {
                    nodeNames.Append(" from ");
                    nodeNames.Append(ancestors.Pop());
                }
                nodeNames.Append(currentNode);
                throw new ApplicationException("Cyclical order detected: " + nodeNames);
            }

            ancestors.Push(currentNode);
            foreach (var adjacentNode in currentNode.SubsequentVertices) {
                CheckForCyclicityRecursively(adjacentNode, ancestors);
            }
            ancestors.Pop();
        }

        private void ApplyTransitivity() {
            foreach (var node in _valueToNode.Left.Values) {
                node.ApplyTransitivity();
            }
        }

        private Bimap<T, Node> _valueToNode;

        internal StrictPartialOrder(IEnumerable<LessThanPair> edges) {
            _definingEdges = new BulkObservableCollection<LessThanPair>();
            _definingEdges.CollectionChanged += _definingEdges_CollectionChanged;
            var singleEnumeration = edges as IList<LessThanPair> ?? edges.ToList();
            _definingEdges.AddRange(singleEnumeration);
        }

        void _definingEdges_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            var values = new HashSet<T>();
            foreach (var edge in _definingEdges) {
                values.Add(edge.Item1);
                values.Add(edge.Item2);
            }

            _valueToNode = values.ToBimap(x => x, x => new Node());

            foreach (var edge in _definingEdges) {
                Node fromNode = _valueToNode.Left[edge.Item1];
                Node toNode = _valueToNode.Left[edge.Item2];
                fromNode.SubsequentVertices.Add(toNode);
            }

            CheckForCyclicity();

            ApplyTransitivity();
        }

        public int Compare(T x, T y) {
            Node xNode, yNode;
            if (!_valueToNode.Left.TryGetValue(x, out xNode)) {
                return 0;
            }
            if (!_valueToNode.Left.TryGetValue(y, out yNode)) {
                return 0;
            }
            if (xNode.SubsequentVertices.Contains(yNode)) return -1;
            if (yNode.SubsequentVertices.Contains(xNode)) return 1;
            return 0;
        }

        public IEnumerator<LessThanPair> GetEnumerator() {
            foreach (var node in _valueToNode.Left) {
                foreach (var adjacentNode in node.Value.SubsequentVertices) {
                    yield return new LessThanPair(node.Key, _valueToNode.Right[adjacentNode]);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
