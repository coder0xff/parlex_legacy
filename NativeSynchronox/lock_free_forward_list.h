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
	class node {
		friend class lock_free_forward_list<T>;
		T value;
		std::atomic<node*> next;

		node(T const &value) : value(value) {}
		node(T &&value) : value(std::move(value)) {}
		template<class... U>
		node(U... params) : value(std::forward(params)...) {}

		node *readNext(std::memory_order loadOrder) {
			node *temp;
			while ((temp = next.load(loadOrder)) == spinDummy);
			if (temp == deadDummy) {
				return nullptr;
			}
			return temp;
		}
	};

	template<class T>
	class ForwardIterator {
		friend class lock_free_forward_list;
		node *current;
	public:
		typedef std::forward_iterator_tag iterator_category;
		typedef T value_type;
		typedef T & reference;
		typedef T * pointer;
		ForwardIterator() : current(nullptr) {}
		ForwardIterator(node* n) : current(n) {}
		ForwardIterator(ForwardIterator const &other) : current(other.current) {}
		ForwardIterator& operator=(ForwardIterator const &other) { current = other.current; return *this; }
		T& operator*() { return *current->value; }
		T& operator->() { return *current->value; }
		ForwardIterator operator++() { 
			assert(current != nullptr);
			current = readNext();
			return *this;
		}

		ForwardIterator operator++(int) {
			assert(current != nullptr);
			ForwardIterator temp = *this;
			current = readNext();
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
		node* oldFirst;
		do {
			while ((oldFirst = first.load(loadOrder)) == spinDummy);
		} while (!first.compare_exchange_weak(oldFirst, nullptr, storeOrder, loadOrder));
		assert(oldFirst != deadDummy);

		bool result = oldFirst;
		while (oldFirst) {
			node* ptr = oldFirst->readNext(loadOrder);
			free_node(oldFirst, loadOrder, storeOrder);
			oldFirst = ptr;
		}
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
	iterator begin(std::memory_order loadOrder = std::memory_order_seq_cst) {
		node *n = first.load(loadOrder);
		assert(n != deadDummy);
		return iterator(n);
	}

	//lock free
	iterator end() {
		return iterator{ nullptr };
	}

	//lock free
	const_iterator cbegin(std::memory_order loadOrder = std::memory_order_seq_cst) {
		node *n = first.load(loadOrder);
		assert(n != deadDummy);
		return const_iterator(n);
	}

	//lock free
	const_iterator cend() {
		return const_iterator{ nullptr };
	}

	//lock free
	iterator insert_after(const_iterator position, T const &value, std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
		return insert_node(position.current->next, new node{ value }, loadOrder, storeOrder);
	}

	//lock free
	iterator insert_after(const_iterator position, T&& value, std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
		return insert_node(position.current->next, new node{ value }, loadOrder, storeOrder);
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

	//NOT lock free
	bool erase_after(const_iterator position, T &value, std::memory_order loadOrder = std::memory_order_seq_cst, std::memory_order storeOrder = std::memory_order_seq_cst) {
		return remove_node(position.current->next, value, loadOrder, storeOrder);
	}

	//NOT lock free
	friend void swap(lock_free_forward_list& a, lock_free_forward_list& b)
	{
		using std::swap; // bring in swap for built-in types
		node* temp;
		while ((temp = a.first.exchange(spinDummy, std::memory_order_relaxed)) == spinDummy);
		temp = b.first.exchange(temp);
		a.first.exchange(temp);
		std::swap(a.current, b.current);
	}

private:
	std::memory_order combine_memory_order(std::memory_order loadOrder, std::memory_order storeOrder) {
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

	std::atomic<node*> first;

	iterator insert_node(std::atomic<node*> &atomic_ptr, node* n, std::memory_order loadOrder, std::memory_order storeOrder) {
		node* oldNext;
		do {
			while ((oldNext = atomic_ptr.load(loadOrder)) == spinDummy) ;
			if (oldNext == deadDummy) return iterator();
			n->next.store(oldNext, storeOrder);
		} while (!atomic_ptr.compare_exchange_weak(oldNext, n, storeOrder, loadOrder));
		return iterator(n);
	}

	node* free_node(node* n, std::memory_order loadOrder, std::memory_order storeOrder) {
		node* subsequent = n->next.exchange(deadDummy, combine_memory_order(loadOrder, storeOrder));
		delete n;
		return subsequent;
	}

	node* seperate(std::atomic<node*> &atomic_ptr, std::memory_order loadOrder, std::memory_order storeOrder) {
		node* oldNext;
		do {
			while ((oldNext = atomic_ptr.load(loadOrder)) == spinDummy);
			if (oldNext == deadDummy) {
				return nullptr;
			}
		} while (!atomic_ptr.compare_exchange_weak(oldNext, nullptr, storeOrder, loadOrder));
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
		node* n;
		while ((n = atomic_ptr.exchange(spinDummy, std::memory_order_relaxed)) == spinDummy);
		if (n == deadDummy) {
			atomic_ptr.store(deadDummy, std::memory_order_relaxed);
			return false;
		}
		if (n == nullptr) {
			atomic_ptr.store(nullptr, std::memory_order_relaxed);
			return false;
		}
		node* x = n->next.exchange(deadDummy, combinedOrder);
		atomic_ptr.store(x, storeOrder);
		value = std::move(n->value);
		free_node(n, loadOrder, storeOrder);
		return true;
	}
};

#undef deadDummy
#undef spinDummy

#endif
