// PDFContextMenu.cpp : Implementation of CPDFContextMenu

#include "pch.h"
#include "PDFContextMenu.h"
#include "MenuBitmapCache.h"
#include <shlwapi.h>
#include <algorithm>
#include <cwctype>

#pragma comment(lib, "shlwapi.lib")

namespace {
	const wchar_t* kContractPrefix = L"--contract-version 1";
	const wchar_t* kAppVerb = L"open";

	std::wstring ToLower(std::wstring value)
	{
		std::transform(value.begin(), value.end(), value.begin(), towlower);
		return value;
	}

	std::wstring GetLowerExtension(const std::wstring& path)
	{
		const wchar_t* ext = PathFindExtensionW(path.c_str());
		if (ext == nullptr || *ext == L'\0')
		{
			return std::wstring();
		}
		return ToLower(std::wstring(ext));
	}

	std::wstring NormalizeExtension(const std::wstring& ext)
	{
		if (ext == L".xlxs") return L".xlsx";
		if (ext == L".doxc") return L".docx";
		if (ext == L".ppxt") return L".pptx";
		if (ext == L".jepg") return L".jpeg";
		return ext;
	}

	std::wstring GetNormalizedExtension(const std::wstring& path)
	{
		return NormalizeExtension(GetLowerExtension(path));
	}

	bool IsPdfPath(const std::wstring& path)
	{
		return GetNormalizedExtension(path) == L".pdf";
	}

	bool IsSupportedToPdfPath(const std::wstring& path)
	{
		const std::wstring ext = GetNormalizedExtension(path);
		return
			ext == L".doc" ||
			ext == L".docx" ||
			ext == L".ppt" ||
			ext == L".pptx" ||
			ext == L".xls" ||
			ext == L".xlsx" ||
			ext == L".jpg" ||
			ext == L".jpeg" ||
			ext == L".png" ||
			ext == L".txt" ||
			ext == L".md" ||
			ext == L".svg";
	}

	std::wstring QuoteArg(const std::wstring& value)
	{
		std::wstring escaped;
		escaped.reserve(value.size());
		for (wchar_t ch : value)
		{
			if (ch == L'"')
			{
				escaped += L"\\\"";
			}
			else
			{
				escaped += ch;
			}
		}
		return L"\"" + escaped + L"\"";
	}

	std::wstring BuildCommandArguments(
		const std::wstring& commandId,
		const std::vector<std::wstring>& inputs = {},
		const std::wstring& workspace = std::wstring())
	{
		std::wstring args = std::wstring(kContractPrefix) + L" --command " + commandId;
		for (const auto& input : inputs)
		{
			args += L" --input ";
			args += QuoteArg(input);
		}
		if (!workspace.empty())
		{
			args += L" --workspace ";
			args += QuoteArg(workspace);
		}
		return args;
	}

	std::vector<std::wstring> SelectByPredicate(
		const std::vector<std::wstring>& files,
		bool (*predicate)(const std::wstring&))
	{
		std::vector<std::wstring> selected;
		selected.reserve(files.size());
		for (const auto& file : files)
		{
			if (predicate(file))
			{
				selected.push_back(file);
			}
		}
		return selected;
	}

	bool HasSingleExtension(const std::vector<std::wstring>& files)
	{
		if (files.empty())
		{
			return false;
		}

		const std::wstring expected = GetNormalizedExtension(files.front());
		if (expected.empty())
		{
			return false;
		}

		for (const auto& file : files)
		{
			if (GetNormalizedExtension(file) != expected)
			{
				return false;
			}
		}

		return true;
	}

	std::wstring TryResolveWorkspace(const std::vector<std::wstring>& files)
	{
		if (files.empty()) return std::wstring();
		wchar_t directory[MAX_PATH];
		wcscpy_s(directory, files.front().c_str());
		if (PathRemoveFileSpecW(directory) && directory[0] != L'\0')
		{
			return directory;
		}
		return std::wstring();
	}

