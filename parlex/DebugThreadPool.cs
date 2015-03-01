//#define SINGLE_THREAD

using System.Threading;

namespace Parlex {
#if (SINGLE_THREAD)

    internal static class DebugThreadPool {
        internal static void QueueUserWorkItem(WaitCallback callback) {
            callback(null);
        }
    }

#else

    internal static class DebugThreadPool {
        internal static void QueueUserWorkItem(WaitCallback callback) {
            ThreadPool.QueueUserWorkItem(callback);
        }
    }

#endif
}
