namespace Parlex {
    class DependencyCounter {
        public void Increment() {
            System.Threading.Interlocked.Increment(ref _count);
        }

        public bool Decrement() {
            return System.Threading.Interlocked.Decrement(ref _count) == 0;
        }
        private int _count;

    }
}
