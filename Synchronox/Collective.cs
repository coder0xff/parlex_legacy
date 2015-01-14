using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;

namespace Synchronox {
    public abstract class Collective {
        protected Collective() {
            ThreadProvider.Default.Start(DeadlockDetector);
        }

        protected void ConstructionCompleted() {
            StartBlocker.Set();
        }

        /// <summary>
        /// Perform any cleanup after all boxes have halted
        /// </summary>
        protected virtual void Terminator() { }

        internal readonly ManualResetEventSlim StartBlocker = new ManualResetEventSlim();

        private int _haltedBoxCount;

        internal void Add(Box box) {
            if (box == null) {
                throw new ArgumentNullException();
            }
            lock (_boxes) {
                _boxes.Add(box);
                _toJoin.Enqueue(box);
            }
        }

        public void Connect<T>(Input<T> input, Output<T> output) {
            input.Owner.VerifyConstructionCompleted();
            output.Owner.VerifyConstructionCompleted();
            if (input.Owner.Collective != this || output.Owner.Collective != this) {
                throw new ApplicationException("The input and output must belong to the called instance of Collective.");
            }
            output.Connect(input);
        }

        public bool IsDone() {
            return _blocker.IsSet;
        }

        public void Join() {
            _blocker.Wait();
            Box box;
            while (_toJoin.TryDequeue(out box)) {
                box.Join();
            }
        }

        /// <summary>
        /// Walks up the Box dependency graph and halts any Boxes waiting on this Box
        /// Then, continue recursively
        /// </summary>
        /// <param name="box"></param>
        internal static void PropagateHalt(Box box) {
            var dependentInputs = box.GetOutputs().SelectMany(output => output.GetConnectedInputs()).Distinct();
            var dependentBoxes = dependentInputs.Select(input => input.Owner).Distinct();
            foreach (var dependentBox in dependentBoxes) {
                if (!dependentBox.IsHalted) {
                    foreach (var input in dependentBox.GetInputs()) {
                        input.CheckWillHalt();
                    }
                }
            }
        }

        private readonly ManualResetEventSlim _blocker = new ManualResetEventSlim(false);
        private readonly List<Box> _boxes = new List<Box>();
        private readonly ConcurrentQueue<Box> _toJoin = new ConcurrentQueue<Box>();

        internal void BoxHalted() {
            if (Interlocked.Increment(ref _haltedBoxCount) == _boxes.Count) {
                Terminator();
                _blocker.Set();
            }
        }

        private static bool DeadlockBreaker(HashSet<Box> blockedSet, Dictionary<Box, Box[]> dependenciesTable, bool doHalt) {
            var anyChanged = true;
            while (anyChanged) {
                anyChanged = false;
                foreach (var blocked in blockedSet) {
                    var dependencies = dependenciesTable[blocked];
                    if (dependencies.Length == 0 || dependencies.Any(box => !blockedSet.Contains(box))) {
                        blockedSet.Remove(blocked);
                        anyChanged = true;
                        break;
                    }
                }
            }
            if (blockedSet.Count > 0) {
                if (doHalt) {
                    var box = blockedSet.First();
                    var input = box.GetInputs().First(i => i.IsBlocked);
                    input.SignalHalt();
                }
                return true;
            }
            if (doHalt) {
                Console.WriteLine("A collective detected that a deadlock might be occurring, but it was a false positive.");
            }
            return false;
        }


        private void DeadlockDetector() {
            var needFullTest = false;
            while (!IsDone()) {
                Box[] boxesCopy;
                if (needFullTest) {
                    lock (_boxes) {
                        boxesCopy = _boxes.ToArray();
                    }
                    Thread.CurrentThread.Priority = ThreadPriority.Normal;
                    foreach (var box in boxesCopy) {
                        box.Lock();
                    }
                    //This branch does require locking
                    //It does not generate false positives
                    //and will take action (halt a box) when
                    //it finds a dead lock
                    var blockedSet = new HashSet<Box>(boxesCopy.Where(box => box.GetInputs().Any(i => i.IsBlocked)));
                    var dependenciesTable = blockedSet.ToDictionary(k => k, k => k.GetInputs().First(i => i.IsBlocked).GetConnectedOutputs().Select(o => o.Owner).Where(d => !d.IsHalted).Distinct().ToArray());
                    DeadlockBreaker(blockedSet, dependenciesTable, true);
                    needFullTest = false;
                    foreach (var box in boxesCopy) {
                        box.Unlock();
                    }
                } else {
                    //this branch doesn't require any locking
                    //it could produce false positives though
                    lock (_boxes) {
                        boxesCopy = _boxes.ToArray();
                    }
                    Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                    var blockedBoxToInput = boxesCopy.ToDictionary(box => box, box => box.GetInputs().FirstOrDefault(input => input.IsBlocked));
                    var blockedSet = new HashSet<Box>(blockedBoxToInput.Where(kvp => kvp.Value != null).Select(kvp => kvp.Key));
                    var dependenciesTable = blockedSet.ToDictionary(k => k, k => blockedBoxToInput[k].GetConnectedOutputs().Select(o => o.Owner).Distinct().Where(d => !d.IsHalted).ToArray());
                    needFullTest = DeadlockBreaker(blockedSet, dependenciesTable, false);
                }
            }
        }
    }
}
