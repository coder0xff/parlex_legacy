#ifndef LOCK_FREE_FORWARD_LIST_H_INCLUDED
#define LOCK_FREE_FORWARD_LIST_H_INCLUDED

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
	static std::unique_ptr<void*> deadDummy(new void* ());
	return deadDummy.get();
}

inline void* lock_free_forward_list_get_spinDummy() {
	static std::unique_ptr<void*> spinDummy(new void* ());
	return spinDummy.get();
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
		std::atomic<node*> next2;
		std::atomic<int> referenceCount;

		node(T const &value) : value(value), next2(nullptr), referenceCount(1) {}
		node(T &&value) : value(std::move(value)), next2(nullptr), referenceCount(1) {}
		template<class... U>
		node(U... params) : value(std::forward(params)...), next2(nullptr), referenceCount(1) {}
		~node() {
			node* n = lockLoadTransferOwnership(next2, std::memory_order_seq_cst, std::memory_order_seq_cst);
			loseOwnership(n, std::memory_order_seq_cst, std::memory_order_seq_cst);
			next2.store(deadDummy);
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
		assert(right != deadDummy);
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
		assert(n != deadDummy);
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
		x = gainOwnership(n, loadOrder, storeOrder);
	}

	template<class T>
	//construction is lock free
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
			node *temp = lockLoadGainOwnershipUnlock(current->next2, std::memory_order_seq_cst, std::memory_order_seq_cst);
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
	bool clear(std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
		node *oldFirst = nullptr;
		exchange(first, oldFirst, loadOrder, storeOrder);
		bool result = oldFirst;
		loseOwnership(oldFirst, loadOrder, storeOrder);
		return result;
	}

	//lock free
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

	//lock free
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

	//lock free
	const_iterator cbegin(std::memory_order loadOrder = std::memory_order_seq_cst) {
		return begin();
	}

	//lock free
	const_iterator cend() {
		return const_iterator();
	}

	//lock free
	iterator insert_after(const_iterator position, T const &value, std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
		return insert_node(position.current->next2, new node(value), loadOrder, storeOrder);
	}

	//lock free
	iterator insert_after(const_iterator position, T&& value, std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
		return insert_node(position.current->next2, new node(value), loadOrder, storeOrder);
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
		node *n = seperate(position.current->next2, loadOrder, storeOrder);
		if (!n) return false;
		result = new lock_free_forward_list<T>();
		result->first = n;
		return true;
	}

	//NOT lock free
	bool erase_after(const_iterator position, T &value, std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
		return remove_node(position.current->next2, value, loadOrder, storeOrder);
	}

	//NOT lock free on a, lock free on b
	friend void swap(lock_free_forward_list& a, lock_free_forward_list& b)
	{
		exchange(a.first, b.first);
	}

private:
	std::atomic<node*> first;

	iterator insert_node(std::atomic<node*> &atomic_ptr, node* n, std::memory_order loadOrder, std::memory_order storeOrder) {
		n->next2.store(n, storeOrder);
		exchange(n->next2, atomic_ptr, loadOrder, storeOrder);
		return iterator(n);
	}

	node* seperate(std::atomic<node*> &atomic_ptr, std::memory_order loadOrder, std::memory_order storeOrder) {
		node* oldNext = nullptr;
		exchange(atomic_ptr, oldNext, loadOrder, storeOrder);
		return oldNext;
	}

	void concat(node* n, std::memory_order loadOrder, std::memory_order storeOrder){
		if (n == nullptr) return;
		std::atomic<node*> *atomic_ptr_ptr = &first;
		node* temp = nullptr;
		while (!atomic_ptr_ptr->compare_exchange_weak(temp, n, storeOrder, loadOrder)) {
			while ((temp = atomic_ptr_ptr.load(loadOrder)) == spinDummy);
			if (temp == deadDummy) { //start over
				atomic_ptr_ptr = &first;
				temp = nullptr;
			} else {
				atomic_ptr_ptr = &temp->next;
				temp = nullptr;
			}
		}
	}

	bool remove_node(std::atomic<node*> &atomic_ptr, T &value, std::memory_order loadOrder, std::memory_order storeOrder) {
		std::memory_order combinedOrder = combine_memory_order(loadOrder, storeOrder);
		node *x = lockLoadTransferOwnership(atomic_ptr, storeOrder, loadOrder);
		if (x == nullptr) {
			node *temp = nullptr;
			storeTransferOwnershipUnlock(atomic_ptr, temp, storeOrder, loadOrder);
			return true;
		}
		value = x->value;
		node *y = lockLoadTransferOwnership(x->next2, loadOrder, storeOrder);
		storeTransferOwnershipUnlock(atomic_ptr, y, loadOrder, storeOrder);
		node *temp = nullptr;
		storeTransferOwnershipUnlock(x->next2, temp, loadOrder, storeOrder);
		loseOwnership(x, loadOrder, storeOrder);
		return true;
	}
};

#undef deadDummy
#undef spinDummy

#endif
