using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Synchronox {
    public class Output<T> : IOutput {
        public Output(Box owner, CallProtector callProtector) {
            if (callProtector == null) throw new ApplicationException("Output may not be constructed by user code.");
            _owner = owner;
        }

        public void Enqueue(T datum) {
            _dataLock.EnterWriteLock();
            _data.Add(datum);
            _dataLock.ExitWriteLock();
            DoTransmissions();
        }

        private readonly Box _owner;
        private readonly List<T> _data = new List<T>();
        private readonly ReaderWriterLockSlim _dataLock = new ReaderWriterLockSlim();
        private readonly List<Connection> _connections = new List<Connection>();
        private readonly ReaderWriterLockSlim _connectionsLock = new ReaderWriterLockSlim();
        
        class Connection {
            public readonly Input<T> Input;
            public int NextTransmitDataIndex;
            public Connection(Input<T> input) {
                Input = input;
            }
        }

        internal void Connect(Input<T> input) {
            _connectionsLock.EnterWriteLock();
            _connections.Add(new Connection(input));
            _connectionsLock.ExitWriteLock();
            input.DidConnect(this);
            DoTransmissions();
        }

        private void DoTransmissions() {
            _connectionsLock.EnterReadLock();
            var temp = _connections.ToArray();
            _connectionsLock.ExitReadLock();
            foreach (var connection in temp) {
                lock (connection) {
                    while (connection.NextTransmitDataIndex < _data.Count) {
                        connection.Input.Enqueue(_data[connection.NextTransmitDataIndex++]);
                    }
                }
            }
        }

        IEnumerable<IInput> IOutput.GetConnectedInputs() {
            _connectionsLock.EnterReadLock();
            var temp = _connections.ToArray();
            _connectionsLock.ExitReadLock();
            return temp.Select(x => x.Input);
        }

        public Box Owner { get { return _owner; } }
    }
}
