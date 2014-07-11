using System.Collections.Concurrent;
using System.Collections.Concurrent.More;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading.More {
        /// <summary>
        /// Processes a recursive algorithm in parallel. 
        /// It is initialized with a starting set of 
        /// distinct items. Run is called with the recursive 
        /// algorithm, which may add more distinct items.
        /// These added items will also be processed, 
        /// recursively, until no more items are added.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class DistinctRecursiveAlgorithmProcessor<T> {
            readonly ConcurrentQueue<T> _items = new ConcurrentQueue<T>();
            readonly ConcurrentSet<T> _alreadyQueuedItems = new ConcurrentSet<T>();

            /// <summary>
            /// Add an item to process. If it is identical to a previously added item,
            /// it will not be added. Thus, only distinct items will be processed.
            /// </summary>
            /// <param name="item"></param>
            /// <returns>False if the item has been previously added</returns>
            public bool Add(T item) {
                if (!_alreadyQueuedItems.TryAdd(item)) {
                    return false;
                }
                _items.Enqueue(item);
                return true;
            }

            /// <summary>
            /// Runs the recursive algorithm specified, and blocks until it is complete.
            /// </summary>
            /// <param name="recursiveAlgorithm"></param>
            [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
            public void Run(Action<T> recursiveAlgorithm) {
                while(_items.Count > 0) {
                    Parallel.For(0, _items.Count, dontCare => {
                        T item;
                        if (_items.TryDequeue(out item))
                        {
                            recursiveAlgorithm(item);
                        }
                        else
                        {
                            throw new ApplicationException("An unknown error occurred.");
                        }
                    });
                }
            }
        }
    }
