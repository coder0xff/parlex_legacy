﻿using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Synchronox {
    internal class ConditionVariable {
        private readonly ConcurrentQueue<ManualResetEventSlim> _waitingThreads = new ConcurrentQueue<ManualResetEventSlim>();
        public bool AnyWaiting { get { return _waitingThreads.Count > 0; } }

        /// <summary>
        ///     Atomically unlocks and waits for a signal.
        ///     Then relocks the mutex before returning
        /// </summary>
        /// <param name="mutex"></param>
        public void Wait(Mutex mutex) {
            if (mutex == null) {
                throw new ArgumentNullException("mutex");
            }
            var waitHandle = new ManualResetEventSlim();
            try {
                _waitingThreads.Enqueue(waitHandle);
                mutex.ReleaseMutex();
                waitHandle.Wait();
            } finally {
                waitHandle.Dispose();
            }
            mutex.WaitOne();
        }

        public bool Signal() {
            ManualResetEventSlim waitHandle;
            if (!_waitingThreads.TryDequeue(out waitHandle)) {
                return false;
            }
            waitHandle.Set();
            return true;
        }
    }
}