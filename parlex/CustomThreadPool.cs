//#define SINGLE_THREAD

using System;
using System.Threading;

namespace Parlex {
#if (SINGLE_THREAD)

    internal class CustomThreadPool {

        public event Action OnIdle = () => { };
        public void QueueUserWorkItem(WaitCallback callback) {
            ++_nestCounter;
            callback(null);
            --_nestCounter;
            System.Diagnostics.Debug.WriteLine(_nestCounter);
            if (_nestCounter == 0) {
                OnIdle();
            }
        }
        private int _nestCounter;

    }

#else

    internal class CustomThreadPool {
        public event Action OnIdle = () => { };
 
        public void QueueUserWorkItem(WaitCallback callback) {
            ItemQueued();
            ThreadPool.QueueUserWorkItem(_ => {
                callback(null);
                ItemCompleted();
            });
        }
        private int itemCount;
        void ItemQueued() {
            Interlocked.Increment(ref itemCount);
        }

        void ItemCompleted() {
            if (Interlocked.Decrement(ref itemCount) == 0) {
                OnIdle();
            }
        }

    }

#endif
}
