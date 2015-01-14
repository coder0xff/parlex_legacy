#include "Box.h"

namespace Synchronox {

	Box::Box(Collective* collective_ptr)
	{
		collective = collective_ptr;
		collective->Add(this);
	}


	Box::~Box()
	{
	}

}