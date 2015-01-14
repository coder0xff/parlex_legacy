#ifndef NO_RESET_EVENT_H
#define NO_RESET_EVENT_H

#include <condition_variable>
#include <atomic>

class NoResetEvent
{
public:
	NoResetEvent() : _state(false) {}
	NoResetEvent(const NoResetEvent& other) = delete;
	void Wait();
	void Set();
	bool IsSet();
private:
	std::condition_variable _underlying;
	std::mutex _sync;
	std::atomic<bool> _state;
};

#endif