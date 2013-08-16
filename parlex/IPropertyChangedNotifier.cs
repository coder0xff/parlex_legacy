using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parlex {
    public interface IPropertyChangedNotifier {
        event Action<Object, String> PropertyChanged;
    }
}
