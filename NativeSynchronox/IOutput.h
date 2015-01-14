#ifndef _IOUTPUT_H_
#define _IOUTPUT_H_

#include <set>

namespace Synchronox {
	class Box;

	class IInput;

	class IOutput
	{
	public:
		virtual ~IOutput();
	protected:
		virtual Box* GetOwner() = 0;
	private:
		friend class Collective;
		virtual std::set<IInput*> GetConnectedInputs() = 0;
	};
}

#endif