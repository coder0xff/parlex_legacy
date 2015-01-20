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
	class Input : public IInput<T> final
	{
	public:
		Input(Box* owner) : owner(owner) {
			owner->_internal_use_only_register_input(this);
		}

		void Dequeue(T& datum) {
			std::unique_lock l(sync);
			if (queue.empty()) {
				l.unlock();
				owner->collective->runner
			}
			datum = queue.front();
			queue.pop();
		}
	private:
		std::vector<IOutput*> GetConnectedOutputs() {
			std::vector<IOutput*> results;
			for (auto &connectedOutput : connectedOutputs) {
				results.push_back(connectedOutput);
			}
			return results;
		}

		Box* GetOwner() {
			return owner;
		}

		friend class Collective;
		template<typename T>
		friend class Output;
		friend class Box;
		std::queue<T> queue;
		std::set<Output<T>*> connectedOutputs;
		Box* owner;
		std::mutex sync;
		void DidConnect(Output<T>* output) {
			connectedOutputs.insert(output);
		}

		void Enqueue(T const &datum) {
			std::unique_lock l(sync);
			queue.push(datum);
			owner->hasPendingWork = true;
		}

		bool GetIsBlocked() {
			return cv.GetAnyWaiting();
		}

		void Lock() {
			sync.lock();
		}

		void Unlock() {
			sync.unlock();
		}
	};
}
#endif