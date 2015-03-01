using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    class GenericSyntaxNodeFactory<T> : ISyntaxNodeFactory where T : SyntaxNode, new() {
        public string Name { get; private set; }
        public bool IsGreedy { get; private set; }

        public SyntaxNode Create() {
            return new T();
        }

        public bool Is(Grammar.ITerminal terminal) {
            return false;
        }

        public bool Is(Grammar.Production production) {
            return false;
        }

        public GenericSyntaxNodeFactory(bool greedy) {
            Name = typeof(T).Name;
            IsGreedy = greedy;
        }
    }
}
