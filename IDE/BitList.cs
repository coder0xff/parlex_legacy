
// ReSharper disable once CheckNamespace
namespace System.Collections.Generic.More {
    /// <summary>
    /// Like BitArray but lots more functionality
    /// </summary>
    public class BitList : IList<bool>, ICloneable {
        private ulong[] _storage = new ulong[1];
        private long _count;

        public BitList(long initialSize = 64, bool initialValue = false) {
            Reserve(initialSize);
            _count = initialSize;
            if (initialValue) {
                Fill(true);
            }
        }

        private void Fill(bool value) {
            if (value) {
                for (long i = 0; i < _storage.LongLength; i++) {
                    _storage[i] = 0xFFFFFFFFFFFFFFFF;
                }
            } else {
                for (long i = 0; i < _storage.LongLength; i++) {
                    _storage[i] = 0;
                }
            }
        }

        public BitList(IEnumerable<bool> source) {
            foreach (bool b in source) {
                Add(b);
            }
        }

        public void Reserve(long bitCount) {
            var requestedStorageSize = (bitCount + 63) >> 6;
            if (requestedStorageSize <= _storage.LongLength) {
                return;
            }
            var newStorage = new ulong[requestedStorageSize];
            _storage.CopyTo(newStorage, 0);
            _storage = newStorage;
        }

        int IList<bool>.IndexOf(bool item) {
            long i;
            for (i = 0; i < _count && i <= int.MaxValue; i++) {
                if (this[i] == item) break;
            }
            if (i == _count) {
                return -1;
            }
            if (i > int.MaxValue) {
                throw new IndexOutOfRangeException("The result cannot fit in the type int");
            }
            return (int) i;
        }

        private long IndexOf(bool item) {
            for (long i = 0; i < _count && i <= int.MaxValue; i++) {
                if (this[i] == item) return i;
            }
            return -1;
        }

        void IList<bool>.Insert(int index, bool item) {
            Insert(index, item);
        }

        private void Insert(long index, bool item) {
            Reserve(index + 1);
            for (long i = _count; i >= index; i--) {
                this[i + 1] = this[i];
            }
            this[index] = item;
            _count++;
        }

        void IList<bool>.RemoveAt(int index) {
            RemoveAt(index);
        }

        private void RemoveAt(long index) {
            for (long i = index + 1; i < _count; i++) {
                this[i - 1] = this[i];
            }
            _count--;
        }

        bool IList<bool>.this[int index] {
            get { return this[index]; }
            set { this[index] = value; }
        }

        public bool this[long index] {
            get {
                return (_storage[index >> 6] & ((ulong) 1 << (int) (index & 0x3F))) != 0;
            }
            set {
                ulong mask = (ulong) 1 << (int) (index & 0x3F);
                _storage[index >> 6] = value ? _storage[index >> 6] | mask : _storage[index >> 6] & ~mask;
            }
        }

        private static int UlongCountLeastSignificantZeros(ulong x) {
            x |= (x << 1);
            x |= (x << 2);
            x |= (x << 4);
            x |= (x << 8);
            x |= (x << 16);
            x |= (x << 32);
            return (64 - UlongPopulationCount(x));
        }

        private static int UlongPopulationCount(ulong x) {
            const ulong m1 = 0x5555555555555555; //binary: 0101...
            const ulong m2 = 0x3333333333333333; //binary: 00110011..
            const ulong m4 = 0x0f0f0f0f0f0f0f0f; //binary:  4 zeros,  4 ones ...
            const ulong h01 = 0x0101010101010101; //the sum of 256 to the power of 0,1,2,3...

            x -= (x >> 1) & m1; //put count of each 2 bits into those 2 bits
            x = (x & m2) + ((x >> 2) & m2); //put count of each 4 bits into those 4 bits 
            x = (x + (x >> 4)) & m4; //put count of each 8 bits into those 8 bits 
            return (int) ((x * h01) >> 56); //returns left 8 bits of x + (x<<8) + (x<<16) + (x<<24) + ... 
        }

        public long PopulationCount() {
            long i;
            long result = 0;
            for (i = 0; i < (_count >> 6); i++) {
                result += UlongPopulationCount(_storage[i]);
            }
            i <<= 6;
            for (; i < _count; i++)
            {
                if (this[i]) {
                    result++;
                }
            }
            return result;
        }

        public long CountLeadingZeros(long startIndex = 0) {
            long result = 0;
            if (startIndex << 26 != 0) { //equivalent to startIndex % 64 != 0
                for (; startIndex < _count && (startIndex << 26) != 0; startIndex++) {
                    if (this[startIndex]) {
                        return result;
                    }
                    result++;
                }
            }
            if (startIndex < _count) {
                long i;
                for (i = startIndex >> 6; i < (_count >> 6); i++) {
                    if (_storage[i] != 0) break;
                    result += 64;
                }
                if (i != _storage.LongLength) {
                    result += UlongCountLeastSignificantZeros(_storage[i]);
                    result = Math.Min(result, _count);
                }
            }
            return result;
        }

        public IEnumerator<bool> GetEnumerator() {
            var copy = (BitList) Clone();
            for (long i = 0; i < copy._count; i++) {
                yield return copy[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void Add(bool item) {
            if (_count >> 6 == _storage.LongLength) {
                Reserve(LongCount * 2);
            }
            this[_count] = item;
            _count++;
        }

        public void Clear() {
            _count = 0;
        }

        public bool Contains(bool item) {
            if (item) {
                for (long i = 0; i < (_count >> 6); i++) {
                    if (_storage[i] != 0) return true;
                }
            } else {
                for (long i = 0; i < (_count >> 6); i++) {
                    if (_storage[i] != 0xFFFFFFFFFFFFFFFF) return true;
                }
            }
            for (var i = (_count & ~0x3F); i < _count; i++) {
                if (this[i] == item) {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(bool[] array, int arrayIndex) {
            for (int i = 0; i < LongCount; i++) {
                array[arrayIndex++] = this[i];
            }
        }

        public bool Remove(bool item) {
            var indexOf = IndexOf(item);
            if (indexOf == -1) {
                return false;
            }
            RemoveAt(indexOf);
            return true;
        }

        public int Count {
            get {
                return (int) _count;
            }
        }

        public long LongCount {
            get {
                return _count;
            }
        }

        public bool IsReadOnly {
            get {
                return false;
            }
        }

        public object Clone() {
            return new BitList {_storage = (ulong[]) _storage.Clone(), _count = _count};
        }

        public void AndWith(BitList other) {
            if (_count != other._count) {
                throw new InvalidOperationException();
            }
            for (long i = 0; i < _storage.LongLength; i++) {
                _storage[i] &= other._storage[i];
            }
        }

        public void OrWith(BitList other) {
            if (_count != other._count) {
                throw new InvalidOperationException();
            }
            for (long i = 0; i < _storage.LongLength; i++) {
                _storage[i] |= other._storage[i];
            }
        }

        public void XorWith(BitList other) {
            if (_count != other._count) {
                throw new InvalidOperationException();
            }
            for (long i = 0; i < _storage.LongLength; i++) {
                _storage[i] ^= other._storage[i];
            }
        }

        public void Not() {
            for (long i = 0; i < _storage.LongLength; i++) {
                _storage[i] = ~_storage[i];
            }            
        }
    }
}
