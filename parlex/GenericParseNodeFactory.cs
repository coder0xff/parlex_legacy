using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    public class GenericParseNodeFactory<T> : IParseNodeFactory where T : ParseNode, new() {
        public string Name { get; private set; }
        public bool IsGreedy { get; private set; }

        ParseNode IParseNodeFactory.Create() {
            return new T();
        }

        bool IParseNodeFactory.Is(ITerminal terminal) {
            return false;
        }

        bool IParseNodeFactory.Is(NfaProduction production) {
            return false;
        }

        public GenericParseNodeFactory() {
            Name = typeof(T).Name;
            IsGreedy = typeof(T).GetCustomAttributes(typeof(GreedyAttribute), false).Length > 0;
        }

        public static IParseNodeFactory FromType(Type t) {
            var t2 = typeof(GenericParseNodeFactory<>).MakeGenericType(t);
            return (IParseNodeFactory)Activator.CreateInstance(t2);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            var asNonGeneric = obj as GenericParseNodeFactory;
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

    public class GenericParseNodeFactory : IParseNodeFactory {
        private Type _t;
        internal IParseNodeFactory _backing;

        public GenericParseNodeFactory(Type t) {
            _t = t;
            var t2 = typeof(GenericParseNodeFactory<>).MakeGenericType(t);
            _backing = (IParseNodeFactory)Activator.CreateInstance(t2);
        }

        string IParseNodeFactory.Name {
            get { return _backing.Name; }
        }

        bool IParseNodeFactory.IsGreedy {
            get { return _backing.IsGreedy; }
        }

        ParseNode IParseNodeFactory.Create() {
            return _backing.Create();
        }

        bool IParseNodeFactory.Is(ITerminal terminal) {
            return _backing.Is(terminal);
        }

        bool IParseNodeFactory.Is(NfaProduction production) {
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
