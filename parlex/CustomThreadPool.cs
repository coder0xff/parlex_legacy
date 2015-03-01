//#define SINGLE_THREAD

using System;
using System.Threading;

namespace Parlex {
#if (SINGLE_THREAD)

    internal class CustomThreadPool {
        internal static void QueueUserWorkItem(WaitCallback callback) {
            callback(null);
        }
    }

#else

    internal class CustomThreadPool {
        private int itemCount;
        public event Action OnIdle = () => { };
 
        void ItemQueued() {
            Interlocked.Increment(ref itemCount);
        }

        void ItemCompleted() {
            if (Interlocked.Decrement(ref itemCount) == 0) {
                OnIdle();
            }
        }

        internal void QueueUserWorkItem(WaitCallback callback) {
            ItemQueued();
            ThreadPool.QueueUserWorkItem(_ => {
                callback(null);
                ItemCompleted();
            });
        }
    }

#endif
}
