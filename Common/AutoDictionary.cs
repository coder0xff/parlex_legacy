using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Concurrent.More {
    //A more convenient way to have a Dictionary<K, HashSet<T>>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix"), SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class AutoDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> {
        private readonly ConcurrentDictionary<TKey, TValue> _storage = new ConcurrentDictionary<TKey, TValue>();

        private readonly Func<TKey, TValue> _valueFactory;

        public AutoDictionary(Func<TKey, TValue> valueFactory) {
            _valueFactory = valueFactory;
        }

        public AutoDictionary() {
            _valueFactory = dontCare => default(TValue);
        }

        public TValue this[TKey key] {
            get { return _storage.GetOrAdd(key, x => _valueFactory(x)); }
            set { _storage[key] = value; }
        }

        public IEnumerable<TKey> Keys {
            get { return _storage.Keys; }
        }

        public IEnumerable<TValue> Values {
            get { return _storage.Values; }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            return _storage.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return _storage.GetEnumerator();
        }

        /// <summary>
        ///     Used to make sure that an entry is created for the specified key
        /// </summary>
        /// <param name="key"></param>
        public bool EnsureCreated(TKey key) {
            bool wasCreated = false;
            _storage.GetOrAdd(key, x => {
                wasCreated = true;
                return _valueFactory(x);
            });
            return wasCreated;
        }

        public void Clear() {
            _storage.Clear();
        }

        public bool TryRemove(TKey key) {
            TValue dontCare;
            return _storage.TryRemove(key, out dontCare);
        }

        public void Add(KeyValuePair<TKey, TValue> value) {
            if (!_storage.TryAdd(value.Key, value.Value)) {
                throw new InvalidOperationException("Could not add key");
            }
        }
    }
}