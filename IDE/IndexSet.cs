using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic.More;
using System.Linq;

namespace IDE {
    /// <summary>
    /// Like a Set&lt;int&gt; but stores a single bit for every integer between 0 and Count (exclusive). This way, Set intersections, unions, copy, etc, can be done extremely quickly on small sets
    /// The algorithms (even Count) are, however, linear and may not do as well as logarithmic algorithms once it gets larger (probably in the thousands)
    /// </summary>
    class IndexSet : ISet<int> {
        private readonly BitList _storage;

        public IndexSet(int count) {
            _storage = new BitList(count);
        }

        public IndexSet(IndexSet other) {
            _storage = (BitList)other._storage.Clone();
        }

        public IndexSet(IEnumerable<int> source, int? count = null) {
// ReSharper disable once PossibleMultipleEnumeration
            _storage = new BitList(count.HasValue ? count.Value : source.Max());
// ReSharper disable once PossibleMultipleEnumeration
            foreach (var i in source) {
                _storage[i] = true;
            }
        }

        public IEnumerator<int> GetEnumerator() {
            for (long i = 0; i < _storage.LongCount; i++) {
                i += _storage.CountLeadingZeros(i);
                if (i < _storage.LongCount) yield return (int)i; //unfortunately, this comparison must be made twice per iteration, because CountLeadingZeros also should not get _storage.LongCount;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public bool Add(int item) {
            if (item >= _storage.Count) {
                throw new IndexOutOfRangeException("This IndexSet can only hold values in the range 0 - " + (_storage.Count - 1));
            }
            var result = !_storage[item];
            _storage[item] = true;
            return result;
        }

        public void UnionWith(IEnumerable<int> other) {
            var asIndexSet = other as IndexSet;
            if (asIndexSet != null && asIndexSet._storage.LongCount == _storage.LongCount) {
                _storage.OrWith(asIndexSet._storage);
            } else {
                throw new NotImplementedException();
            }
        }

        public void IntersectWith(IEnumerable<int> other) {
            var asIndexSet = other as IndexSet;
            if (asIndexSet != null && asIndexSet._storage.LongCount == _storage.LongCount) {
                _storage.AndWith(asIndexSet._storage);
            } else {
                throw new NotImplementedException();
            }
        }

        public void ExceptWith(IEnumerable<int> other) {
            var asIndexSet = other as IndexSet;
            if (asIndexSet != null && asIndexSet._storage.LongCount == _storage.LongCount) {
                _storage.Not();
                _storage.OrWith(asIndexSet._storage);
                _storage.Not();
            } else {
                throw new NotImplementedException();
            }
        }

        private void AbsoluteCompliment() {
            _storage.Not();
        }

        public void SymmetricExceptWith(IEnumerable<int> other) {
            var asIndexSet = other as IndexSet;
            if (asIndexSet != null && asIndexSet.Count == Count) {
                _storage.XorWith(asIndexSet._storage);
            } else {
                throw new NotImplementedException();
            }
        }

        public bool IsSubsetOf(IEnumerable<int> other) {
            var asIndexSet = other as IndexSet;
            if (asIndexSet != null && asIndexSet._storage.LongCount == _storage.LongCount) {
                var temp = new IndexSet(this);
                temp.ExceptWith(asIndexSet);
                return temp._storage.CountLeadingZeros() == temp._storage.LongCount;
            }
            throw new NotImplementedException();
        }

        public bool IsSupersetOf(IEnumerable<int> other) {
            var asIndexSet = other as IndexSet;
            if (asIndexSet != null && asIndexSet._storage.LongCount == _storage.LongCount) {
                var temp = new IndexSet(asIndexSet);
                temp.ExceptWith(this);
                return temp._storage.CountLeadingZeros() == temp._storage.LongCount;
            }
            throw new NotImplementedException();
        }

        public bool IsProperSupersetOf(IEnumerable<int> other) {
            var asIndexSet = other as IndexSet;
            if (asIndexSet != null && asIndexSet._storage.LongCount == _storage.LongCount) {
                return IsSupersetOf(other) && Count > asIndexSet.Count;
            }
            throw new NotImplementedException();
        }

        public bool IsProperSubsetOf(IEnumerable<int> other) {
            var asIndexSet = other as IndexSet;
            if (asIndexSet != null && asIndexSet._storage.LongCount == _storage.LongCount) {
                return IsSubsetOf(asIndexSet) && Count < asIndexSet.Count;
            }
            throw new NotImplementedException();
        }

        public bool Overlaps(IEnumerable<int> other) {
            throw new NotImplementedException();
        }

        public bool SetEquals(IEnumerable<int> other) {
            throw new NotImplementedException();
        }

        void ICollection<int>.Add(int item) {
            throw new NotImplementedException();
        }

        public void Clear() {
            throw new NotImplementedException();
        }

        public bool Contains(int item) {
            throw new NotImplementedException();
        }

        public void CopyTo(int[] array, int arrayIndex) {
            throw new NotImplementedException();
        }

        public bool Remove(int item) {
            throw new NotImplementedException();
        }

        public int Count { get { return (int)_storage.PopulationCount(); } }

        public bool IsReadOnly { get { return false; } }

        public bool SequenceEquals(IndexSet other) {
            if (other._storage.LongCount != _storage.LongCount) {
                throw new NotImplementedException();
            }
            return _storage.SequenceEquals(other._storage);
        }

        public int GetChecksum() {
            return _storage.GetChecksum();
        }
    }
}
