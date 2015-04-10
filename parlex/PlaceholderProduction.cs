using System;

namespace Parlex {
    class PlaceholderProduction : Production {
        public PlaceholderProduction(String name) : base(name, false) {
        }

        public override bool IsGreedy {
            get { throw new ApplicationException("Placeholder recognizers cannot be used to parse."); }
        }
    }
}
