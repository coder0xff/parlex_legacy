using System.Collections.Generic;

namespace System.Collections.Concurrent.More
{
    //A more convenient way to have a Dictionary<K, HashSet<T>>
    public class AutoDictionary<TK, TV> : IEnumerable<KeyValuePair<TK, TV>> {
        private readonly ConcurrentDictionary<TK, TV> _storage = new ConcurrentDictionary<TK, TV>();

        public TV this[TK key] {
            get {
                return _storage.GetOrAdd(key, x => _valueFactory(x));
            }
            set {
                _storage[key] = value;
            }
        }

        private readonly Func<TK, TV> _valueFactory;

        public AutoDictionary(Func<TK, TV> valueFactory) {
            _valueFactory = valueFactory;
        }

        public AutoDictionary() {
            _valueFactory = dontCare => default(TV);
        }

        public IEnumerator<KeyValuePair<TK, TV>> GetEnumerator() {
            return _storage.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return _storage.GetEnumerator();
        }

        public IEnumerable<TK> Keys { get { return _storage.Keys; }}
        public IEnumerable<TV> Values { get { return _storage.Values; }} 
    }
}
