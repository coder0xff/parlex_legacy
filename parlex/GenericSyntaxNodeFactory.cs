using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    public class GenericSyntaxNodeFactory<T> : ISyntaxNodeFactory where T : SyntaxNode, new() {
        public string Name { get; private set; }
        public bool IsGreedy { get; private set; }

        SyntaxNode ISyntaxNodeFactory.Create() {
            return new T();
        }

        bool ISyntaxNodeFactory.Is(ITerminal terminal) {
            return false;
        }

        bool ISyntaxNodeFactory.Is(NfaProduction production) {
            return false;
        }

        public GenericSyntaxNodeFactory() {
            Name = typeof(T).Name;
            IsGreedy = typeof(T).GetCustomAttributes(typeof(GreedyAttribute), false).Length > 0;
        }

        public static ISyntaxNodeFactory FromType(Type t) {
            var t2 = typeof(GenericSyntaxNodeFactory<>).MakeGenericType(t);
            return (ISyntaxNodeFactory)Activator.CreateInstance(t2);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            var asNonGeneric = obj as GenericSyntaxNodeFactory;
            if (asNonGeneric != null) {
                return Equals(asNonGeneric._backing);
            }
            if (obj.GetType() != GetType()) {
                return false;
            }
            return true;
        }

        public override int GetHashCode() {
            return typeof(T).GetHashCode();
        }
    }

    public class GenericSyntaxNodeFactory : ISyntaxNodeFactory {
        private Type _t;
        internal ISyntaxNodeFactory _backing;

        public GenericSyntaxNodeFactory(Type t) {
            _t = t;
            var t2 = typeof(GenericSyntaxNodeFactory<>).MakeGenericType(t);
            _backing = (ISyntaxNodeFactory)Activator.CreateInstance(t2);
        }

        string ISyntaxNodeFactory.Name {
            get { return _backing.Name; }
        }

        bool ISyntaxNodeFactory.IsGreedy {
            get { return _backing.IsGreedy; }
        }

        SyntaxNode ISyntaxNodeFactory.Create() {
            return _backing.Create();
        }

        bool ISyntaxNodeFactory.Is(ITerminal terminal) {
            return _backing.Is(terminal);
        }

        bool ISyntaxNodeFactory.Is(NfaProduction production) {
            return _backing.Is(production);
        }

        public override bool Equals(object obj) {
            return _backing.Equals(obj);
        }

        public override int GetHashCode() {
            return _t.GetHashCode();
        }
    }
}
