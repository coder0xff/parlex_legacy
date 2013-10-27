using System;

namespace Common {
    public interface IPropertyChangedNotifier {
        event Action<Object, String> PropertyChanged;
    }
}
