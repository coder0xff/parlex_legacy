//#define SINGLE_THREAD_MODE

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections.Concurrent.More;

namespace Parlex {
	public interface IReceiver<T> {
		Action<T> GetReceiveHandler();
		Action GetTerminateHandler();
	}

	public interface ITransmitter<T> {
		event Action<T> Transmit;
		event Action Terminate;
	}

	public class Transponder<T> {
		public class Desynchronizer {
			#if SINGLE_THREAD_MODE
			#else
			int numberOfPendingTransmits = 0;
			ManualResetEventSlim mayTerminate = new ManualResetEventSlim(false);
			#endif
			Action<T> receive;
			Action terminate;

			public Desynchronizer(IReceiver<T> receiver) {
				receive = receiver.GetReceiveHandler();
				terminate = receiver.GetTerminateHandler();
			}

			public void Transmit(T value) {
				#if SINGLE_THREAD_MODE
				receive(value);
				#else
				System.Threading.Interlocked.Increment(ref numberOfPendingTransmits);
				new Thread(_ => {
					try {
						receive(value);
					}
					finally {
						if (System.Threading.Interlocked.Decrement(ref numberOfPendingTransmits) == 0) {
							mayTerminate.Set();
						}
					}
				}).Start();
				#endif
			}

			public void Terminate() {
				#if SINGLE_THREAD_MODE
				terminate();
				#else
				new Thread(_ => {
					mayTerminate.Wait();
					terminate();
				}).Start();
				#endif
			}
		}

		public List<Desynchronizer> receivers = new List<Desynchronizer>();
		public List<ITransmitter<T>> transmitters = new List<ITransmitter<T>>();
		public ConcurrentSet<T> values = new ConcurrentSet<T>();
		public bool terminated = false;

		public Transponder() {
			incoming_value_delegate = incoming_value_handler;
		}

		public void incoming_value_handler(T value) {
			lock (transmitters) {
				if (terminated) {
					throw new InvalidOperationException("New values may not be passed through a Transponder<T> once all its registered IReceiver<T>s have terminated.");
				}
				if (values.TryAdd(value)) {
					lock (receivers) {
						foreach (Desynchronizer receiver in receivers) {
							receiver.Transmit(value);
						}
					}
				}
			}
		}

		public Action<T> incoming_value_delegate;

		public void Register(ITransmitter<T> transmitter) {
			lock (transmitters) {
				if (terminated) {
					throw new InvalidOperationException("This Transponder has already called Terminate on its registered IReceiver<T>s. No new ITransmitter<T>s can be registered.");
				}
				transmitters.Add(transmitter);
			}
			transmitter.Transmit += incoming_value_delegate;
			transmitter.Terminate += () => {
				bool didTerminate = false;
				lock (transmitters) {
					if (transmitters.Remove(transmitter)) {
						if (transmitters.Count == 0) {
							terminated = true;
							didTerminate = true;
						}
					} else {
						throw new InvalidOperationException("The specified ITransmitter<T> was not registered to this Transpoder<T>");
					}
				}
				if (didTerminate) {
					lock (receivers) {
						foreach (Desynchronizer receiver in receivers) {
							receiver.Terminate();
						}
					}
				}
			};
		}

		public void Register(IReceiver<T> receiver) {
			lock (transmitters) {
				lock (receivers) {
					Desynchronizer desyncReceiver = new Desynchronizer(receiver);
					receivers.Add(desyncReceiver);
					foreach (T value in values) {
						desyncReceiver.Transmit(value);
                    }
					if (terminated) {
						desyncReceiver.Terminate();
                    }
				}
			}
		}

		public IEnumerable<T> GetAllValues() {
			lock (transmitters) {
				if (!terminated) {
					throw new InvalidOperationException("The listing of values that passed through a Transponder<T> cannot be retrieved until all its registered ITransmitters<T> have terminated.");
				}
			}
			return values;
		}
	}
}

