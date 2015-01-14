#include "ConditionVariable.h"

namespace Synchronox {
	bool ConditionVariable::GetAnyWaiting() {
		return !_waitingThreads.empty();
	}

	void ConditionVariable::Wait(std::unique_lock<std::mutex> lock) {
		NoResetEvent waitHandle;
		_waitingThreads.push(&waitHandle);
		lock.unlock();
		waitHandle.Wait();
		lock.lock();
	}

	bool ConditionVariable::Signal() {
		NoResetEvent* waitHandle;
		if (_waitingThreads.pop(waitHandle)) {
			waitHandle->Set();
			return true;
		}
		return false;
	}
}