using System;
using System.Collections.Generic;
using Common;

namespace System.Collections.Generic.More {
    public class ObservableList<T> : IList<T> where T : IPropertyChangedNotifier {
        private readonly List<T> _storage = new List<T>();

        public event Action<int> ItemInserted = delegate { };
        public event Action<int, T> ItemRemoved = delegate { }; 
        public event Action<T> ItemChanged = delegate { };

        private readonly Action<Object, String> _itemProperyChangedEventHandler;

        public ObservableList() {
            _itemProperyChangedEventHandler = (sender, name) => ItemChanged((T) sender);
        }

        public int IndexOf(T item) {
            return _storage.IndexOf(item);
        }

        public void Insert(int index, T item) {
            _storage.Insert(index, item);
            item.PropertyChanged += _itemProperyChangedEventHandler;
            ItemInserted(index);
        }

        public void RemoveAt(int index) {
            var o = _storage[index];
            _storage.RemoveAt(index);
            o.PropertyChanged -= _itemProperyChangedEventHandler;
            ItemRemoved(index, o);
        }

        public T this[int index] {
            get { return _storage[index]; }
            set {
                if (!value.Equals(_storage[index])) {
                    RemoveAt(index);
                    Insert(index, value);
                }
            }
        }

        public void Add(T item) {
            _storage.Add(item);
            item.PropertyChanged += _itemProperyChangedEventHandler;
            ItemInserted(_storage.Count - 1);
        }

        public void Clear() {
            while(Count > 0) {
                RemoveAt(0);
            }
        }

        public bool Contains(T item) {
            return _storage.Contains(item);
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex) {
            _storage.CopyTo(array, arrayIndex);
        }

        public int Count {
            get { return _storage.Count; }
        }

        public bool IsReadOnly {
            get { return false; }
        }

        public bool Remove(T item) {
            int index = _storage.IndexOf(item);
            if (index > -1) {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public IEnumerator<T> GetEnumerator() {
            return _storage.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return _storage.GetEnumerator();
        }
    }
    
    public class ObservableListOfImmutable<T> : IList<T> {
        private readonly List<T> _storage = new List<T>();

        public event Action<int> ItemInserted = delegate { };
        public event Action<int, T> ItemRemoved = delegate { };

        public int IndexOf(T item) {
            return _storage.IndexOf(item);
        }

        public void Insert(int index, T item) {
            _storage.Insert(index, item);
            ItemInserted(index);
        }

        public void RemoveAt(int index) {
            var o = _storage[index];
            _storage.RemoveAt(index);
            ItemRemoved(index, o);
        }

        public T this[int index] {
            get { return _storage[index]; }
            set {
                if (!value.Equals(_storage[index])) {
                    RemoveAt(index);
                    Insert(index, value);
                }
            }
        }

        public void Add(T item) {
            _storage.Add(item);
            ItemInserted(_storage.Count - 1);
        }

        public void Clear() {
            while (Count > 0) {
                RemoveAt(0);
            }
        }

        public bool Contains(T item) {
            return _storage.Contains(item);
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex) {
            _storage.CopyTo(array, arrayIndex);
        }

        public int Count {
            get { return _storage.Count; }
        }

        public bool IsReadOnly {
            get { return false; }
        }

        public bool Remove(T item) {
            int index = _storage.IndexOf(item);
            if (index > -1) {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public IEnumerator<T> GetEnumerator() {
            return _storage.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return _storage.GetEnumerator();
        }
    }
}
