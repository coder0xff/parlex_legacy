#ifndef _IINPUT_H_
#define _IINPUT_H_

namespace Synchronox {
	class Box;

	class IOutput;

	class IInput
	{
	public:
		virtual ~IInput();
	protected:
		friend class Box;
		friend class Collective;
		virtual std::set<IOutput *> GetConnectedOutputs() = 0;
		virtual Box* GetOwner() = 0;
		virtual bool GetIsBlocked() = 0;
		virtual void Lock() = 0;
		virtual void Unlock() = 0;
	};
}

#endif