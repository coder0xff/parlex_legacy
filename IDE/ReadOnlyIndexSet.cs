using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDE {
    class ReadOnlyIndexSet : IEnumerable<int> {
        public ReadOnlyIndexSet(IndexSet source) {
            _value = new IndexSet(source);
            _hashCode = _value.GetChecksum();
        }

        protected bool Equals(ReadOnlyIndexSet other) {
            return _hashCode == other._hashCode && _value.SequenceEquals(other._value);
        }

        public IEnumerator<int> GetEnumerator() {
            return _value.GetEnumerator();
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (ReadOnlyIndexSet)) return false;
            return Equals((ReadOnlyIndexSet) obj);
        }

        public override int GetHashCode() {
            return _hashCode;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public static bool operator ==(ReadOnlyIndexSet left, ReadOnlyIndexSet right) {
            return Equals(left, right);
        }

        public static bool operator !=(ReadOnlyIndexSet left, ReadOnlyIndexSet right) {
            return !Equals(left, right);
        }

        private readonly IndexSet _value;
        private readonly int _hashCode;
    }
}