	void AddCommand(
		std::vector<CPDFContextMenu::MenuCommand>& commands,
		const std::wstring& commandId,
		const std::wstring& verb,
		const std::wstring& label,
		const std::wstring& helpText,
		const std::wstring& args)
	{
		CPDFContextMenu::MenuCommand command;
		command.commandId = commandId;
		command.verb = verb;
		command.label = label;
		command.helpText = helpText;
		command.arguments = args;
		commands.push_back(command);
	}

	std::vector<CPDFContextMenu::MenuCommand> BuildAvailableCommands(const std::vector<std::wstring>& files)
	{
		std::vector<CPDFContextMenu::MenuCommand> commands;
		const auto pdfs = SelectByPredicate(files, IsPdfPath);
		const auto toPdfSupported = SelectByPredicate(files, IsSupportedToPdfPath);
		const std::wstring workspace = TryResolveWorkspace(files);
		const bool allPdf = !files.empty() && pdfs.size() == files.size();
		const bool allToPdfSupported = !files.empty() && toPdfSupported.size() == files.size();
		const bool sameExtension = HasSingleExtension(files);

		AddCommand(
			commands,
			L"home",
			L"open-home",
			L"Open PDFToys",
			L"Open PDFToys home screen",
			workspace.empty() ? BuildCommandArguments(L"home") : BuildCommandArguments(L"home", {}, workspace));

		if (allPdf && files.size() == 1)
		{
			const std::wstring& inputFile = pdfs[0];

			AddCommand(
				commands,
				L"split",
				L"split-pdf",
				L"Split PDF",
				L"Split the selected PDF in PDFToys",
				BuildCommandArguments(L"split", { inputFile }));
			AddCommand(
				commands,
				L"compress",
				L"compress-pdf",
				L"Compress PDF",
				L"Compress the selected PDF in PDFToys",
				BuildCommandArguments(L"compress", { inputFile }));
			AddCommand(
				commands,
				L"organize-pages",
				L"organize-pages",
				L"Organize Pages",
				L"Reorder, rotate, delete, or extract pages in the selected PDF",
				BuildCommandArguments(L"organize-pages", { inputFile }));
			AddCommand(
				commands,
				L"protect",
				L"protect-pdf",
				L"Protect PDF",
				L"Encrypt the selected PDF in PDFToys",
				BuildCommandArguments(L"protect", { inputFile }));
			AddCommand(
				commands,
				L"unlock",
				L"unlock-pdf",
				L"Unlock PDF",
				L"Remove protection from the selected PDF in PDFToys",
				BuildCommandArguments(L"unlock", { inputFile }));
			AddCommand(
				commands,
				L"pdf-to-jpg",
				L"pdf-to-jpg",
				L"Convert PDF to JPG",
				L"Convert the selected PDF to JPG",
				BuildCommandArguments(L"pdf-to-jpg", { inputFile }));
			AddCommand(
				commands,
				L"pdf-to-jpeg",
				L"pdf-to-jpeg",
				L"Convert PDF to JPEG",
				L"Convert the selected PDF to JPEG",
				BuildCommandArguments(L"pdf-to-jpeg", { inputFile }));
			AddCommand(
				commands,
				L"pdf-to-png",
				L"pdf-to-png",
				L"Convert PDF to PNG",
				L"Convert the selected PDF to PNG",
				BuildCommandArguments(L"pdf-to-png", { inputFile }));
			AddCommand(
				commands,
				L"pdf-to-markdown",
				L"pdf-to-markdown",
				L"Convert PDF to Markdown",
				L"Convert the selected PDF to Markdown",
				BuildCommandArguments(L"pdf-to-markdown", { inputFile }));
		}

		if (allPdf && files.size() >= 2)
		{
			AddCommand(
				commands,
				L"merge",
				L"merge-pdfs",
				L"Merge PDFs",
				L"Merge selected PDFs in PDFToys",
				BuildCommandArguments(L"merge", pdfs));
			AddCommand(
				commands,
				L"compress",
				L"compress-pdfs",
				L"Compress PDFs",
				L"Compress selected PDFs in PDFToys",
				BuildCommandArguments(L"compress", pdfs));
			AddCommand(
				commands,
				L"protect",
				L"protect-pdfs",
				L"Protect PDFs",
				L"Encrypt selected PDFs in PDFToys",
				BuildCommandArguments(L"protect", pdfs));
			AddCommand(
				commands,
				L"unlock",
				L"unlock-pdfs",
				L"Unlock PDFs",
				L"Remove protection from selected PDFs in PDFToys",
				BuildCommandArguments(L"unlock", pdfs));
			AddCommand(
				commands,
				L"pdf-to-jpg",
				L"pdfs-to-jpg",
				L"Convert PDFs to JPG",
				L"Convert selected PDFs to JPG",
				BuildCommandArguments(L"pdf-to-jpg", pdfs));
			AddCommand(
				commands,
				L"pdf-to-jpeg",
				L"pdfs-to-jpeg",
				L"Convert PDFs to JPEG",
				L"Convert selected PDFs to JPEG",
				BuildCommandArguments(L"pdf-to-jpeg", pdfs));
			AddCommand(
				commands,
				L"pdf-to-png",
				L"pdfs-to-png",
				L"Convert PDFs to PNG",
				L"Convert selected PDFs to PNG",
				BuildCommandArguments(L"pdf-to-png", pdfs));
			AddCommand(
				commands,
				L"pdf-to-markdown",
				L"pdfs-to-markdown",
				L"Convert PDFs to Markdown",
				L"Convert selected PDFs to Markdown",
				BuildCommandArguments(L"pdf-to-markdown", pdfs));
		}

		if (allToPdfSupported && files.size() == 1)
		{
			AddCommand(
				commands,
				L"to-pdf",
				L"to-pdf",
				L"Convert to PDF",
				L"Convert selected files to PDF in PDFToys",
				BuildCommandArguments(L"to-pdf", files));
		}
		else if (allToPdfSupported && files.size() >= 2 && sameExtension)
		{
			AddCommand(
				commands,
				L"combine-to-pdf",
				L"combine-to-pdf",
				L"Combine into PDF",
				L"Create one PDF from all selected files",
				BuildCommandArguments(L"combine-to-pdf", files));
			AddCommand(
				commands,
				L"convert-each-to-pdf",
				L"convert-each-to-pdf",
				L"Convert Each to PDF",
				L"Create one PDF per selected file",
				BuildCommandArguments(L"convert-each-to-pdf", files));
		}

		return commands;
	}
	void ApplyMenuBitmap(HMENU menu, UINT itemId, bool byPosition, HBITMAP bitmap)
	{
		if (bitmap == nullptr)
		{
			return;
		}

		SetMenuItemBitmaps(menu, itemId, byPosition ? TRUE : FALSE, bitmap, bitmap);
	}

