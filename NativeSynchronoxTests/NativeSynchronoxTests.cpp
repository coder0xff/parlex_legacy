// NativeSynchronoxTests.cpp : Defines the entry point for the console application.
//

#define _CRTDBG_MAP_ALLOC
#include <stdlib.h>
#include <crtdbg.h>

#include "stdafx.h"
#include "lock_free_forward_list_tests.h"

int _tmain(int argc, _TCHAR* argv[])
{
	_CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);
	//malloc(500);
	lock_free_forward_list_tests::test_all();
	return 0;
}
