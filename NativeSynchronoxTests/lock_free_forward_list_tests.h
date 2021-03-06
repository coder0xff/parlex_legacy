#ifdef _MSC_VER //for doing leak detection
#	define DUMP _CrtDumpMemoryLeaks()
#else
#	define DUMP
#endif 

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
		DUMP;
	}

	static void test_02() {
		{
			lock_free_forward_list<int> a;
			a.push_front(2);
			int v = 0;
			bool success = a.pop_front(v);
			assert(success);
			assert(v == 2);
			assert(a.empty());
		}
		DUMP;
	}

	static void test_03() {
		{
			lock_free_forward_list<int> a;
			a.push_front(2);
			a.push_front(5);
			int v = 0;
			bool success = a.pop_front(v);
			assert(success);
			assert(v == 5);
			success = a.pop_front(v);
			assert(v);
			assert(v == 2);
			assert(a.empty());
		}
		DUMP;
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
			for (auto &thread : threads) {
				thread.join();
			}
			int totalElementCount = perThreadElementCount * threadCount;
			for (int k = 0; k < totalElementCount; k++) {
				int v = 0;
				bool success = a.pop_front(v);
				assert(success);
				std::cout << v << " ";
			}
			assert(a.empty());
		}
		DUMP;
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
			for (auto &thread : threads) {
				thread.join();
			}
			assert(a.empty());
		}
		DUMP;
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
			for (auto &thread : threads) {
				thread.join();
			}
			std::set<int> remainingNumbers;
			int totalElementCount = perThreadElementCount * threadCount;
			for (int k = 0; k < totalElementCount; k++) {
				remainingNumbers.insert(k);
			}
			for (int k = 0; k < totalElementCount; k++) {
				int v = 0;
				bool success = a.pop_front(v);
				assert(success);
				std::cout << v << " ";
				success = remainingNumbers.erase(v);
				assert(success);
			}
			assert(remainingNumbers.empty());
			assert(a.empty());
		}
		DUMP;
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
							bool success = remainingNumbers.erase(x);
							assert(success);
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
			for (auto &thread : threads) {
				thread.join();
			}
			assert(a.empty());
			assert(remainingNumbers.empty());
		}
		DUMP;
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
							bool success = remainingNumbers.erase(x);
							assert(success);
						}
						std::cout << x << " ";
					}
				});
			}
			for (auto &thread : threads) {
				thread.join();
			}
			assert(a.empty());
			assert(remainingNumbers.empty());
		}
		DUMP;
	}

	static void test_09() {
		{
			lock_free_forward_list<int> a;
			a.push_front(2);
			a.push_front(5);
			auto i = a.begin();
			assert(*i == 5);
			i++;
			assert(*i == 2);
			i++;
			assert(i == a.end());

		}
		DUMP;
	}

	static void test_10() {
		{
			lock_free_forward_list<int> a;
			a.push_front(2);
			auto i = a.begin();
			assert(*i == 2);
			a.push_front(5);
			i++;
			assert(i == a.end());
		}
		DUMP;
	}

	static void test_11() {
		{
			lock_free_forward_list<int> a;
			a.push_front(2);
			auto i = a.begin();
			int v;
			a.pop_front(v);
			a.push_front(5);
			auto j = a.begin();
			assert(*i == 2);
			assert(*j == 5);
			i++;
			assert(i == a.end());
			j++;
			assert(j == a.end());
		}
		DUMP;
	}

	static void test_12() {
		{
			lock_free_forward_list<int> a;
			a.push_front(2);
			a.push_front(5);
			a.insert_after(a.begin(), 3);
			auto i = a.begin();
			assert(*i == 5);
			i++;
			assert(*i == 3);
			i++;
			assert(*i == 2);
			i++;
			assert(i == a.end());
		}
		DUMP;
	}

	static void test_13() {
		{
			lock_free_forward_list<int> a;
			a.push_front(2);
			a.push_front(3);
			a.push_front(5);
			auto i = a.begin();
			assert(*i == 5);
			i++;
			int v;
			a.erase_after(a.begin(), v);
			assert(v == 3);
			assert(*i == 3);
			i++;
			assert(i == a.end());
			assert(*(++a.begin()) == 2);
		}
		DUMP;
	}

	static void test_14() {
		{
			std::cout << "\ntest_14\n";
			lock_free_forward_list<int> a;
			for (int i = 0; i < 100000; i++) {
				a.push_front(i);
			}
		}
		DUMP;
	}

	static void test_15() {
		{
			lock_free_forward_list<int> a;
			std::vector<std::thread> threads1;
			std::vector<std::thread> threads2;
			int const threadCount = 5;
			int const perThreadOpCount = 100000;
			bool done = false;
			for (int i = 0; i < threadCount; i++) {
				threads1.emplace_back([&, i]() {
					for (int j = 0; j < perThreadOpCount; j++) {
						int op = rand() % (perThreadOpCount / 100);
						if (op == 0) {
							std::cout << "\n" << a.clear() << "\n";
						}
						else {
							a.push_front(rand() % 20, std::memory_order_relaxed, std::memory_order_relaxed);
						}
					}
				});
			}
			for (int i = 0; i < threadCount; i++) {
				threads2.emplace_back([&, i]() {
					auto iterator = a.begin();
					while (!done) {
						if (iterator != a.end()) {
							std::cout << *iterator << " ";
						}
						if (iterator == a.end()) {
							iterator = a.begin();
						}
						else {
							++iterator;
						}
					}
				});
			}
			for (auto &thread : threads1) {
				thread.join();
			}
			done = true;
			for (auto &thread : threads2) {
				thread.join();
			}
		}
		DUMP;
	}

	//static void test_() {
	//	{
	//		lock_free_forward_list<int> a;
	//	}
	//	DUMP;
	//}


	static void test_all() {
		for (int repeat = 0; repeat < 10; repeat++) {
			test_01();
			test_02();
			test_03();
			test_04();
			test_05();
			test_06();
			test_07();
			test_08();
			test_09();
			test_10();
			test_11();
			test_12();
			test_13();
			test_14();
			test_15();
		}
	}
};
