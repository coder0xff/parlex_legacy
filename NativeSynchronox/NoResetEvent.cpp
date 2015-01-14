#include "NoResetEvent.h"
#include <atomic>

NoResetEvent::NoResetEvent() : _state(false) { }

NoResetEvent::~NoResetEvent() { }

void NoResetEvent::Wait() {
	std::unique_lock<std::mutex> lock(_sync);
	while (!_state) {
		_underlying.wait(lock);
	}
}

void NoResetEvent::Set() {
	std::unique_lock<std::mutex> lock(_sync);
	_state = true;
	_underlying.notify_all();
}