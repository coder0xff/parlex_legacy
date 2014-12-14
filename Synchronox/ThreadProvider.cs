using System;
using System.Collections.Concurrent;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronox {
    class ThreadProvider : IDisposable {
        class PoolThread : IDisposable {
            private readonly ThreadProvider _pool;
            private readonly ManualResetEventSlim _sleeper = new ManualResetEventSlim();
            private Action _toRun;
            private ManualResetEventSlim _toComplete;
            private bool _disposed;
            private readonly Thread _thread;

            public PoolThread(ThreadProvider pool) {
                _pool = pool;
                _thread = new Thread(Loop);
                _thread.Start();
            }

            private void Loop() {
                while (!_disposed) {
                    _sleeper.Wait();
                    _sleeper.Reset();
                    _toRun();
                    _toComplete.Set();
                    _toRun = null;
                    _pool._availableThreads.Add(this);
                }
            }

            public void Go(Action action, ManualResetEventSlim completed) {
                if (_disposed) throw new ObjectDisposedException("");
                _toRun = action;
                _toComplete = completed;
                _sleeper.Set();
            }

            public void Dispose() {
                _disposed = true;
            }
        }

        private bool _disposed;
        private readonly ReaderWriterLockSlim _disposedLock = new ReaderWriterLockSlim();
        private readonly ConcurrentBag<PoolThread> _availableThreads = new ConcurrentBag<PoolThread>();
        private readonly ConcurrentSet<PoolThread> _allThreads = new ConcurrentSet<PoolThread>();

        public class Task {
            internal readonly ManualResetEventSlim _completed = new ManualResetEventSlim();
            public void Join() {
                _completed.Wait();
            }
        }

        public Task Start(Action action) {
            _disposedLock.EnterReadLock();
            if (_disposed) throw new ObjectDisposedException("");
            PoolThread poolThread;
            if (!_availableThreads.TryTake(out poolThread)) {
                poolThread = new PoolThread(this);
                _allThreads.Add(poolThread);
                Debug.WriteLine("Threads in this pool: " + _allThreads.Count);
            }
            var result = new Task();
            poolThread.Go(action, result._completed);
            _disposedLock.ExitReadLock();
            return result;
        }


        public void Dispose() {
            _disposedLock.EnterWriteLock();
            _disposed = true;
            foreach (var poolThread in _allThreads) {
                poolThread.Dispose();
            }
            _disposedLock.ExitWriteLock();
        }

        ~ThreadProvider() {
            Dispose();
        }

        static public ThreadProvider Default { get { return _default; } }

        static ThreadProvider _default = new ThreadProvider();
    }
}
