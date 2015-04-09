using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    class PlaceholderIRecognizerDefinition : RecognizerDefinition {
        private readonly String _name;
        public PlaceholderIRecognizerDefinition(String name) {
            _name = name;
        }

        public override string Name { get { return _name; } }
    }
}
