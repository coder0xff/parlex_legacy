using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.More;

namespace System.Collections.Concurrent.More
{
    public class AsyncSet<T> : IEnumerable<T>, IDisposable
    {
        List<T> storage = new List<T>();
        HashSet<T> tester = new HashSet<T>();
        bool closed = false;
        System.Threading.ReaderWriterLockSlim sync = new ReaderWriterLockSlim();
        System.Threading.More.ConditionVariable cv = new ConditionVariable();

        public void Add(T item)
        {
            sync.EnterWriteLock();
            if (closed)
            {
                throw new InvalidOperationException("The set has been closed.");
            }
            if (tester.Add(item))
            {
                storage.Add(item);
                cv.Broadcast();
            }
            sync.ExitWriteLock();
        }

        public void Close()
        {
            sync.EnterWriteLock();
            closed = true;
            cv.Broadcast();
            sync.ExitWriteLock();
        }

        public IEnumerator<T> GetEnumerator()
        {
            int index = 0;
            sync.EnterReadLock();
            while (!closed)
            {
                while (index < storage.Count)
                {
                    T item = storage[index++];
                    sync.ExitReadLock();
                    yield return item;
                    sync.EnterReadLock();
                }
                cv.WaitRead(sync);
            }
            sync.ExitReadLock();
            while (index < storage.Count)
            {
                yield return storage[index++];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposeManaged)
        {
            sync.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
