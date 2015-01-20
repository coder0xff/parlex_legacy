#ifndef _NODE_H_
#define _NODE_H_

#include "NoResetEvent.h"
#include <vector>
#include "IInput.h"
#include "IOutput.h"
#include <boost/coroutine/coroutine.hpp>
#include <mutex>
#include <atomic>

namespace Synchronox {
	typedef boost::coroutines::symmetric_coroutine<void> coroutine;

	class Collective;

	class Box
	{
	protected:
		Box();
		virtual void Initializer();
		virtual void Computer() = 0;
		virtual void Terminator();
	private:
		coroutine::yield_type *yield;
		friend class Collective;
		std::atomic<bool> hasPendingWork;
		bool isHalted;
		coroutine::call_type coro;
		NoResetEvent completion;
		std::vector<IInput*> inputs;
		std::vector<IOutput*> outputs;
		Collective* collective;

		std::vector<IInput*> GetInputs();
		std::vector<IOutput*> GetOutputs();
		bool GetIsHalted();

		std::unique_lock<std::mutex> Lock();
		void VerifyConstructionCompleted();
		void Join();
		void _internal_use_only_register_input(IInput *input);
		void _internal_use_only_register_output(IOutput *output);
		std::atomic<bool> needsService;
	};
}
#endif