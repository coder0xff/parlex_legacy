using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections.Concurrent.More;

namespace parlex {
	public interface IReceiver<T> {
		Action<T> GetReceiveHandler();

		Action GetTerminateHandler();
	}

	public interface ITransmitter<T> {
		event Action<T> Transmit;
		event Action Terminate;
	}

	public class Transponder<T> {
		private List<Action<T>> receiveHandlers = new List<Action<T>>();
		private List<Action> terminateHandlers = new List<Action>();
		private List<ITransmitter<T>> transmitters = new List<ITransmitter<T>>();
		private ConcurrentSet<T> values = new ConcurrentSet<T>();
		private bool terminated = false;

		public Transponder() {
			incoming_value_delegate = incoming_value_handler;
		}

		private void incoming_value_handler(T value) {
			lock (transmitters) {
				if (terminated) {
					throw new InvalidOperationException("New values may not be passed through a Transponder<T> once all its registered IReceiver<T>s have terminated.");
				}
				if (values.TryAdd(value)) {
					lock (receiveHandlers) {
						foreach (Action<T> receiveHandler in receiveHandlers) {
							ThreadPool.QueueUserWorkItem(_ => receiveHandler(value));
						}
					}
				}
			}
		}

		private Action<T> incoming_value_delegate;

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
					lock (receiveHandlers) {
						foreach (Action terminateHandler in terminateHandlers) {
							ThreadPool.QueueUserWorkItem(_ => terminateHandler());
						}
					}
				}
			};
		}

		public void Register(IReceiver<T> receiver) {
			lock (transmitters) {
				lock (receiveHandlers) {
					Action<T> receiveHandler = receiver.GetReceiveHandler();
					Action terminateHandler = receiver.GetTerminateHandler();
					receiveHandlers.Add(receiveHandler);
					terminateHandlers.Add(terminateHandler);
					foreach (T value in values) {
						receiveHandler(value);
					}
					if (terminated) {
						terminateHandler();
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

