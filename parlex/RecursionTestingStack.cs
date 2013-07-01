using System.Collections.Generic;

namespace parlex {
    class RecursionTestingStack {
        class MutableInt {
            public int Value;
        }
        private readonly List<Product> _stack = new List<Product>();
        private readonly LinkedList<MutableInt> _trackers = new LinkedList<MutableInt>();
        public bool Push(Product v) {
            bool result = false;
            _trackers.AddLast(new MutableInt());
            _stack.Add(v);
            int trackerIndex = 0;
            foreach (var tracker in _trackers) {
                int trackerDistanceFromEnd = _trackers.Count - trackerIndex;

                if (trackerIndex < _trackers.Count - 1 && trackerIndex + tracker.Value < _stack.Count && _stack[trackerIndex + tracker.Value] == v) {
                    tracker.Value++;
                    if (tracker.Value * 2 == trackerDistanceFromEnd) {
                        result = true;
                    }
                } else {
                    tracker.Value = 0;
                }
                trackerIndex++;
            }
            return result;
        }

        public Product Pop() {
            Product result = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            _trackers.RemoveLast();
            foreach (var tracker in _trackers) {
                if (tracker.Value > 0) {
                    tracker.Value--;
                }
            }
            return result;
        }

        public int Count { get { return _stack.Count; } }
    }
}
