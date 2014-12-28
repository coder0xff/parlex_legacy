using System.Collections;
using System.Collections.Generic;
using Synchronox;
using System.Diagnostics;
using NUnit.Framework;

#pragma warning disable 169
// ReSharper disable UnassignedReadonlyField.Compiler

namespace NUnitTests {
    [TestFixture]
    class SynchronoxTests1 {
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
                ConstructionCompleted();
            }
            private readonly List<int> _results;
            protected override void Computer() {
                while (true) {
                    int l;
                    if (!Left.Dequeue(out l)) return;
                    int r;
                    if (!Right.Dequeue(out r)) return;
                    _results.Add(l + r);
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
            for (var j = 0; j < 100; j++) {
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

    [TestFixture]
    class SynchronoxTests2 {
        class DeadlockNode : Node {
            public DeadlockNode(Collective collective) : base(collective) {
                ConstructionCompleted();
            }
            protected override void Computer() {
                int datum;
                i.Dequeue(out datum);
            }

            public readonly Input<int> i;
            public readonly Output<int> o;
        }

        class DeadlockCollective1 : Collective {
            public DeadlockCollective1() {
                var node = new DeadlockNode(this);
                Connect(node.i, node.o);
                ConstructionCompleted();
            }
        }

        [Test]
        public void Test1() {
            for (var i = 0; i < 100; i++) {
                var testCollective = new DeadlockCollective1();
                testCollective.Join();
            }
        }

        class DeadlockCollective2 : Collective {
            public DeadlockCollective2() {
                var node1 = new DeadlockNode(this);
                var node2 = new DeadlockNode(this);
                Connect(node1.i, node2.o);
                Connect(node2.i, node1.o);
                ConstructionCompleted();
            }
        }

        [Test]
        public void Test2() {
            for (var i = 0; i < 100; i++) {
                var testCollective = new DeadlockCollective2();
                testCollective.Join();
            }
        }
    }
}

// ReSharper restore UnassignedReadonlyField.Compiler
#pragma warning restore 169
