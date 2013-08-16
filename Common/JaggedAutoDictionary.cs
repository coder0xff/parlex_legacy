using System;

namespace Common {
    public class JaggedAutoDictionary<T0, TV> : AutoDictionary<T0, TV> {
        public JaggedAutoDictionary(Func<TV> valueFactory) : base(valueFactory) {
        }

        public JaggedAutoDictionary() : base() {            
        } 
    }

    public class JaggedAutoDictionary<T0, T1, TV> : AutoDictionary<T0, JaggedAutoDictionary<T1, TV>> {
        public JaggedAutoDictionary(Func<TV> valueFactory) : base(() => new JaggedAutoDictionary<T1, TV>(valueFactory)) {            
        }

        public JaggedAutoDictionary() : base(() => new JaggedAutoDictionary<T1, TV>()) {            
        }
    }

    public class JaggedAutoDictionary<T0, T1, T2, TV> : AutoDictionary<T0, JaggedAutoDictionary<T1, T2, TV>> {
        public JaggedAutoDictionary(Func<TV> valueFactory)
            : base(() => new JaggedAutoDictionary<T1, T2, TV>(valueFactory)) {
        }

        public JaggedAutoDictionary()
            : base(() => new JaggedAutoDictionary<T1, T2, TV>()) {
        }
    }
}
