#ifndef _NODE_H_
#define _NODE_H_

#include "NoResetEvent.h"
#include <vector>
#include "IInput.h"
#include "IOutput.h"
#include <boost/coroutine/coroutine.hpp>
#include <mutex>

namespace Synchronox {
	class Collective;

	class Box
	{
	protected:
		Box();
		virtual void Initializer();
		virtual void Computer() = 0;
		virtual void Terminator();
	private:
		friend class Collective;
		bool isHalted;
		NoResetEvent constructionBlocker;
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
		void SetConstructionCompleted() {
			constructionBlocker.Set();
		}
		std::atomic<bool> needsService;
	};
}
#endif