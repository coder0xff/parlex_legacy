using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System.Collections.Concurrent.More {
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class ConcurrentSet<T> : ICollection<T> {
        readonly ConcurrentDictionary<T, byte> _storage;

        public ConcurrentSet() {
            _storage = new ConcurrentDictionary<T, byte>();
        }

        public ConcurrentSet(IEnumerable<T> collection) {
            _storage = new ConcurrentDictionary<T, byte>(collection.Select(_ => new KeyValuePair<T, byte>(_, 0)));
        }

        public ConcurrentSet(IEqualityComparer<T> comparer) {
            _storage = new ConcurrentDictionary<T, byte>(comparer);
        }

        public ConcurrentSet(IEnumerable<T> collection, IEqualityComparer<T> comparer) {
            _storage = new ConcurrentDictionary<T, byte>(collection.Select(_ => new KeyValuePair<T, byte>(_, 0)), comparer);
        }

        public ConcurrentSet(int concurrencyLevel, int capacity) {
            _storage = new ConcurrentDictionary<T, byte>(concurrencyLevel, capacity);
        }

        public ConcurrentSet(int concurrencyLevel, IEnumerable<T> collection, IEqualityComparer<T> comparer) {
            _storage = new ConcurrentDictionary<T, byte>(concurrencyLevel, collection.Select(_ => new KeyValuePair<T, byte>(_, 0)), comparer);
        }

        public ConcurrentSet(int concurrencyLevel, int capacity, IEqualityComparer<T> comparer) {
            _storage = new ConcurrentDictionary<T, byte>(concurrencyLevel, capacity, comparer);
        }

        public int Count { get { return _storage.Count; } }

        public bool IsEmpty { get { return _storage.IsEmpty; } }

        public void Clear() {
            _storage.Clear();
        }

        public bool Contains(T item) {
            return _storage.ContainsKey(item);
        }

        public bool TryAdd(T item) {
            return _storage.TryAdd(item, 0);
        }

        public bool TryRemove(T item) {
            byte dontCare;
            return _storage.TryRemove(item, out dontCare);
        }

        public void Add(T item) {
            ((ICollection<KeyValuePair<T, byte>>)_storage).Add(new KeyValuePair<T, byte>(item, 0));
        }

        public void CopyTo(T[] array, int arrayIndex) {
            if (array == null) throw new ArgumentNullException("array");
            foreach (var pair in _storage)
                array[arrayIndex++] = pair.Key;
        }

        public bool IsReadOnly {
            get { return false; }
        }

        public bool Remove(T item) {
            return TryRemove(item);
        }

        public IEnumerator<T> GetEnumerator() {
            return _storage.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return _storage.Keys.GetEnumerator();
        }
    }
}