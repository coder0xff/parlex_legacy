using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    public class GenericParseNodeFactory<T> : GenericParseNodeFactory, IParseNodeFactory where T : ParseNode, new() {
        public GenericParseNodeFactory() : base(typeof(T)) {
        }
    }

    public class GenericParseNodeFactory : IParseNodeFactory {
        private static AutoDictionary<Type, ParseNode> nodes;
        static GenericParseNodeFactory() {
            nodes = new AutoDictionary<Type, ParseNode>(t => (ParseNode)Activator.CreateInstance(t));
        }

        private readonly Type _t;
        private String _name;
        private bool _isGreedy;

        public GenericParseNodeFactory(Type t) {
            _t = t;
            _name = _t.Name;
            _isGreedy = _t.GetCustomAttributes(typeof(GreedyAttribute), false).Length > 0;
        }

        string IParseNodeFactory.Name {
            get { return _name; }
        }

        bool IParseNodeFactory.IsGreedy {
            get { return _isGreedy; }
        }

        ParseNode IParseNodeFactory.Create() {
            return nodes[_t];
        }

        bool IParseNodeFactory.Is(TerminalDefinition terminalDefinition) {
            return false;
        }

        bool IParseNodeFactory.Is(NfaProduction production) {
            return false;
        }

        public override int GetHashCode() {
            return _t.GetHashCode();
        }
    }
}
