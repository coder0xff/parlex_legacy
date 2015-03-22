using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    public class Production : ISymbol {
        public string Name { get; set; }
        public BehaviorTree Behavior { get; set; }
        public int Precedence { get; set; }
        public Associativity Associativity { get; set; }
        public Boolean Greedy { get; set; }
    }
}
