using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.More;

namespace System.Collections.Concurrent.More {
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public sealed class AsyncSet<T> : IEnumerable<T>, IDisposable {
        private readonly ConditionVariable _cv = new ConditionVariable();
        private readonly List<T> _storage = new List<T>();
        private readonly ReaderWriterLockSlim _sync = new ReaderWriterLockSlim();
        private readonly HashSet<T> _tester = new HashSet<T>();
        private bool _closed;

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IEnumerator<T> GetEnumerator() {
            int index = 0;
            _sync.EnterReadLock();
            while (!_closed) {
                while (index < _storage.Count) {
                    T item = _storage[index++];
                    _sync.ExitReadLock();
                    yield return item;
                    _sync.EnterReadLock();
                }
                _cv.WaitRead(_sync);
            }
            _sync.ExitReadLock();
            while (index < _storage.Count) {
                yield return _storage[index++];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void Add(T item) {
            _sync.EnterWriteLock();
            try {
                if (_closed) {
                    throw new InvalidOperationException("The set has been closed.");
                }
                if (_tester.Add(item)) {
                    _storage.Add(item);
                    _cv.Broadcast();
                }
            } finally {
                _sync.ExitWriteLock();
            }
        }

        public bool TryAdd(T item) {
            _sync.EnterWriteLock();
            try {
                if (_closed) {
                    throw new InvalidOperationException("The set has been closed.");
                }
                if (!_tester.Add(item)) {
                    return false;
                }
                _storage.Add(item);
                _cv.Broadcast();
                return true;
            } finally {
                _sync.ExitWriteLock();
            }
        }

        public void Close() {
            _sync.EnterWriteLock();
            _closed = true;
            _cv.Broadcast();
            _sync.ExitWriteLock();
        }

        private void Dispose(bool disposeManaged) {
            _sync.Dispose();
        }
    }
}