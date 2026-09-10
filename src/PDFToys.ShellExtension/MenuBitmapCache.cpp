#include "pch.h"
#include "MenuBitmapCache.h"
#include "resource.h"
#include <map>
#include <shlwapi.h>
#include <string>

#pragma comment(lib, "shlwapi.lib")

namespace {
    std::map<int, HBITMAP> g_bitmapCache;

    using GetSystemMetricsForDpiFn = int (WINAPI*)(int, UINT);

    GetSystemMetricsForDpiFn ResolveGetSystemMetricsForDpi()
    {
        static const auto getter = reinterpret_cast<GetSystemMetricsForDpiFn>(
            GetProcAddress(GetModuleHandleW(L"user32.dll"), "GetSystemMetricsForDpi"));
        return getter;
    }

    int GetSystemMetricForDpi(int metric, UINT dpi)
    {
        const auto getter = ResolveGetSystemMetricsForDpi();
        if (getter != nullptr)
        {
            return getter(metric, dpi);
        }

        return MulDiv(GetSystemMetrics(metric), static_cast<int>(dpi), 96);
    }

    UINT GetMenuDpi(HWND hwnd)
    {
        if (hwnd != nullptr)
        {
            const UINT dpi = GetDpiForWindow(hwnd);
            if (dpi != 0)
            {
                return dpi;
            }
        }

        const UINT systemDpi = GetDpiForSystem();
        if (systemDpi != 0)
        {
            return systemDpi;
        }

        return 96;
    }

    HINSTANCE GetCurrentModuleInstance()
    {
        HMODULE module = nullptr;
        GetModuleHandleExW(
            GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
            reinterpret_cast<LPCWSTR>(&GetCurrentModuleInstance),
            &module);
        return module;
    }

    std::wstring GetModulePath()
    {
        wchar_t dllPath[MAX_PATH];
        if (GetModuleFileNameW(GetCurrentModuleInstance(), dllPath, MAX_PATH) == 0)
        {
            return std::wstring();
        }

        return dllPath;
    }

    std::wstring GetAppExePath()
    {
        const std::wstring modulePath = GetModulePath();
        if (modulePath.empty())
        {
            return modulePath;
        }

        wchar_t directory[MAX_PATH];
        wcscpy_s(directory, modulePath.c_str());
        if (!PathRemoveFileSpecW(directory))
        {
            return std::wstring();
        }

        return std::wstring(directory) + L"\\PDFToys.App.exe";
    }

    HICON ExtractIconFromPath(const std::wstring& path, int resourceIndex, int size)
    {
        HICON icons[1] = {};
        const UINT extracted = PrivateExtractIconsW(
            path.c_str(),
            resourceIndex,
            size,
            size,
            icons,
            nullptr,
            1,
            0);
        if (extracted == 0 || icons[0] == nullptr)
        {
            return nullptr;
        }

        return icons[0];
    }
} // namespace

int MenuBitmapCache::GetBitmapSize(HWND hwnd, int metric, int fallback96)
{
    const UINT dpi = GetMenuDpi(hwnd);
    int size = GetSystemMetricForDpi(metric, dpi);
    if (size <= 0)
    {
        size = MulDiv(fallback96, static_cast<int>(dpi), 96);
    }

    return size;
}

HICON MenuBitmapCache::LoadMenuIcon(int size)
{
    const std::wstring modulePath = GetModulePath();
    const int sizesToTry[] = { size, 32, 24, 20, 16 };

    if (!modulePath.empty())
    {
        for (int trySize : sizesToTry)
        {
            HICON icon = ExtractIconFromPath(modulePath, -static_cast<int>(IDI_PDFTOYS), trySize);
            if (icon != nullptr)
            {
                return icon;
            }
        }
    }

    const std::wstring appPath = GetAppExePath();
    if (!appPath.empty())
    {
        for (int trySize : sizesToTry)
        {
            HICON icon = ExtractIconFromPath(appPath, 0, trySize);
            if (icon != nullptr)
            {
                return icon;
            }
        }
    }

    HINSTANCE module = GetCurrentModuleInstance();
    for (int trySize : sizesToTry)
    {
        HICON icon = static_cast<HICON>(LoadImageW(
            module,
            MAKEINTRESOURCEW(IDI_PDFTOYS),
            IMAGE_ICON,
            trySize,
            trySize,
            0));
        if (icon != nullptr)
        {
            return icon;
        }
    }

    return nullptr;
}

HBITMAP MenuBitmapCache::CreateBitmapFromIcon(HICON icon, int size)
{
    HDC screenDc = GetDC(nullptr);
    if (screenDc == nullptr)
    {
        return nullptr;
    }

    HDC memoryDc = CreateCompatibleDC(screenDc);
    if (memoryDc == nullptr)
    {
        ReleaseDC(nullptr, screenDc);
        return nullptr;
    }

    BITMAPINFO bitmapInfo = {};
    bitmapInfo.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    bitmapInfo.bmiHeader.biWidth = size;
    bitmapInfo.bmiHeader.biHeight = -size;
    bitmapInfo.bmiHeader.biPlanes = 1;
    bitmapInfo.bmiHeader.biBitCount = 32;

    void* bits = nullptr;
    HBITMAP bitmap = CreateDIBSection(memoryDc, &bitmapInfo, DIB_RGB_COLORS, &bits, nullptr, 0);
    if (bitmap == nullptr || bits == nullptr)
    {
        DeleteDC(memoryDc);
        ReleaseDC(nullptr, screenDc);
        return nullptr;
    }

    memset(bits, 0, static_cast<size_t>(size) * static_cast<size_t>(size) * 4);

    HGDIOBJ previousBitmap = SelectObject(memoryDc, bitmap);
    DrawIconEx(memoryDc, 0, 0, icon, size, size, 0, nullptr, DI_NORMAL);
    SelectObject(memoryDc, previousBitmap);

    DeleteDC(memoryDc);
    ReleaseDC(nullptr, screenDc);
    return bitmap;
}

HBITMAP MenuBitmapCache::GetBitmapForSize(int size)
{
    const auto existing = g_bitmapCache.find(size);
    if (existing != g_bitmapCache.end())
    {
        return existing->second;
    }

    HICON icon = LoadMenuIcon(size);
    if (icon == nullptr)
    {
        return nullptr;
    }

    HBITMAP bitmap = CreateBitmapFromIcon(icon, size);
    DestroyIcon(icon);
    if (bitmap != nullptr)
    {
        g_bitmapCache.emplace(size, bitmap);
    }

    return bitmap;
}

HBITMAP MenuBitmapCache::GetBitmap(HWND hwnd)
{
    const int size = GetBitmapSize(hwnd, SM_CXSMICON, 16);
    return GetBitmapForSize(size);
}

void MenuBitmapCache::Shutdown()
{
    for (const auto& entry : g_bitmapCache)
    {
        if (entry.second != nullptr)
        {
            DeleteObject(entry.second);
        }
    }

    g_bitmapCache.clear();
}
