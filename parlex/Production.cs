using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    public class Production : Recognizer {
        private String _name;
        private Boolean _greedy;

        public Production(String name, bool greedy = false) {
            _name = name;
            _greedy = greedy;
        }

        public override string Name { get { return _name; } }

        public override bool IsGreedy {
            get { return _greedy; }
        }

        public override void Start() {
            throw new NotImplementedException();
        }

        public void SetName(String name) {
            _name = name;
        }

        public void SetIsGreedy(bool greedy) {
            _greedy = greedy;
        }

        public BehaviorTree Behavior { get; set; }
        public int Precedence { get; set; }
        public Associativity Associativity { get; set; }
    }
}
