using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common {
    /// <summary>
    /// An immutable set of States that can be quickly tested for set inequality.
    /// Note that returning 'true' from equals is still O(n) as it requires comparing every element.
    /// </summary>
    public class ReadOnlyHashSet<T> : IEnumerable<T> {
        public bool Equals(ReadOnlyHashSet<T> other) {
            return _hashCode == other._hashCode && _items.SetEquals(other._items);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            return obj is ReadOnlyHashSet<T> && Equals((ReadOnlyHashSet<T>)obj);
        }

        public override int GetHashCode() {
            return _hashCode;
        }

        public static bool operator ==(ReadOnlyHashSet<T> left, ReadOnlyHashSet<T> right) {
            return left.Equals(right);
        }

        public static bool operator !=(ReadOnlyHashSet<T> left, ReadOnlyHashSet<T> right) {
            return !left.Equals(right);
        }

        private readonly HashSet<T> _items;
        private readonly int _hashCode;

        public ReadOnlyHashSet(IEnumerable<T> items)
        {
            _items = new HashSet<T>(items);
            _hashCode = 0;
            List<int> hashes = items.Select(state => state.GetHashCode()).ToList();
            hashes.Sort();
            foreach (var hash in hashes) {
                _hashCode = _hashCode * 397 ^ hash;
            }
        }

        public IEnumerator<T> GetEnumerator() {
            return _items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return _items.GetEnumerator();
        }
    }
}
