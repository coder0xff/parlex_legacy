using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Common;
using parlex.Annotations;

namespace parlex {
    public class OldProduction : IEquatable<OldProduction>, INotifyPropertyChanged {
        private readonly String _title;
        public readonly List<CompiledGrammar.NfaSequence> Sequences = new List<CompiledGrammar.NfaSequence>();

        public OldProduction(String title) {
            _title = title;
        }

        public string Title {
            get { return _title; }
            //set {
            //    if (value != _title) {
            //        _title = value;
            //        PropertyChanged();
            //    }
            //}
        }

        public bool Equals(OldProduction other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(_title, other._title);
        }

        public override int GetHashCode() {
            return _title.GetHashCode();
        }

        public static bool operator ==(OldProduction left, OldProduction right) {
            return Equals(left, right);
        }

        public static bool operator !=(OldProduction left, OldProduction right) {
            return !Equals(left, right);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((OldProduction)obj);
        }

        public override string ToString() {
            return _title;
        }

        public virtual string GetExample() {
            return Sequences.Select(sequence => sequence.GetExample()).Where(example => example != null).OrderBy(example => Rng.Next()).FirstOrDefault();
        }

        public void ReplaceSequences(OldProduction other) {
            Sequences.Clear();
            Sequences.AddRange(other.Sequences);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
