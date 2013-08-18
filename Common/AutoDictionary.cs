using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Common
{
    //A more convenient way to have a Dictionary<K, HashSet<T>>
    public class AutoDictionary<TK, TV> : IEnumerable<KeyValuePair<TK, TV>> {
        private readonly ConcurrentDictionary<TK, TV> _storage = new ConcurrentDictionary<TK, TV>();

        public TV this[TK key] {
            get {
                return _storage.GetOrAdd(key, x => _valueFactory());
            }
            set {
                _storage[key] = value;
            }
        }

        private readonly Func<TV> _valueFactory;

        public AutoDictionary(Func<TV> valueFactory) {
            _valueFactory = valueFactory;
        }

        public AutoDictionary() {
            _valueFactory = () => default(TV);
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