	void ApplyPopupMenuBitmap(HMENU menu, UINT indexMenu, HBITMAP bitmap)
	{
		if (bitmap == nullptr)
		{
			return;
		}

		MENUITEMINFOW itemInfo = {};
		itemInfo.cbSize = sizeof(itemInfo);
		itemInfo.fMask = MIIM_BITMAP;
		itemInfo.hbmpItem = bitmap;
		SetMenuItemInfoW(menu, indexMenu, TRUE, &itemInfo);

		SetMenuItemBitmaps(menu, indexMenu, TRUE, bitmap, bitmap);
	}

	bool IsVerbMatch(const CMINVOKECOMMANDINFO* pici, const wchar_t* expectedVerb)
	{
		if (pici == nullptr || expectedVerb == nullptr)
		{
			return false;
		}

		if (HIWORD(pici->lpVerb) == 0)
		{
			return false;
		}

		if (pici->cbSize >= sizeof(CMINVOKECOMMANDINFOEX) &&
			(pici->fMask & CMIC_MASK_UNICODE) != 0)
		{
			const auto* piciex = reinterpret_cast<const CMINVOKECOMMANDINFOEX*>(pici);
			if (piciex->lpVerbW != nullptr)
			{
				return _wcsicmp(piciex->lpVerbW, expectedVerb) == 0;
			}
		}

		if (pici->lpVerb != nullptr)
		{
			return _stricmp(pici->lpVerb, CW2A(expectedVerb)) == 0;
		}

		return false;
	}
} // namespace

