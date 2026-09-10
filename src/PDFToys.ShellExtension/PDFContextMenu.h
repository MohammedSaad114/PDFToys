// PDFContextMenu.h : Declaration of the CPDFContextMenu

#pragma once
#include "resource.h" // main symbols

#include "PDFToysShellExtension_i.h"
#include <shobjidl.h>
#include <string>
#include <vector>

#if defined(_WIN32_WCE) && !defined(_CE_DCOM) && !defined(_CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA)
#error "Single-threaded COM objects are not properly supported on Windows CE platform, such as the Windows Mobile platforms that do not include full DCOM support. Define _CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA to force ATL to support creating single-thread COM object's and allow use of it's single-threaded COM object implementations. The threading model in your rgs file was set to 'Free' as that is the only threading model supported in non DCOM Windows CE platforms."
#endif

using namespace ATL;

// CPDFContextMenu
class ATL_NO_VTABLE CPDFContextMenu : public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<CPDFContextMenu, &CLSID_PDFContextMenu>,
	public IPDFContextMenu,
	public IShellExtInit,
	public IContextMenu
{
public:
	struct MenuCommand
	{
		std::wstring commandId;
		std::wstring verb;
		std::wstring label;
		std::wstring helpText;
		std::wstring arguments;
	};

	CPDFContextMenu()
	{
	}

	// IShellExtInit
	IFACEMETHODIMP Initialize(PCIDLIST_ABSOLUTE pidlFolder, IDataObject* pdtobj, HKEY hkeyProgID);

	// IContextMenu
	IFACEMETHODIMP QueryContextMenu(HMENU hmenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, UINT uFlags);
	IFACEMETHODIMP InvokeCommand(CMINVOKECOMMANDINFO* pici);
	IFACEMETHODIMP GetCommandString(UINT_PTR idCmd, UINT uType, UINT* pReserved, CHAR* pszName, UINT cchMax);

	DECLARE_REGISTRY_RESOURCEID(106)

	BEGIN_COM_MAP(CPDFContextMenu)
		COM_INTERFACE_ENTRY(IPDFContextMenu)
		COM_INTERFACE_ENTRY(IShellExtInit)
		COM_INTERFACE_ENTRY(IContextMenu)
	END_COM_MAP()

	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}

private:
	std::vector<std::wstring> m_selectedFiles;
	std::vector<MenuCommand> m_activeCommands;

};

OBJECT_ENTRY_AUTO(__uuidof(PDFContextMenu), CPDFContextMenu)
