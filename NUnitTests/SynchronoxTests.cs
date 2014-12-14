using System.Collections;
using System.Collections.Generic;
using Synchronox;
using System.Diagnostics;
using NUnit.Framework;

#pragma warning disable 169
// ReSharper disable UnassignedReadonlyField.Compiler

namespace NUnitTests {
    [TestFixture]
    class SynchronoxTests {
        class MyNumberNode : Node {
            public MyNumberNode(Collective collective) : base(collective) {
                ConstructionCompleted();
            }

            protected override void Computer() {
                for (var i = 0; i <= 10; i++) {
                    Number.Enqueue(i);
                }
            }

            public readonly Output<int> Number;
        }

        class MySumNode : Node, IEnumerable<int> {
            public MySumNode(Test1Collective collective) : base(collective) {
                _results = collective.results;
            }
            private readonly List<int> _results;
            protected override void Computer() {
                while (true) {
                    _results.Add(Left.Dequeue() + Right.Dequeue());
                }
            }

            public readonly Input<int> Left;
            public readonly Input<int> Right;
            public readonly Output<int> Sum;
            public IEnumerator<int> GetEnumerator() {
                return _results.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
        }

        class Test1Collective : Collective, IEnumerable<int> {
            internal List<int> results = new List<int>(); 
            public Test1Collective() {
                var left = new MyNumberNode(this);
                var right = new MyNumberNode(this);
                var sum = new MySumNode(this);
                Connect(sum.Left, left.Number);
                Connect(sum.Right, right.Number);
                ConstructionCompleted();
            }

            public IEnumerator<int> GetEnumerator() {
                return results.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
        }
        [Test]
        public void Test1() {
            var testCollective = new Test1Collective();
            testCollective.Join();
            var expected = 0;
            foreach (var i in testCollective) {
                Debug.Assert(i == expected);
                expected += 2;
            }
            Debug.Assert(expected == 22);
        }
    }
}

// ReSharper restore UnassignedReadonlyField.Compiler
#pragma warning restore 169
