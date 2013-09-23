using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic.More;
using System.Linq;
using System.Linq.More;
using System.Text;

namespace parlex {
    class StrictPartialOrder<T> : IComparer<T>, IEnumerable<StrictPartialOrder<T>.Edge> {
        private class Node {
            internal readonly HashSet<Node> AdjacentNodes = new HashSet<Node>();

            internal void ApplyTransitivity() {
                ApplyTransitivityRecursively(this);
            }

            private void ApplyTransitivityRecursively(Node node) {
                foreach (var adjacentNode in node.AdjacentNodes) {
                    AdjacentNodes.Add(adjacentNode);
                    ApplyTransitivityRecursively(adjacentNode);
                }
            }
        }

        internal struct Edge {
            internal readonly T From;
            internal readonly T To;

            internal Edge(T @from, T to) : this() {
                From = @from;
                To = to;
            }
        }

        private void CheckForCyclicity() {
            var ancestors = new Stack<Node>();
            foreach (var node in _valueToNode.Left) {
                CheckForCyclicityRecursively(node.Value, ancestors);
            }
        }

        private void CheckForCyclicityRecursively(Node currentNode, Stack<Node> ancestors) {
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
            foreach (var adjacentNode in currentNode.AdjacentNodes) {
                CheckForCyclicityRecursively(adjacentNode, ancestors);
            }
            ancestors.Pop();
        }

        private void ApplyTransitivity() {
            foreach (var node in _valueToNode.Left.Values) {
                node.ApplyTransitivity();
            }
        }

        private readonly Bimap<T, Node> _valueToNode;

        internal StrictPartialOrder(IEnumerable<Edge> edges) {
            var values = new HashSet<T>();
            foreach (var edge in edges) {
                values.Add(edge.From);
                values.Add(edge.To);
            }

            _valueToNode = values.ToBimap(x => x, x => new Node());

            foreach (var edge in edges) {
                Node fromNode = _valueToNode.Left[edge.From];
                Node toNode = _valueToNode.Left[edge.To];
                fromNode.AdjacentNodes.Add(toNode);
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
            if (xNode.AdjacentNodes.Contains(yNode)) return -1;
            if (yNode.AdjacentNodes.Contains(xNode)) return 1;
            return 0;
        }

        public IEnumerator<Edge> GetEnumerator() {
            foreach (var node in _valueToNode.Left) {
                foreach (var adjacentNode in node.Value.AdjacentNodes) {
                    yield return new Edge(node.Key, _valueToNode.Right[adjacentNode]);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
