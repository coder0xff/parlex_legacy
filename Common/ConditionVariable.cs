using System.Collections.Concurrent;

namespace System.Threading.More
{
    class ConditionVariable
    {
        class ThreadSync : IDisposable
        {
            public readonly System.Threading.Thread thread = System.Threading.Thread.CurrentThread;
            public readonly ManualResetEventSlim sync = new ManualResetEventSlim();

            public void Dispose()
            {
                Dispose(true);
            }

            protected virtual void Dispose(bool disposeManaged)
            {
                sync.Dispose();
            }
        }

        ConcurrentQueue<ThreadSync> waitingThreads = new ConcurrentQueue<ThreadSync>();

        /// <summary>
        /// Atomically unlocks and waits for a signal.
        /// Then relocks the mutex before returning
        /// </summary>
        /// <param name="mutex"></param>
        public void Wait(Mutex mutex)
        {
            ThreadSync ts = new ThreadSync();
            waitingThreads.Enqueue(ts);
            mutex.ReleaseMutex();
            ts.sync.Wait();
            mutex.WaitOne();
        }

        public void WaitRead(ReaderWriterLockSlim rwlock)
        {
            ThreadSync ts = new ThreadSync();
            waitingThreads.Enqueue(ts);
            rwlock.ExitReadLock();
            ts.sync.Wait();
            ts.Dispose();
            rwlock.EnterReadLock();
        }

        public void Signal()
        {
            ThreadSync ts;
            if (waitingThreads.TryDequeue(out ts))
            {
                ts.sync.Set();
            }
        }

        public void Broadcast()
        {
            ThreadSync ts;
            while (waitingThreads.TryDequeue(out ts))
            {
                ts.sync.Set();
            }
        }
    }
}
