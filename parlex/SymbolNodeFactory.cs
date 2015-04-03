﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parlex {
    internal class SymbolNodeFactory : IParseNodeFactory {
        protected bool Equals(SymbolNodeFactory other) {
            return Equals(_production, other._production) && Equals(_terminal, other._terminal);
        }

        public override int GetHashCode() {
            unchecked {
                return ((_production != null ? _production.GetHashCode() : 0)*397) ^ (_terminal != null ? _terminal.GetHashCode() : 0);
            }
        }

        private readonly NfaProduction _production;
        private readonly ITerminal _terminal;

        public SymbolNodeFactory(ISymbol symbol) {
            _production = symbol as NfaProduction;
            if (_production == null) {
                _terminal = symbol as ITerminal;
                System.Diagnostics.Debug.Assert(_terminal != null);
            }
        }

        public string Name {
            get {
                if (_production != null) {
                    return _production.Name;
                }
                return _terminal.Name;
            }
        }

        public bool IsGreedy {
            get {
                if (_production != null) {
                    return _production.Greedy;
                }
                return false;
            }
        }

        public ParseNode Create() {
            if (_production != null) {
                return new NfaSymbolNode(_production);
            }
            return new TerminalParseNode(_terminal);
        }

        public bool Is(ITerminal terminal) {
            return _terminal == terminal;
        }

        public bool Is(NfaProduction production) {
            return _production == production;
        }

        public override string ToString() {
            return Name;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != this.GetType()) {
                return false;
            }
            return Equals((SymbolNodeFactory)obj);
        }
    }
}
