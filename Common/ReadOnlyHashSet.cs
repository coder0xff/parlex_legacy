using System.Linq;

namespace System.Collections.Generic.More {
    /// <summary>
    /// An immutable set of States that can be quickly tested for set inequality.
    /// Note that returning 'true' from equals is still O(n) as it requires comparing every element.
    /// </summary>
    public class ReadOnlyHashSet<T> : ISet<T> {
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
            return left != null && left.Equals(right);
        }

        public static bool operator !=(ReadOnlyHashSet<T> left, ReadOnlyHashSet<T> right) {
            return left != null && !left.Equals(right);
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

        IEnumerator IEnumerable.GetEnumerator() {
            return _items.GetEnumerator();
        }

        public void UnionWith(IEnumerable<T> other) {
            throw new InvalidOperationException();
        }

        public void IntersectWith(IEnumerable<T> other) {
            throw new InvalidOperationException();
        }

        public void ExceptWith(IEnumerable<T> other) {
            throw new InvalidOperationException();
        }

        public void SymmetricExceptWith(IEnumerable<T> other) {
            throw new InvalidOperationException();
        }

        public bool IsSubsetOf(IEnumerable<T> other) {
            return _items.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other) {
            return _items.IsSupersetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other) {
            return _items.IsProperSupersetOf(other);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other) {
            return _items.IsProperSubsetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other) {
            return _items.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other) {
            return _items.SetEquals(other);
        }

        public bool Add(T item) {
            throw new InvalidOperationException();
        }

        void ICollection<T>.Add(T item) {
            throw new InvalidOperationException();
        }

        public void Clear() {
            throw new InvalidOperationException();
        }

        public bool Contains(T item) {
            return _items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex) {
            _items.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item) {
            throw new InvalidOperationException();
        }

        public int Count {
            get { return _items.Count; }
        }

        public bool IsReadOnly {
            get { return true; }
        }

        public static ReadOnlyHashSet<T> MultiIntersect(IEnumerable<IEnumerable<T>> sets) {
            var firstSet = sets.FirstOrDefault();
            var temp = new HashSet<T>();
            if (firstSet != null) {
                temp.UnionWith(firstSet);
                foreach (var source in sets.Skip(1)) {
                    temp.IntersectWith(source);
                }
            }
            return new ReadOnlyHashSet<T>(temp);
        }
    }
}
