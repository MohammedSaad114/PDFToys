#pragma once

#include <windows.h>

class MenuBitmapCache
{
public:
    static HBITMAP GetBitmap(HWND hwnd);
    static void Shutdown();

private:
    static int GetBitmapSize(HWND hwnd, int metric, int fallback96);
    static HBITMAP GetBitmapForSize(int size);
    static HICON LoadMenuIcon(int size);
    static HBITMAP CreateBitmapFromIcon(HICON icon, int size);
};
