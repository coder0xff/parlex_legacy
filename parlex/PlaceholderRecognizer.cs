using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    class PlaceholderRecognizer : Recognizer {
        private readonly String _name;
        public PlaceholderRecognizer(String name) {
            _name = name;
        }

        public override string Name { get { return _name; } }

        public override bool IsGreedy {
            get { throw new ApplicationException("Placeholder recognizers cannot be used to parse."); }
        }

        public override void Start() {
            throw new ApplicationException("Placeholder recognizers cannot be used to parse.");
        }
    }
}
