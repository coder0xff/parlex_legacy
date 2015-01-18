// NativeSynchronoxTests.cpp : Defines the entry point for the console application.
//

#ifdef _MSC_VER //for doing leak detection
#	define _CRTDBG_MAP_ALLOC
#	include <stdlib.h>
#	include <crtdbg.h>
#endif

#include "lock_free_forward_list_tests.h"

int main(int argc, char** argv)
{
#ifdef _MSC_VER
	_CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);
#endif
	lock_free_forward_list_tests::test_all();
	return 0;
}