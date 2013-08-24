namespace System.Collections.Concurrent.More {
    public class JaggedAutoDictionary<T0, TV> : AutoDictionary<T0, TV> {
        public JaggedAutoDictionary(Func<T0, TV> valueFactory) : base(valueFactory) {
        }

        public JaggedAutoDictionary() {            
        } 
    }

    public class JaggedAutoDictionary<T0, T1, TV> : AutoDictionary<T0, JaggedAutoDictionary<T1, TV>> {
        public JaggedAutoDictionary(Func<T0, T1, TV> valueFactory) : base(forward0 => new JaggedAutoDictionary<T1, TV>(forward1 => valueFactory(forward0, forward1))) {
        }

        public JaggedAutoDictionary() : base(forward0 => new JaggedAutoDictionary<T1, TV>()) {
        }
    }

    public class JaggedAutoDictionary<T0, T1, T2, TV> : AutoDictionary<T0, JaggedAutoDictionary<T1, T2, TV>> {
        public JaggedAutoDictionary(Func<T0, T1, T2, TV> valueFactory)
            : base(forward0 => new JaggedAutoDictionary<T1, T2, TV>((forward1, forward2) => valueFactory(forward0, forward1, forward2))) {
        }

        public JaggedAutoDictionary()
            : base(forward0 => new JaggedAutoDictionary<T1, T2, TV>()) {
        }
    }
}