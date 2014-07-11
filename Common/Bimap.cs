using System.Collections.Generic;
using System.Linq;

namespace System.Collections.Generic.More {
    public class BiMap<T1, T2> {
        class BimapView<U1, U2> : IDictionary<U1, U2> {
            public readonly Dictionary<U1, U2> _keyToValue;
            public readonly Dictionary<U2, U1> _valueToKey;

            public BimapView(Dictionary<U1, U2> keyToValue, Dictionary<U2, U1> valueToKey) {
                _keyToValue = keyToValue;
                _valueToKey = valueToKey;
            }

            public void Add(U1 key, U2 value) {
                if (_keyToValue.ContainsKey(key)) {
                    _keyToValue.Add(key, value); //throw exception
                } else if (_valueToKey.ContainsKey(value)) {
                    _valueToKey.Add(value, key); //throw exception
                } else {
                    _keyToValue.Add(key, value);
                    _valueToKey.Add(value, key);
                }
        }

            public bool ContainsKey(U1 key) {
                return _keyToValue.ContainsKey(key);
            }

            public ICollection<U1> Keys {
                get { return _keyToValue.Keys; }
            }

            public bool Remove(U1 key) {
                U2 value;
                if (TryGetValue(key, out value)) {
                    _keyToValue.Remove(key);
                    _valueToKey.Remove(value);
                    return true;
                }
                return false;
            }

            public bool TryGetValue(U1 key, out U2 value) {
                return _keyToValue.TryGetValue(key, out value);
            }

            public ICollection<U2> Values {
                get { return _valueToKey.Keys; }
            }

            public U2 this[U1 key] {
                get {
                    U2 value;
                    if (_keyToValue.TryGetValue(key, out value)) {
                        return value;
                    }
                    Add(key, default(U2));
                    return default(U2);
                }
                set {
                    U2 oldValue;
                    if (!_keyToValue.TryGetValue(key, out oldValue) || !oldValue.Equals(value)) {
                            _valueToKey.Add(value, key); //may throw
                            _keyToValue.Add(key, value);
                    }                    
                }
            }

            public void Add(KeyValuePair<U1, U2> item) {
                Add(item.Key, item.Value);
            }

            public void Clear() {
                _valueToKey.Clear();
                _keyToValue.Clear();
            }

            public bool Contains(KeyValuePair<U1, U2> item) {
                return _keyToValue.Contains(item);
            }

            public void CopyTo(KeyValuePair<U1, U2>[] array, int arrayIndex) {
                ((IDictionary<U1, U2>)_keyToValue).CopyTo(array, arrayIndex);
            }

            public int Count {
                get { return _keyToValue.Count; }
            }

            public bool IsReadOnly {
                get { return false; }
            }

            public bool Remove(KeyValuePair<U1, U2> item) {
                ((IDictionary<U2, U1>) _valueToKey).Remove(new KeyValuePair<U2, U1>(item.Value, item.Key));
                return ((IDictionary<U1, U2>)_keyToValue).Remove(item);
            }

            public IEnumerator<KeyValuePair<U1, U2>> GetEnumerator() {
                return _keyToValue.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
                return _keyToValue.GetEnumerator();
            }
        }

        readonly BimapView<T1, T2> _left;
        readonly BimapView<T2, T1> _right;

        public BiMap() {
            var leftDictionary = new Dictionary<T1, T2>();
            var rightDictionary = new Dictionary<T2, T1>();
            _left = new BimapView<T1, T2>(leftDictionary, rightDictionary);
            _right = new BimapView<T2, T1>(rightDictionary, leftDictionary);
        }

        public IDictionary<T1, T2> Left { get { return _left; }} 
        public IDictionary<T2, T1> Right { get { return _right; }} 

        public int Count { get { return _left.Count; }}

        public void Clear() {
            _left.Clear();
        }
    }
}

namespace System.Linq.More {
    public static class BimapIEnumerableExtensions {
        public static Collections.Generic.More.BiMap<TLeft, TRight> ToBimap<TElement, TLeft, TRight>(this IEnumerable<TElement> enumerable, Func<TElement, TLeft> leftFunc, Func<TElement, TRight> rightFunc) {
            var result = new Collections.Generic.More.BiMap<TLeft, TRight>();
            foreach (var variable in enumerable) {
                result.Left.Add(leftFunc(variable), rightFunc(variable));
            }
            return result;
        }

        public static Collections.Generic.More.BiMap<TLeft, TRight> ToBimap<TElement, TLeft, TRight>(this IEnumerable<TElement> enumerable, Func<TElement, int, TLeft> leftFunc, Func<TElement, int, TRight> rightFunc) {
            var result = new Collections.Generic.More.BiMap<TLeft, TRight>();
            int indexCounter = 0;
            foreach (var variable in enumerable) {
                result.Left.Add(leftFunc(variable, indexCounter), rightFunc(variable, indexCounter));
                indexCounter++;
            }
            return result;
        }
    }
}