using System.Collections.Concurrent;

namespace System.Threading.More {
    public class ConditionVariable {
        sealed class ThreadSync : IDisposable {
            internal readonly ManualResetEventSlim Sync = new ManualResetEventSlim();

            public void Dispose() {
                Sync.Dispose();
            }
        }

        readonly ConcurrentQueue<ThreadSync> _waitingThreads = new ConcurrentQueue<ThreadSync>();

        /// <summary>
        /// Atomically unlocks and waits for a signal.
        /// Then relocks the mutex before returning
        /// </summary>
        /// <param name="mutex"></param>
        public void Wait(Mutex mutex) {
            if (mutex == null) throw new ArgumentNullException("mutex");
            var ts = new ThreadSync();
            try {
                _waitingThreads.Enqueue(ts);
                mutex.ReleaseMutex();
                ts.Sync.Wait();
            } finally {
                ts.Dispose();
            }
            mutex.WaitOne();
        }

        public void WaitRead(ReaderWriterLockSlim readerWriterLock) {
            if (readerWriterLock == null) throw new ArgumentNullException("readerWriterLock");
            var ts = new ThreadSync();
            try {
                _waitingThreads.Enqueue(ts);
                readerWriterLock.ExitReadLock();
                ts.Sync.Wait();
            } finally {
                ts.Dispose();
            }
            readerWriterLock.EnterReadLock();
        }

        public void Signal() {
            ThreadSync ts;
            if (_waitingThreads.TryDequeue(out ts)) {
                ts.Sync.Set();
            }
        }

        public void Broadcast() {
            ThreadSync ts;
            while (_waitingThreads.TryDequeue(out ts)) {
                ts.Sync.Set();
            }
        }
    }
}