// CPDFContextMenu
// 1. Windows calls this immediately when the user right-clicks a file.
// We use this to grab the file path of the PDF they clicked.
IFACEMETHODIMP CPDFContextMenu::Initialize(PCIDLIST_ABSOLUTE pidlFolder, IDataObject* pdtobj, HKEY hkeyProgID)
{
	if (!pdtobj) return E_INVALIDARG;

	FORMATETC fe = { CF_HDROP, NULL, DVASPECT_CONTENT, -1, TYMED_HGLOBAL };
	STGMEDIUM stm;

	m_selectedFiles.clear(); // Clear out any old files

	if (SUCCEEDED(pdtobj->GetData(&fe, &stm)))
	{
		HDROP hDrop = static_cast<HDROP>(GlobalLock(stm.hGlobal));
		if (hDrop)
		{
			// Ask Windows exactly how many files the user highlighted
			UINT fileCount = DragQueryFileW(hDrop, 0xFFFFFFFF, NULL, 0);

			// Loop through every single file and save its path
			for (UINT i = 0; i < fileCount; i++)
			{
				wchar_t filePath[MAX_PATH];
				if (DragQueryFileW(hDrop, i, filePath, ARRAYSIZE(filePath)))
				{
					m_selectedFiles.push_back(filePath);
				}
			}
			GlobalUnlock(stm.hGlobal);
		}
		ReleaseStgMedium(&stm);
	}
	return S_OK;
}

// 2. Windows calls this to build the visual right-click menu.
// We literally inject our button into the list here.
IFACEMETHODIMP CPDFContextMenu::QueryContextMenu(HMENU hmenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, UINT uFlags)
{
	UNREFERENCED_PARAMETER(idCmdLast);

	// If Windows is just asking for a default menu (like a double-click), skip it.
	if (CMF_DEFAULTONLY & uFlags) return MAKE_HRESULT(SEVERITY_SUCCESS, 0, USHORT(0));

	m_activeCommands = BuildAvailableCommands(m_selectedFiles);
	if (m_activeCommands.empty())
	{
		return MAKE_HRESULT(SEVERITY_SUCCESS, 0, USHORT(0));
	}

	HMENU subMenu = CreatePopupMenu();
	if (subMenu == nullptr)
	{
		return E_OUTOFMEMORY;
	}

	const HBITMAP menuBitmap = MenuBitmapCache::GetBitmap(nullptr);
	for (size_t i = 0; i < m_activeCommands.size(); ++i)
	{
		const UINT commandId = idCmdFirst + static_cast<UINT>(i);
		if (!InsertMenuW(
			subMenu,
			static_cast<UINT>(i),
			MF_STRING | MF_BYPOSITION,
			commandId,
			m_activeCommands[i].label.c_str()))
		{
			DestroyMenu(subMenu);
			return HRESULT_FROM_WIN32(GetLastError());
		}

		ApplyMenuBitmap(subMenu, commandId, false, menuBitmap);
	}

	MENUITEMINFOW parentItem = {};
	parentItem.cbSize = sizeof(parentItem);
	parentItem.fMask = MIIM_STRING | MIIM_SUBMENU | MIIM_BITMAP;
	parentItem.hSubMenu = subMenu;
	parentItem.dwTypeData = const_cast<LPWSTR>(L"PDFToys");
	parentItem.hbmpItem = menuBitmap;

	if (!InsertMenuItemW(hmenu, indexMenu, TRUE, &parentItem))
	{
		DestroyMenu(subMenu);
		return HRESULT_FROM_WIN32(GetLastError());
	}

	ApplyPopupMenuBitmap(hmenu, indexMenu, menuBitmap);

	// Tell Windows how many items we added
	return MAKE_HRESULT(SEVERITY_SUCCESS, 0, USHORT(m_activeCommands.size()));
}

