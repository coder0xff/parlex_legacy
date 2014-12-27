﻿using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Synchronox {
    public class Input<T> : IInput {
        public Input(Node owner, CallProtector callProtector) {
            if (callProtector == null) throw new ApplicationException("Output may not be constructed by user code.");
            _owner = owner;
        }

        private bool _causedHalt;

        private readonly Queue<T> _queue = new Queue<T>();
 
        private readonly ConcurrentSet<Output<T>> _connectedOutputs = new ConcurrentSet<Output<T>>();

        internal void DidConnect(Output<T> output) {
            _sync.WaitOne();
            try {
                if (_causedHalt) {
                    throw new ApplicationException("The input previously detected that it would not receive any additional input. Check the logic of your program to make sure that connections are not created at times that may occur after all existing connections have been halted.");
                }
            } finally {
                _sync.ReleaseMutex();
            }
            _connectedOutputs.TryAdd(output);
        }

        internal void Enqueue(T datum, Output<T> sender) {
            _sync.WaitOne();
            if (_causedHalt) {
                throw new ApplicationException("The input previously detected that it would not receive any additional input. The owning node has been halted. No more input data may be transmitted to the node on this input.");
            }
            _queue.Enqueue(datum);
            _cv.Signal();
            _sync.ReleaseMutex();
        }

        private bool ComputeIsHalting() {
            if (_causedHalt) {
                return true;
            }
            if (_queue.Count > 0) return false;
            if (_connectedOutputs.Count == 0 || _connectedOutputs.All(connectedOutput => connectedOutput.Owner.IsHalted)) {
                _causedHalt = true;
                return true;
            }
            return false;
        }

        public bool Dequeue(out T datum) {
            datum = default(T);
            _sync.WaitOne();
            try {
                while (_queue.Count == 0) {
                    if (ComputeIsHalting()) {
                        return false;
                    }
                    _cv.Wait(_sync);
                }
                datum = _queue.Dequeue();
                return true;
            } finally {
                try {
                    _sync.ReleaseMutex();
                } catch {
                    
                }
            }
        }

        void IInput.CheckWillHalt() {
            _sync.WaitOne();
            _cv.Signal();
            _sync.ReleaseMutex();
        }

        public bool IsBlocked() {
            return _cv.WaitingThreadCount > 0;
        }

        public void SignalHalt() {
            _causedHalt = true;
            _cv.Signal();
        }

        public void Lock() {
            _sync.WaitOne();
        }

        public void Unlock() {
            _sync.ReleaseMutex();
        }

        private readonly Node _owner;
        private readonly Mutex _sync = new Mutex();
        private readonly ConditionVariable _cv = new ConditionVariable();
        IEnumerable<IOutput> IInput.GetConnectedOutputs() {
            return _connectedOutputs.ToArray();
        }

        /// <summary>
        /// Temporarily block the input from either being enqueued, dequeud, or waited upon.
        /// </summary>

        public Node Owner { get { return _owner; } }
    }
}