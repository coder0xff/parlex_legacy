using System;
using System.Collections.Generic;

namespace parlex {
    public class Product : IEquatable<Product> {
        private readonly String _title;
        internal readonly List<CompiledGrammar.NfaSequence> Sequences = new List<CompiledGrammar.NfaSequence>();

        internal Product(String title) {
            _title = title;
        }

        public string Title {
            get { return _title; }
        }

        public bool Equals(Product other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(_title, other._title);
        }

        public override int GetHashCode() {
            return (_title != null ? _title.GetHashCode() : 0);
        }

        public static bool operator ==(Product left, Product right) {
            return Equals(left, right);
        }

        public static bool operator !=(Product left, Product right) {
            return !Equals(left, right);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Product)obj);
        }

        public override string ToString() {
            return _title;
        }
    }
}
