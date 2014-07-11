using System.Collections.Generic;

namespace System.Collections.Concurrent.More
{
    //A more convenient way to have a Dictionary<K, HashSet<T>>
    public class AutoDictionary<KeyType, ValueType> : IEnumerable<KeyValuePair<KeyType, ValueType>> {
        readonly ConcurrentDictionary<KeyType, ValueType> _storage = new ConcurrentDictionary<KeyType, ValueType>();

        public ValueType this[KeyType key] {
            get {
                return _storage.GetOrAdd(key, x => _valueFactory(x));
            }
            set {
                _storage[key] = value;
            }
        }

        /// <summary>
        /// Used to make sure that an entry is created for the specified key
        /// </summary>
        /// <param name="key"></param>
        public bool EnsureCreated(KeyType key) {
            bool wasCreated = false;
            _storage.GetOrAdd(key, x => { wasCreated = true; return _valueFactory(x); });
            return wasCreated;
        }

        public void Clear() {
            _storage.Clear();
        }

        readonly Func<KeyType, ValueType> _valueFactory;

        public AutoDictionary(Func<KeyType, ValueType> valueFactory) {
            _valueFactory = valueFactory;
        }

        public AutoDictionary() {
            _valueFactory = dontCare => default(ValueType);
        }

        public IEnumerator<KeyValuePair<KeyType, ValueType>> GetEnumerator() {
            return _storage.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return _storage.GetEnumerator();
        }

        public IEnumerable<KeyType> Keys { get { return _storage.Keys; }}
        public IEnumerable<ValueType> Values { get { return _storage.Values; }}

        public bool TryRemove(KeyType key) {
            ValueType dontCare;
            return _storage.TryRemove(key, out dontCare);
        }
    }
}
