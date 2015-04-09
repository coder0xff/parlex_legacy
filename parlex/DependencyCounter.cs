using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    class DependencyCounter {
        private int _count;

        public void Increment() {
            System.Threading.Interlocked.Increment(ref _count);
        }

        public bool Decrement() {
            return System.Threading.Interlocked.Decrement(ref _count) == 0;
        }
    }
}
