using System;
using System.Collections.Generic;

namespace parlex {
    public class Product : IEquatable<Product> {
        public bool Equals(Product other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Title, other.Title);
        }

        public override int GetHashCode() {
            return (Title != null ? Title.GetHashCode() : 0);
        }

        public static bool operator ==(Product left, Product right) {
            return Equals(left, right);
        }

        public static bool operator !=(Product left, Product right) {
            return !Equals(left, right);
        }

        public readonly String Title;
        internal readonly List<GrammarAnalyzer.NfaSequence> Sequences = new List<GrammarAnalyzer.NfaSequence>();

        internal Product(String title) {
            Title = title;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Product)obj);
        }

        public override string ToString() {
            return Title;
        }
    }
}
