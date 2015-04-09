using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    public class Production : RecognizerDefinition {
        private String _name;

        public Production(String name) {
            _name = name;
        }

        public override string Name { get { return _name; } }
        public void SetName(String name) {
            _name = name;
        }
        public BehaviorTree Behavior { get; set; }
        public int Precedence { get; set; }
        public Associativity Associativity { get; set; }
        public Boolean Greedy { get; set; }
    }
}
