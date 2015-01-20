#include "Collective.h"
#include <cassert>
#include <set>
#include <utility>

namespace Synchronox {
	Collective::Collective(int threadCount) {
		if (threadCount == -1) {
			threadCount = std::thread::hardware_concurrency();
		}
		assert(threadCount > 0);

		for (int spawn = 0; spawn < threadCount; spawn++) {
			runners.emplace_back([this](coroutine::yield_type& yield) { RunnerLoop(yield); });
			coroutine::call_type* coro = &runners.back();
			runnerThreads.emplace_back([this, coro] {
				(*coro)();
			});
		}
	}

	bool Collective::IsDone() {
		return blocker.IsSet();
	}

	void Collective::Join() {
		blocker.Wait();
		std::unique_lock<std::mutex> lock(boxesMutex);
		int index = 0;
		while (index < boxes.size()) {
			lock.unlock();
			boxes[index]->Join();
			lock.lock();
		}
	}

	void Collective::ConstructionCompleted() {
		startBlocker.Set();
	}

	void Collective::Terminator() {}

	void Collective::PropagateHalt(Box* box) {
		std::set<Box*> dependentBoxes;
		for (auto output : box->GetOutputs()) {
			for (auto input : output->GetConnectedInputs()) {
				dependentBoxes.insert(input->GetOwner());
			}
		}
		for (auto dependentBox : dependentBoxes) {
			if (!dependentBox->GetIsHalted()) {
				for (auto input : dependentBox->GetInputs()) {
					input->CheckWillHalt();
				}
			}
		}
	}

	void Collective::BoxHalted() {
		haltedBoxCount++;
	}

	/// <summary>
	/// Construct a doubly linked graph from the current input/output dependencies
	/// The graph excludes any vertices that would represent halted a Box
	/// A vertex is "blocked" if it has any "inNeighbors"
	/// Non-blocked vertices, and their successors, are removed
	/// The remaining vertices' corresponding boxes are deadlocked
	/// 
	/// This algorithm is O(n + k)
	/// 	
	/// </summary>
	/// <param name="lockCollective">the lockCollective parameter determines
	/// if all boxes will be locked before testing. Without locking, false
	/// positives/negatives are possible. With locking, the test is guaranteed
	/// to be correct.</param>
	/// <returns>Box*</returns>
	Box* Collective::DetectDeadlock(bool lockCollective) {
		std::unique_lock<std::mutex> lock;
		std::vector<std::unique_lock<std::mutex>> boxLocks;
		std::vector<Box*> boxesCopy;

		//fill boxesCopy, locking each Box if lockCollective is true
		if (lockCollective) {
			lock = std::move(std::unique_lock<std::mutex>(boxesMutex));			
			for (std::unique_ptr<Box> &boxPtr : boxes) {
				if (boxPtr->GetIsHalted()) continue;
				boxLocks.push_back(std::move(boxPtr->Lock()));
				boxesCopy.push_back(boxPtr.get());
			}
		}
		else {
			std::unique_lock<std::mutex> shortLock(boxesMutex);
			for (std::unique_ptr<Box> &boxPtr : boxes) {
				if (boxPtr->GetIsHalted()) continue;
				boxesCopy.push_back(boxPtr.get());
			}
		}

		class vertex {
		public:
			bool isHalted;
			std::set<vertex*> inNeighbors;
			std::set<vertex*> outNeighbors;
			Box* box;
		};

		//construct the vertices of the graph
		typedef std::map<Box*, std::unique_ptr<vertex>> map_t;
		map_t boxToVertex;
		auto getVertex = [&boxToVertex](Box* box) { 
			auto pair = boxToVertex.insert(map_t::value_type(box, std::move(std::unique_ptr<vertex>())));
			if (pair.second) {
				pair.first->second = std::move(std::unique_ptr<vertex>());
			}
			vertex *result = pair.first->second.get();
			result->box = box;
			return result;
		};

		//construct the edges
		for (std::unique_ptr<Box> &boxPtr : boxes) {
			vertex *inVertex = getVertex(boxPtr.get());
			for (IInput* input : boxPtr->GetInputs()) {
				if (input->GetIsBlocked()) {
					for (IOutput *output : input->GetConnectedOutputs()) {
						vertex *outVertex = getVertex(output->GetOwner());
						inVertex->outNeighbors.insert(outVertex);
						outVertex->inNeighbors.insert(inVertex);
					}
				}
			}
		}

		//Find and separate any unblocked boxes (ones that have no in neighbors)
		std::set<vertex*> transitivelyBlockedSet;
		std::queue<vertex*> transitivelyUnblocked;
		for (auto &pair : boxToVertex) {
			vertex *v = pair.second.get();
			if (v->inNeighbors.size() > 0) {
				transitivelyBlockedSet.insert(v);
			}
			else {
				transitivelyUnblocked.push(v);
			}
		}

		//and apply the unblocked "property" transitively down stream
		//this is similar to Kahn's "Topological sorting of large networks"
		//except that nodes are removed regardless of whether they have any
		//remaining in edges
		while (transitivelyUnblocked.size() > 0) {
			vertex *unblocked = transitivelyUnblocked.front();
			transitivelyUnblocked.pop();
			for (vertex *downstream : unblocked->outNeighbors) {
				transitivelyBlockedSet.erase(downstream);
				transitivelyUnblocked.push(downstream);
			}
		}

		//the remaining vertices, if any, are deadlocked
		//return the associated box of one of them
		if (transitivelyBlockedSet.size() > 0) {
			return (*transitivelyBlockedSet.begin())->box;
		}
		return nullptr;

		//all locks are freed thanks to vector of unique_ptrs
		//all vertices are freed thanks to map of unique_ptrs
	}

	void Collective::RunnerLoop(coroutine::yield_type& yield) {
		startBlocker.Wait();
		while (!blocker.IsSet()) {
			for (auto &boxPtr : boxes) {
				if (boxPtr->hasPendingWork.exchange(false)) {
					yield(boxPtr->coro);
				}
			}
		}
	}
}