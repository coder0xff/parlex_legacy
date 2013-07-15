using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace parlex {
    public class StrictPartialOrder<T> : IComparer<T> {
        public class Node {
            public readonly HashSet<Node> AdjacentNodes = new HashSet<Node>();
            public readonly T Value;

            public Node(T value) {
                Value = value;
            }

            public void ApplyTransitivity() {
                ApplyTransitivityRecursively(this);
            }

            private void ApplyTransitivityRecursively(Node node) {
                foreach (var adjacentNode in node.AdjacentNodes) {
                    AdjacentNodes.Add(adjacentNode);
                    ApplyTransitivityRecursively(adjacentNode);
                }
            }
        }

        public struct Edge {
            public readonly T From;
            public readonly T To;

            public Edge(T @from, T to) : this() {
                From = @from;
                To = to;
            }
        }

        private void CheckForCyclicity() {
            var ancestors = new Stack<Node>();
            foreach (var node in _valueToNode) {
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
            foreach (var node in _valueToNode.Values) {
                node.ApplyTransitivity();
            }
        }

        private readonly Dictionary<T, Node> _valueToNode;

        public StrictPartialOrder(IEnumerable<Edge> edges) {
            var values = new HashSet<T>();
            foreach (var edge in edges) {
                values.Add(edge.From);
                values.Add(edge.To);
            }

            _valueToNode = values.ToDictionary(x => x, x => new Node(x));

            foreach (var edge in edges) {
                Node fromNode = _valueToNode[edge.From];
                Node toNode = _valueToNode[edge.To];
                fromNode.AdjacentNodes.Add(toNode);
            }

            CheckForCyclicity();

            ApplyTransitivity();
        }

        public int Compare(T x, T y) {
            Node xNode, yNode;
            if (!_valueToNode.TryGetValue(x, out xNode)) {
                return 0;
            }
            if (!_valueToNode.TryGetValue(y, out yNode)) {
                return 0;
            }
            if (xNode.AdjacentNodes.Contains(yNode)) return -1;
            if (yNode.AdjacentNodes.Contains(xNode)) return 1;
            return 0;
        }
    }
}
