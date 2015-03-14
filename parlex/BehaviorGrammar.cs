using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    public class BehaviorGrammar {
        public class Production {
            public String Name;
            public BehaviorTree Behavior;
        }

        public List<Production> Productions;

        public Production Main;
    }
}
