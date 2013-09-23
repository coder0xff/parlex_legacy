using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;

namespace parlex {
    public class Product : IEquatable<Product> {
        public readonly String _title;
        public readonly List<CompiledGrammar.NfaSequence> Sequences = new List<CompiledGrammar.NfaSequence>();

        public Product(String title) {
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

        public virtual string GetExample() {
            return Sequences.Select(sequence => sequence.GetExample()).Where(example => example != null).OrderBy(example => Rng.Next()).FirstOrDefault();
        }

        public void ReplaceSequences(Product other) {
            Sequences.Clear();
            Sequences.AddRange(other.Sequences);
        }
    }
}
