using System;

namespace Parlex {
    public class Production : Recognizer {
        public override string Name { get { return _name; } }

        public override bool IsGreedy {
            get { return _greedy; }
        }

        public BehaviorTree Behavior { get; set; }
        public int Precedence { get; set; }
        public Associativity Associativity { get; set; }
        public Production(String name, bool greedy = false) {
            _name = name;
            _greedy = greedy;
        }

        public override void Start() {
            throw new NotSupportedException("Productions must first be converted to NfaProductions to be used for parsing.");
        }

        public void SetName(String name) {
            _name = name;
        }

        public void SetIsGreedy(bool greedy) {
            _greedy = greedy;
        }

        private String _name;
        private Boolean _greedy;

    }
}
