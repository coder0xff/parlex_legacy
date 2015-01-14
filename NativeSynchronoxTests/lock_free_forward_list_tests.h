#include "lock_free_forward_list.h"
#include <vector>
#include <thread>
#include <iostream>
#include <set>
#include <mutex>

class lock_free_forward_list_tests {
public:
	static void test_01() {
		{
			lock_free_forward_list<int> a;
		}
		_CrtDumpMemoryLeaks();
	}

	static void test_02() {
		{
			lock_free_forward_list<int> a;
			a.push_front(2);
			int v = 0;
			assert(a.pop_front(v));
			assert(v == 2);
			assert(a.empty());
		}
		_CrtDumpMemoryLeaks();
	}

	static void test_03() {
		{
			lock_free_forward_list<int> a;
			a.push_front(2);
			a.push_front(5);
			int v = 0;
			assert(a.pop_front(v));
			assert(v == 5);
			assert(a.pop_front(v));
			assert(v == 2);
			assert(a.empty());
		}
		_CrtDumpMemoryLeaks();
	}

	static void test_04() {
		{
			lock_free_forward_list<int> a;
			std::vector<std::thread> threads;
			int threadCount = 5;
			int perThreadElementCount = 1000;
			for (int i = 0; i < threadCount; i++) {
				threads.emplace_back([&]() {
					for (int j = 0; j < perThreadElementCount; j++) {
						a.push_front(j);
					}
				});
			}
			for (int i = 0; i < threadCount; i++) {
				threads[i].join();
			}
			int totalElementCount = perThreadElementCount * threadCount;
			for (int k = 0; k < totalElementCount; k++) {
				int v = 0;
				assert(a.pop_front(v));
				std::cout << v << " ";
			}
			assert(a.empty());
		}
		_CrtDumpMemoryLeaks();
	}

	static void test_05() {
		{
			lock_free_forward_list<int> a;
			std::vector<std::thread> threads;
			for (int i = 0; i < 5; i++) {
				threads.emplace_back([&a]() {
					for (int j = 0; j < 1000; j++) {
						int y = rand();
						a.push_front(y, std::memory_order_relaxed, std::memory_order_relaxed);
						std::this_thread::sleep_for(std::chrono::microseconds(rand() % 10));
						int x;
						a.pop_front(x, std::memory_order_relaxed, std::memory_order_relaxed);
						if (x == y) {
							std::cout << "y";
						}
						else {
							std::cout << "n";
						}
					}
				});
			}
			for (int i = 0; i < 5; i++) {
				threads[i].join();
			}
			assert(a.empty());
		}
		_CrtDumpMemoryLeaks();
	}

	static void test_06() {
		{
			lock_free_forward_list<int> a;
			std::vector<std::thread> threads;
			int threadCount = 5;
			int perThreadElementCount = 1000;
			for (int i = 0; i < threadCount; i++) {
				threads.emplace_back([&a, i, perThreadElementCount]() {
					for (int j = 0; j < perThreadElementCount; j++) {
						a.push_front(j + i * perThreadElementCount);
					}
				});
			}
			for (int i = 0; i < threadCount; i++) {
				threads[i].join();
			}
			std::set<int> remainingNumbers;
			int totalElementCount = perThreadElementCount * threadCount;
			for (int k = 0; k < totalElementCount; k++) {
				remainingNumbers.insert(k);
			}
			for (int k = 0; k < totalElementCount; k++) {
				int v;
				assert(a.pop_front(v));
				std::cout << v << " ";
				assert(remainingNumbers.erase(v));
			}
			assert(remainingNumbers.empty());
			assert(a.empty());
		}
		_CrtDumpMemoryLeaks();
	}

	static void test_07() {
		{
			lock_free_forward_list<int> a;
			std::vector<std::thread> threads;
			int threadCount = 5;
			int perThreadElementCount = 1000;
			int totalElementCount = perThreadElementCount * threadCount;
			std::mutex mutex;
			std::cout << "Initializing lock_free_forward_list_tests::test_07\n";
			std::set<int> remainingNumbers;
			for (int k = 0; k < totalElementCount; k++) {
				remainingNumbers.insert(k);
			}
			for (int i = 0; i < threadCount; i++) {
				threads.emplace_back([&, i]() {
					for (int j = 0; j < perThreadElementCount; j++) {
						int y = j + i * perThreadElementCount;
						a.push_front(y, std::memory_order_relaxed, std::memory_order_relaxed);
						std::this_thread::sleep_for(std::chrono::microseconds(rand() % 50));
						int x;
						a.pop_front(x, std::memory_order_relaxed, std::memory_order_relaxed);
						{
							std::unique_lock<std::mutex> lock(mutex);
							assert(remainingNumbers.erase(x));
						}
						if (x == y) {
							std::cout << "y";
						}
						else {
							std::cout << "n";
						}
					}
				});
			}
			for (int i = 0; i < threadCount; i++) {
				threads[i].join();
			}
			assert(a.empty());
			assert(remainingNumbers.empty());
		}
		_CrtDumpMemoryLeaks();
	}

	static void test_08() {
		{
			lock_free_forward_list<int> a;
			std::vector<std::thread> threads;
			int threadCount = 5;
			int perThreadElementCount = 1000;
			int totalElementCount = perThreadElementCount * threadCount;
			std::mutex mutex;
			std::set<int> remainingNumbers;
			std::cout << "Initializing lock_free_forward_list_tests::test_08\n";
			for (int k = 0; k < totalElementCount; k++) {
				remainingNumbers.insert(k);
			}
			for (int i = 0; i < threadCount; i++) {
				threads.emplace_back([&, i]() {
					for (int j = 0; j < perThreadElementCount; j++) {
						int y = j + i * perThreadElementCount;
						a.push_front(y, std::memory_order_relaxed, std::memory_order_relaxed);
					}
				});
			}
			for (int i = 0; i < threadCount; i++) {
				threads.emplace_back([&, i]() {
					for (int j = 0; j < perThreadElementCount; j++) {
						int x;
						a.pop_front(x, std::memory_order_relaxed, std::memory_order_relaxed);
						{
							std::unique_lock<std::mutex> lock(mutex);
							assert(remainingNumbers.erase(x));
						}
						std::cout << x << " ";
					}
				});
			}
			for (int i = 0; i < threadCount * 2; i++) {
				threads[i].join();
			}
			assert(a.empty());
			assert(remainingNumbers.empty());
		}
		_CrtDumpMemoryLeaks();
	}

	static void test_09() {

	}

	static void test_all() {
//		test_01();
// 		test_02();
// 		test_03();
// 		test_04();
// 		test_05();
//		test_06();
// 		test_07();
 		test_08();
	}
};
