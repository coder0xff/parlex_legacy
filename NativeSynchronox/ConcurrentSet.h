#ifndef _CONCURRENT_SET_H_
#define _CONCURRENT_SET_H_

#include <set>
#include <mutex>

namespace Synchronox {
	template<typename T>
	class ConcurrentSet
	{
	public:
		ConcurrentSet();
		~ConcurrentSet();
		void insert(T const &val);
	private:
		std::set<T> underlying;
		std::mutex sync;
	};
}

#endif