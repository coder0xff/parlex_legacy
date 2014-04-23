using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common {
    public class DiscreteProbabilityDistribution<T> {
        private List<double> _map = new List<double>{0};
        private List<T> _items = new List<T>{default(T)};

        public DiscreteProbabilityDistribution() { } 
        public DiscreteProbabilityDistribution(IEnumerable<Tuple<T, double>> items) {
            foreach (var tuple in items) {
                Add(tuple.Item1, tuple.Item2);
            }
        }

        public void Add(T item, double weight) {
            _items.Add(item);
            _map.Add(_map[_map.Count - 1] + weight);
        }

        public T Next() {
            double sample = Rng.NextDouble(0, _map[_map.Count - 1]);
            //do a binary search for the least map index that is greater than sample
            int low = 0;
            int hi = _map.Count - 1;
            while(low != hi) {
                int selectedIndex = (low + hi) / 2; //truncate
                if (_map[selectedIndex] > sample) {
                    hi = selectedIndex;
                } else {
                    low = selectedIndex + 1;
                }
            }
            return _items[low];
        }
    }
}
