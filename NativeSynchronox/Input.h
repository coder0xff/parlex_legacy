#ifndef _INPUT_H_
#define _INPUT_H_

#include <queue>
#include "IInput.h"
#include "ConcurrentSet.h"
#include "ConditionVariable.h"

namespace Synchronox {
	class Box;

	class IOutput;

	template<typename T>
	class Output;

	template<typename T>
	class Input : public IInput
	{
	public:
		Input(Box* owner);
		bool Dequeue(T& datum);
		bool GetIsBlocked();
	protected:
		std::vector<IOutput*> const GetConnectedOutputs();
		Box* GetOwner();
	private:
		friend class Collective;
		template<typename T>
		friend class Output;
		friend class Box;
		bool causedHalt;
		std::queue<T> queue;
		ConcurrentSet<Output<T>*> connectedOutputs;
		Box* owner;
		std::mutex sync;
		ConditionVariable cv;
		void DidConnect(Output<T>* output);
		void Enqueue(T datum);
		bool ComputeIsHalting();
		void CheckWillHalt();
		void SignalHalt();
		void Lock();
		void Unlock();
	};
}
#endif