// 3. This just provides the hover-text (tooltip) at the bottom of the screen.
IFACEMETHODIMP CPDFContextMenu::GetCommandString(UINT_PTR idCmd, UINT uType, UINT* pReserved, CHAR* pszName, UINT cchMax)
{
	UNREFERENCED_PARAMETER(pReserved);

	if ((uType != GCS_HELPTEXTW && uType != GCS_VERBW) || pszName == nullptr || cchMax == 0)
	{
		return E_INVALIDARG;
	}

	if (idCmd >= m_activeCommands.size())
	{
		return E_INVALIDARG;
	}

	const auto& command = m_activeCommands[idCmd];
	if (uType == GCS_HELPTEXTW)
	{
		wcscpy_s(reinterpret_cast<PWSTR>(pszName), cchMax, command.helpText.c_str());
	}
	else
	{
		wcscpy_s(reinterpret_cast<PWSTR>(pszName), cchMax, command.verb.c_str());
	}

	return S_OK;
}

// 4. THE ACTION! Windows runs this when the user actually clicks your "PDFToys" button.
IFACEMETHODIMP CPDFContextMenu::InvokeCommand(CMINVOKECOMMANDINFO* pici)
{
	if (pici == nullptr)
	{
		return E_INVALIDARG;
	}

	size_t commandIndex = SIZE_MAX;
	if (HIWORD(pici->lpVerb) == 0)
	{
		commandIndex = LOWORD(pici->lpVerb);
	}
	else
	{
		for (size_t i = 0; i < m_activeCommands.size(); ++i)
		{
			if (IsVerbMatch(pici, m_activeCommands[i].verb.c_str()))
			{
				commandIndex = i;
				break;
			}
		}
	}

	if (commandIndex >= m_activeCommands.size())
	{
		return E_INVALIDARG;
	}

	wchar_t dllPath[MAX_PATH];
	const DWORD dllPathLength = GetModuleFileNameW(_AtlBaseModule.GetModuleInstance(), dllPath, ARRAYSIZE(dllPath));
	if (dllPathLength == 0)
	{
		return HRESULT_FROM_WIN32(GetLastError());
	}

	if (dllPathLength >= ARRAYSIZE(dllPath))
	{
		return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
	}

	std::wstring exePath = dllPath;
	size_t pos = exePath.find_last_of(L"\\/");
	if (pos == std::wstring::npos)
	{
		return HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND);
	}

	exePath = exePath.substr(0, pos) + L"\\PDFToys.App.exe";
	if (!PathFileExistsW(exePath.c_str()))
	{
		return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
	}

	const std::wstring& args = m_activeCommands[commandIndex].arguments;

	// Launch the C# application!
	SHELLEXECUTEINFOW sei = { sizeof(sei) };
	sei.fMask = SEE_MASK_DEFAULT;
	sei.hwnd = pici->hwnd;
	sei.nShow = SW_SHOWNORMAL;
	sei.lpVerb = kAppVerb;
	sei.lpFile = exePath.c_str();
	sei.lpParameters = args.c_str(); // Launch command contract arguments

	if (!ShellExecuteExW(&sei))
	{
		return HRESULT_FROM_WIN32(GetLastError());
	}

	return S_OK;
}
