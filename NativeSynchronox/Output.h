#ifndef _OUTPUT_H_
#define _OUTPUT_H_

#include <vector>
#include <mutex>

namespace Synchronox {
	class Box;

	class IInput;

	template<typename T>
	class Input;

	template<typename T>
	class Output
	{
	public:
		Output(Box* owner);
		~Output();
		void Enqueue(T datum);
		Box* GetOwner();
	private:
		friend class Collective;

		class Connection {
		public:
			Input<T> input;
			int nextTransmitDataIndex;
		};

		Box* owner;
		std::vector<T> data;
		std::mutex dataLock;
		std::vector<Connection> _connections;
		std::mutex connectionsLock;

		void Connect(Input<T> input);
		void DoTransmissions();
		std::vector<IInput*> GetConnectedInputs();
	};
}

#endif