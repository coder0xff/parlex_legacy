#ifndef _OUTPUT_H_
#define _OUTPUT_H_

#include <vector>
#include <mutex>
#include "IOutput.h"

namespace Synchronox {
	class Box;

	class IInput;

	template<typename T>
	class Input;

	template<typename T>
	class Output : public IOutput<T> final
	{
	public:
		Output(Box* owner) : owner(owner) { }

		~Output();

		void Enqueue(T datum) {
			data.push_back(datum);
			DoTransmissions();
		}

		Box* GetOwner() {
			return owner;
		}
	private:
		friend class Collective;

		class Connection {
		public:
			Input<T> *input;
			int nextTransmitDataIndex;
		};

		Box* owner;
		std::vector<T> data;
		std::vector<Connection> connections;
		std::mutex connectionsLock;

		void Connect(Input<T> input) {
			{
				std::unique_lock<std::mutex> l(connectionsLock);
				connections.push_back(Connection{ input, 0 });
			}
			DoTransmissions();
		}

		void DoTransmissions() {
			std::unique_lock<std::mutex> l(connectionsLock);
			for (auto &connection : connections) {
				while (connection.nextTransmitDataIndex < data.size()) {
					connection.input->Enqueue(data[connection.nextTransmitDataIndex++]);
				}
			}
		}

		std::vector<IInput*> GetConnectedInputs() {
			std::vector<IInput*> results;
			std::unique_lock<std::mutex> l(connectionsLock);
			for (auto &connection : connections) {
				results.push_back(connection.input);
			}
			return results;
		}
	};
}

#endif