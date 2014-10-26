using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Concurrent.More {
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix"), SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class JaggedAutoDictionary<T0, TValue> : AutoDictionary<T0, TValue> {
        public JaggedAutoDictionary(Func<T0, TValue> valueFactory)
            : base(valueFactory) {}

        public JaggedAutoDictionary() {}
    }

    [SuppressMessage("Microsoft.Design", "CA1005:AvoidExcessiveParametersOnGenericTypes"), SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix"), SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public class JaggedAutoDictionary<T0, T1, TValue> : AutoDictionary<T0, JaggedAutoDictionary<T1, TValue>> {
        public JaggedAutoDictionary(Func<T0, T1, TValue> valueFactory)
            : base(forward0 => new JaggedAutoDictionary<T1, TValue>(forward1 => valueFactory(forward0, forward1))) {}

        public JaggedAutoDictionary()
            : base(forward0 => new JaggedAutoDictionary<T1, TValue>()) {}
    }

    [SuppressMessage("Microsoft.Design", "CA1005:AvoidExcessiveParametersOnGenericTypes"), SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix"), SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class JaggedAutoDictionary<T0, T1, T2, TValue> : AutoDictionary<T0, JaggedAutoDictionary<T1, T2, TValue>> {
        public JaggedAutoDictionary(Func<T0, T1, T2, TValue> valueFactory)
            : base(forward0 => new JaggedAutoDictionary<T1, T2, TValue>((forward1, forward2) => valueFactory(forward0, forward1, forward2))) {}

        public JaggedAutoDictionary()
            : base(forward0 => new JaggedAutoDictionary<T1, T2, TValue>()) {}
    }
}