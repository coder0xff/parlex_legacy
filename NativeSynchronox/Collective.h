#ifndef _COLLECTIVE_H_
#define _COLLECTIVE_H_

#include <memory>
#include <forward_list>
#include <set>
#include <map>
#include <thread>
#include <mutex>
#include <boost/coroutine/coroutine.hpp>
#include <boost/thread/tss.hpp>
#include <boost/lockfree/queue.hpp>

#include "NoResetEvent.h"
#include "Box.h"
#include "Input.h"
#include "Output.h"
#include "lock_free_forward_list.h"

namespace Synchronox {
	typedef boost::coroutines::symmetric_coroutine<void> coroutine;

	class Collective {
		Collective(Collective const &other) = delete;
	public:
		template<typename T>
		void Connect(Input<T>& input, Output<T>& output) {
			output.Connect(input);
		}

		bool IsDone();
		void Join();
		template<typename T, typename... U>
		std::shared_ptr<T> CreateBox(U... args) {
			std::lock<std::mutex> lock(boxesMutex);
			auto box = new T(args...);
			box->collective = this;
			box->Initializer();
			box->coro = std::move(coroutine::call_type([this, box](coroutine::yield_type& yield) {
				box->yield = &yield;
				box->Computer();
			}));
			box->hasPendingWork = true;
			boxes.emplace_front(box);
		}

	protected:
		Collective(int threadCount = -1);
		void ConstructionCompleted();
		virtual void Terminator();
	private:
		friend class Box;

		NoResetEvent startBlocker;
		std::atomic<int> haltedBoxCount;
		NoResetEvent blocker;
		lock_free_forward_list<std::unique_ptr<Box>> boxes;

		boost::thread_specific_ptr<coroutine::call_type> runner;
		std::vector<coroutine::call_type> runners;
		std::vector<std::thread> runnerThreads;

		void PropagateHalt(Box* box);
		void BoxHalted();
		bool DeadlockBreaker();
		Box* DetectDeadlock(bool lockCollective);
		void RunnerLoop(coroutine::yield_type& yield);
	};
}

#endif