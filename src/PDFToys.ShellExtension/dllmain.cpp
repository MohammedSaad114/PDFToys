// dllmain.cpp : Implementation of DllMain.

#include "pch.h"
#include "framework.h"
#include "resource.h"
#include "PDFToysShellExtension_i.h"
#include "dllmain.h"
#include "MenuBitmapCache.h"

CPDFToysShellExtensionModule _AtlModule;

// DLL Entry Point
extern "C" BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
	if (dwReason == DLL_PROCESS_DETACH)
	{
		MenuBitmapCache::Shutdown();
	}

	hInstance;
	lpReserved;
	return _AtlModule.DllMain(dwReason, lpReserved);
}
