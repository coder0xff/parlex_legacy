#ifndef CONDITION_VARIABLE_H
#define CONDITION_VARIABLE_H

#include "boost/lockfree/queue.hpp"
#include <mutex>
#include "NoResetEvent.h"

namespace Synchronox {
	class ConditionVariable
	{
		ConditionVariable(ConditionVariable const &other) = delete;
	public:
		bool GetAnyWaiting();
		void Wait(std::unique_lock<std::mutex> lock);
		bool Signal();
	private:
		boost::lockfree::queue<NoResetEvent*> _waitingThreads;
	};
}

#endif