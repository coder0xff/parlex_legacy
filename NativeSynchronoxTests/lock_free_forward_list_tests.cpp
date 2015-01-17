	// Is noexcept supported?
	#if defined(__clang__) && __has_feature(cxx_noexcept) || \
		defined(__GXX_EXPERIMENTAL_CXX0X__) && __GNUC__ * 10 + __GNUC_MINOR__ >= 46 || \
		defined(_MSC_FULL_VER) && _MSC_FULL_VER >= 180021114
	#  define NOEXCEPT noexcept
	#else
	#  define NOEXCEPT
	#endif

	#include <memory>
	#include <atomic>

	inline void* lock_free_forward_list_get_deadDummy() {
		static std::unique_ptr<void*> deadDummy_(new void* ());
		return deadDummy_.get();
	}

	inline void* lock_free_forward_list_get_spinDummy() {
		static std::unique_ptr<void*> spinDummy_(new void* ());
		return spinDummy_.get();
	}

	#define deadDummy ((node*)lock_free_forward_list_get_deadDummy())
	#define spinDummy ((node*)lock_free_forward_list_get_spinDummy())

	//similar to std::forward_list, but thread safe and lock free
	//some methods have been removed/added to facilitate these guarantees.
	template<class T>
	class lock_free_forward_list
	{
	private:
		static std::memory_order combine_memory_order(std::memory_order loadOrder, std::memory_order storeOrder) {
			if (loadOrder == std::memory_order_seq_cst || storeOrder == std::memory_order_seq_cst){
				return std::memory_order_seq_cst;
			}
			if (loadOrder == std::memory_order_acquire || loadOrder == std::memory_order_consume || loadOrder == std::memory_order_acq_rel) {
				if (storeOrder == std::memory_order_release || storeOrder == std::memory_order_acq_rel) {
					return std::memory_order_acq_rel;
				}
				if (storeOrder == std::memory_order_relaxed) {
					return loadOrder;
				}
			}

			return storeOrder;
		}

		template<class T>
		class ForwardIterator;

		class node;

		class node {
			friend class lock_free_forward_list<T>;
			friend class ForwardIterator<T>;
			T value;
			std::atomic<node*> next;
			std::atomic<int> referenceCount;

			node(T const &value) : value(value), next(nullptr), referenceCount(1) {}
			node(T &&value) : value(std::move(value)), next(nullptr), referenceCount(1) {}
			template<class... U>
			node(U... params) : value(std::forward(params)...), next(nullptr), referenceCount(1) {}
			~node() {
				node* n = lockLoadTransferOwnership(next, std::memory_order_seq_cst, std::memory_order_seq_cst);
				loseOwnership(n, std::memory_order_seq_cst, std::memory_order_seq_cst);
				next.store(deadDummy);
			}
		};

		//lock free
		static void loseOwnership(node *&n, std::memory_order loadOrder, std::memory_order storeOrder) {
			assert(n != deadDummy);
			assert(n != spinDummy);
			if (n && n->referenceCount.fetch_sub(1, combine_memory_order(loadOrder, storeOrder)) == 1) {
				delete n;
			}
			n = nullptr;
		}

		//lock free
		static node *gainOwnership(node* n, std::memory_order loadOrder, std::memory_order storeOrder) {
			assert(n != deadDummy);
			assert(n != spinDummy);
			assert(n != nullptr);
			n->referenceCount.fetch_add(1, combine_memory_order(loadOrder, storeOrder));
			return n;
		}

		//lock free
		static void exchange(std::atomic<node*> &left, node* &right, std::memory_order loadOrder, std::memory_order storeOrder) {
			assert(right != spinDummy);
			node *n = left.load(loadOrder);
			do {
				while (n == spinDummy) {
					n = left.load(loadOrder);
				}
			} while (!left.compare_exchange_weak(n, right, storeOrder, loadOrder));
			assert(n != deadDummy);
			right = n;
		}

		//NOT lock free on left, lock free on right
		static void exchange(std::atomic<node*> &left, std::atomic<node*> &right, std::memory_order loadOrder, std::memory_order storeOrder) {
			node* temp = lockLoadTransferOwnership(left, loadOrder, storeOrder);
			exchange(right, temp, loadOrder, storeOrder);
			storeTransferOwnershipUnlock(left, temp, loadOrder, storeOrder);
		}

		//NOT lock free
		static node *lockLoadTransferOwnership(std::atomic<node*> &atomic_ptr, std::memory_order loadOrder, std::memory_order storeOrder) {
			node *n;
			n = atomic_ptr.load(loadOrder);
			do {
				while (n == spinDummy) {
					n = atomic_ptr.load(loadOrder);
				}
			} while (!atomic_ptr.compare_exchange_weak(n, spinDummy, std::memory_order_relaxed));
			if (n == deadDummy) {
				atomic_ptr.store(deadDummy, std::memory_order_relaxed);
				return nullptr;
			}
			if (n == nullptr) return nullptr;
			return n;
		}

		//lock free - but requires a preceding call to lockLoadTransferOwnership
		static void storeTransferOwnershipUnlock(std::atomic<node*> &atomic_ptr, node* &n, std::memory_order loadOrder, std::memory_order storeOrder) {
			assert(atomic_ptr.load(std::memory_order_relaxed) == spinDummy);
			atomic_ptr.store(n, storeOrder);
			n = nullptr;
		}

		//NOT lock free
		static node* lockLoadGainOwnershipUnlock(std::atomic<node*> &atomic_ptr, std::memory_order loadOrder, std::memory_order storeOrder) {
			node *temp = lockLoadTransferOwnership(atomic_ptr, loadOrder, storeOrder);
			if (temp == nullptr && atomic_ptr.load(loadOrder) == deadDummy) return nullptr;
			node* result = temp ? gainOwnership(temp, loadOrder, storeOrder) : nullptr;
			storeTransferOwnershipUnlock(atomic_ptr, temp, loadOrder, storeOrder);
			return result;
		}

		//lock free
		static void loseOwnershipStoreTransferOwnership(std::atomic<node*> &atomic_ptr, node* &n, std::memory_order loadOrder, std::memory_order storeOrder) {
			assert(n != deadDummy);
			assert(n != spinDummy);
			exchange(atomic_ptr, n, loadOrder, storeOrder);
			if (n != nullptr) {
				loseOwnership(n, storeOrder, loadOrder);
			}
		}

		static void loseOwnershipStoreTransferOwnership(node * &x, node* &n, std::memory_order loadOrder, std::memory_order storeOrder) {
			assert(n != deadDummy);
			assert(n != spinDummy);
			std::swap(x, n);
			if (n != nullptr) {
				loseOwnership(n, storeOrder, loadOrder);
			}
		}

		//lock free
		static void loseOwnershipStoreGainOwnership(node *&x, node *n, std::memory_order loadOrder, std::memory_order storeOrder) {
			if (x != nullptr) {
				loseOwnership(x, loadOrder, storeOrder);
			}
			x = n ? gainOwnership(n, loadOrder, storeOrder) : nullptr;
		}

		template<class T>
		//construction is lock free (though begin() is not)
		//incrementing is NOT lock free
		class ForwardIterator {
			friend class lock_free_forward_list;
			node *current;
		public:
			typedef std::forward_iterator_tag iterator_category;
			typedef T value_type;
			typedef T & reference;
			typedef T * pointer;
			ForwardIterator() : current(nullptr) {}
			ForwardIterator(node* n) : current(n ? gainOwnership(n, std::memory_order_seq_cst, std::memory_order_seq_cst) : nullptr) {}
			ForwardIterator(ForwardIterator const &other) : current(other.current ? gainOwnership(other.current, std::memory_order_seq_cst, std::memory_order_seq_cst) : nullptr) {}
			ForwardIterator(ForwardIterator &&other) : current(nullptr) { std::swap(current, other.current); }
			~ForwardIterator() {
				loseOwnership(current, std::memory_order_seq_cst, std::memory_order_seq_cst);
			}
			ForwardIterator& operator=(ForwardIterator const &other) {
				loseOwnershipStoreGainOwnership(current, other.current, std::memory_order_seq_cst, std::memory_order_seq_cst);
				return *this;
			}

			T &operator*() { return current->value; }
			T &operator->() { return current->value; }
			ForwardIterator operator++() {
				assert(current != nullptr);
				node *temp = lockLoadGainOwnershipUnlock(current->next, std::memory_order_seq_cst, std::memory_order_seq_cst);
				loseOwnershipStoreTransferOwnership(current, temp, std::memory_order_seq_cst, std::memory_order_seq_cst);
				return *this;
			}

			ForwardIterator operator++(int) {
				assert(current != nullptr);
				ForwardIterator temp = *this;
				++*this;
				return temp;
			}

			friend void swap(ForwardIterator& a, ForwardIterator& b) NOEXCEPT
			{
				using std::swap; // bring in swap for built-in types
				std::swap(a.current, b.current);
			}

			operator ForwardIterator<const T>() const
			{
				return ForwardIterator<const T>(current);
			}

			bool operator==(ForwardIterator const &rhs) {
				return current == rhs.current;
			}

			bool operator!=(ForwardIterator const &rhs) {
				return !(*this == rhs);
			}
		};

	public:
		typedef T value_type;
		typedef value_type & reference;
		typedef const value_type & const_reference;
		typedef value_type * pointer;
		typedef value_type const * const_pointer;
		typedef ForwardIterator<T> iterator;
		typedef ForwardIterator<const T> const_iterator;

		lock_free_forward_list() : first(nullptr) {
		}

		~lock_free_forward_list() {
			clear();
		}

		//lock free
		bool empty(std::memory_order loadOrder = std::memory_order_seq_cst) {
			return first.load(loadOrder) == nullptr;
		}

		//lock free
		//iterators will still contain correct values,
		//but incrementing them or inserting after them will result in a default constructed iterator
		int clear(std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
			node *oldFirst = nullptr;
			exchange(first, oldFirst, loadOrder, storeOrder);
			//if we just delete the first node, it may cascade down all the
			//subsequent nodes. This would be fine, if not for the possibility
			//of blowing the stack. Instead we delete them in reverse.
			std::vector<node*> nodes;
			while (oldFirst) {
				nodes.push_back(oldFirst);
				node *temp = deadDummy;
				exchange(oldFirst->next, temp, loadOrder, storeOrder);
				oldFirst = temp;
			}
			for (auto i = nodes.rbegin(); i != nodes.rend(); ++i) {
				loseOwnership(*i, loadOrder, storeOrder);
			}
			return nodes.size();
		}

		//NOT lock free - iterators and inserts will block, and then end or fail respectively
		//elements inserted during this algorithm will be removed as well
		//use locked_clear to have inserts fail instead
		int locked_clear(std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
			node *oldFirst = nullptr;
			exchange(first, oldFirst, loadOrder, storeOrder);
			//if we just delete the first node, it may cascade down all the
			//subsequent nodes. This would be fine, if not for the possibility
			//of blowing the stack. Instead we delete them in reverse.
			std::vector<node*> nodes;
			while (oldFirst) {
				nodes.push_back(oldFirst);
				node *temp = spinDummy;
				exchange(oldFirst->next, temp, loadOrder, storeOrder);
				oldFirst = temp;
			}
			for (auto i = nodes.rbegin(); i != nodes.rend(); ++i) {
				loseOwnership(*i, loadOrder, storeOrder);
			}
			return nodes.size();
		}

		//NOT lock free
		T& front(std::memory_order loadOrder = std::memory_order_seq_cst) {
			return *begin(loadOrder);
		}

		//lock free
		void push_front(const T& value, std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
			insert_node(first, new node(value), loadOrder, storeOrder);
		}

		//lock free
		void push_front(T&& value, std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
			auto result = insert_node(first, new node(std::move(value)), loadOrder, storeOrder);
			assert(result.current != nullptr);
		}

		//lock free
		template<class... U>
		void emplace_front(U... params) {
			insert_node(first, new node(params...), std::memory_order_seq_cst, std::memory_order_seq_cst);
		}

		//lock free
		template<class... U>
		void emplace_front_ordered(std::memory_order loadOrder, std::memory_order storeOrder, U... params) {
			insert_node(first, new node(params...), loadOrder, storeOrder);
		}

		//NOT lock free
		bool pop_front(T &value, std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
			return remove_node(first, value, loadOrder, storeOrder);
		}

		//NOT lock free
		iterator begin(std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
			node *n = lockLoadGainOwnershipUnlock(first, loadOrder, storeOrder);
			iterator result(n);
			loseOwnership(n, loadOrder, storeOrder);
			return result;
		}

		//lock free
		iterator end() {
			return iterator();
		}

		//NOT lock free
		const_iterator cbegin(std::memory_order loadOrder = std::memory_order_seq_cst) {
			return begin();
		}

		//lock free
		const_iterator cend() {
			return const_iterator();
		}

		//lock free - except construction of iterator
		//returns a default constructed iterator if position is no longer valid
		iterator insert_after(const_iterator position, T const &value, std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
			return insert_node(position.current->next, new node(value), loadOrder, storeOrder);
		}

		//lock free - except construction of iterator
		iterator insert_after(const_iterator position, T&& value, std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
			return insert_node(position.current->next, new node(value), loadOrder, storeOrder);
		}

		//lock free
		iterator insert_after(const_iterator pos, int count, const T& value, std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
			if (count <= 0) return iterator();
			iterator result = pos = insert_after(pos, value, loadOrder, storeOrder);
			for (int i = 1; i < count; i++) {
				pos = insert_after(pos, value, loadOrder, storeOrder);
			}
			return result;
		}

		//lock free
		template< class InputIt >
		iterator insert_after(const_iterator pos, InputIt first, InputIt last, std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
			if (first == last) return iterator();
			iterator result = pos = insert_after(pos, *first, loadOrder, storeOrder);
			++first;
			while (first != last) {
				pos = insert_after(pos, first, loadOrder, storeOrder);
				++first;
			}
			return result;
		}

		//lock free
		iterator insert_after(const_iterator pos, std::initializer_list<T> ilist, std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
			return insert_after(pos, ilist.begin(), ilist.end(), loadOrder, storeOrder);
		}

		//lock free
		template<class... U>
		iterator emplace_after(const_iterator position, U&&... params) {
			return insert_node(position, new node(std::forward(params)...));
		}

		//lock free
		template<class... U>
		iterator emplace_after_ordered(const_iterator position, std::memory_order loadOrder, std::memory_order storeOrder, U&&... params) {
			return insert_node(position, new node(std::forward(params)...), loadOrder, storeOrder);
		}

		//lock free
		//all the elements after position are moved to a new lock_free_forward_list
		bool separate_after(const_iterator position, lock_free_forward_list<T> *&result, std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
			node *n = seperate(position.current->next, loadOrder, storeOrder);
			if (!n) return false;
			result = new lock_free_forward_list<T>();
			result->first = n;
			return true;
		}

		void concat(lock_free_forward_list &other, std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
			node *n = seperate(other.first, loadOrder, storeOrder);
			concat(other.first)
		}		

		//NOT lock free
		bool erase_after(const_iterator position, T &value, std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
			return remove_node(position.current->next, value, loadOrder, storeOrder);
		}

		//NOT lock free on a, lock free on b
		friend void swap(lock_free_forward_list& a, lock_free_forward_list& b)
		{
			exchange(a.first, b.first);
		}

	private:
		std::atomic<node*> first;

		static iterator insert_node(std::atomic<node*> &atomic_ptr, node* n, std::memory_order loadOrder, std::memory_order storeOrder) {
			iterator result(n); //it's possible that the node is removed before we return, so do this early
			n->next.store(n, storeOrder);
			exchange(n->next, atomic_ptr, loadOrder, storeOrder);
			return result;
		}

		static node* seperate(std::atomic<node*> &atomic_ptr, std::memory_order loadOrder, std::memory_order storeOrder) {
			node* oldNext = nullptr;
			exchange(atomic_ptr, oldNext, loadOrder, storeOrder);
			return oldNext;
		}

		static void concat(std::atomic<node*> &first, node* n, std::memory_order loadOrder, std::memory_order storeOrder){
			if (n == nullptr) return;
			std::atomic<node*> *atomic_ptr_ptr = &first;
			node* temp = nullptr;
			while (!atomic_ptr_ptr->compare_exchange_weak(temp, n, storeOrder, loadOrder)) {
				while ((temp = atomic_ptr_ptr.load(loadOrder)) == spinDummy);
				if (temp == deadDummy) { //start over
					atomic_ptr_ptr = &first;
					temp = nullptr;
				}
				else {
					atomic_ptr_ptr = &temp->next;
					temp = nullptr;
				}
			}
		}

		static bool remove_node(std::atomic<node*> &atomic_ptr, T &value, std::memory_order loadOrder, std::memory_order storeOrder) {
			std::memory_order combinedOrder = combine_memory_order(loadOrder, storeOrder);
			node *x = lockLoadTransferOwnership(atomic_ptr, storeOrder, loadOrder);
			if (x == nullptr) {
				if (atomic_ptr.load(loadOrder) == deadDummy) return false;
				node *temp = nullptr;
				storeTransferOwnershipUnlock(atomic_ptr, temp, storeOrder, loadOrder);
				return false;
			}
			value = x->value;
			node *y = lockLoadTransferOwnership(x->next, loadOrder, storeOrder);
			storeTransferOwnershipUnlock(atomic_ptr, y, loadOrder, storeOrder);
			node *temp = deadDummy;
			storeTransferOwnershipUnlock(x->next, temp, loadOrder, storeOrder);
			loseOwnership(x, loadOrder, storeOrder);
			return true;
		}
	};

	#undef deadDummy
	#undef spinDummy

	/*************************************/
	/***                               ***/
	/***      Test Code                ***/
	/***                               ***/
	/*************************************/

	#ifdef _MSC_VER //for doing leak detection
	#	define _CRTDBG_MAP_ALLOC
	#	include <stdlib.h>
	#	include <crtdbg.h>
	#	define DUMP _CrtDumpMemoryLeaks()
	#else
	#	define DUMP
	#endif 

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
				assert(a.pop_front(v));
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
				assert(a.pop_front(v));
				assert(v == 5);
				assert(a.pop_front(v));
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
					assert(a.pop_front(v));
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
					int v;
					assert(a.pop_front(v));
					std::cout << v << " ";
					assert(remainingNumbers.erase(v));
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
								assert(remainingNumbers.erase(x));
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

	int main(int argc, char** argv)
	{
	#ifdef _MSC_VER
		_CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);
	#endif
		lock_free_forward_list_tests::test_all();
		return 0;
	}

