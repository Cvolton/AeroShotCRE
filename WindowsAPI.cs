/*  AeroShot - Transparent screenshot utility for Windows
    Copyright (C) 2021 Cvolton
    Copyright (C) 2015 toe_head2001
    Copyright (C) 2012 Caleb Joseph

    AeroShot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    AeroShot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>. */

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;

namespace AeroShot
{
    internal delegate bool CallBackPtr(IntPtr hwnd, int lParam);

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowsRect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;

        internal WindowsRect(int x)
        {
            Left = x;
            Top = x;
            Right = x;
            Bottom = x;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowsMargins
    {
        internal int LeftWidth;
        internal int RightWidth;
        internal int TopHeight;
        internal int BottomHeight;

        internal WindowsMargins(int left, int right, int top, int bottom)
        {
            LeftWidth = left;
            RightWidth = right;
            TopHeight = top;
            BottomHeight = bottom;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BitmapInfo
    {
        public Int32 biSize;
        public Int32 biWidth;
        public Int32 biHeight;
        public Int16 biPlanes;
        public Int16 biBitCount;
        public Int32 biCompression;
        public Int32 biSizeImage;
        public Int32 biXPelsPerMeter;
        public Int32 biYPelsPerMeter;
        public Int32 biClrUsed;
        public Int32 biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IconInfoStruct
    {
        internal bool fIcon;
        internal Int32 xHotspot;
        internal Int32 yHotspot;
        internal IntPtr hbmMask;
        internal IntPtr hbmColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CursorInfoStruct
    {
        internal Int32 cbSize;
        internal Int32 flags;
        internal IntPtr hCursor;
        internal PointStruct ptScreenPos;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PointStruct
    {
        internal int X;
        internal int Y;
    }

    internal enum DwmWindowAttribute
    {
        NonClientRenderingEnabled = 1,
        NonClientRenderingPolicy,
        TransitionsForceDisabled,
        AllowNonClientPaint,
        CaptionButtonBounds,
        NonClientRtlLayout,
        ForceIconicRepresentation,
        Flip3DPolicy,
        ExtendedFrameBounds,
        HasIconicBitmap,
        DisallowPeek,
        ExcludedFromPeek,
        Last
    }

    internal static class WindowsApi
    {
        internal struct DWM_COLORIZATION_PARAMS
        {
            public uint clrColor;
            public uint clrAfterGlow;
            public uint nIntensity;
            public uint clrAfterGlowBalance;
            public uint clrBlurBalance;
            public uint clrGlassReflectionIntensity;
            public bool fOpaque;
        }

        internal enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117,

            // http://pinvoke.net/default.aspx/gdi32/GetDeviceCaps.html
        }

        internal static HWND FindWindow(string lpClassName, string lpWindowName)
            => User32.FindWindow(lpClassName, lpWindowName);

        internal static bool SetForegroundWindow(HWND hWnd) => User32.SetForegroundWindow(hWnd);

        internal static bool ShowWindow(HWND hWnd, ShowWindowCommand nCmdShow) => User32.ShowWindow(hWnd, nCmdShow);

        internal static bool SetWindowPos(HWND hWnd, HWND hWndInsertAfter,
            int x, int y, int width, int height, User32.SetWindowPosFlags uFlags)
            => User32.SetWindowPos(hWnd, hWndInsertAfter, x, y, width, height, uFlags);

        internal static bool GetWindowRect(HWND hWnd, out RECT rect) => User32.GetWindowRect(hWnd, out rect);

        internal static long GetWindowLong(HWND hWnd, User32.WindowLongFlags nIndex) =>
            User32.GetWindowLong(hWnd, nIndex);

        internal static HWND GetWindow(HWND hWnd, uint uCmd) => User32.GetWindow(hWnd, uCmd);

        internal static int GetWindowText(HWND hWnd, StringBuilder lpString, int nMaxCount) =>
            User32.GetWindowText(hWnd, lpString, nMaxCount);

        internal static int GetWindowTextLength(HWND hWnd) => User32.GetWindowTextLength(hWnd);

        internal static HWND GetForegroundWindow() => User32.GetForegroundWindow();
        internal static bool RegisterHotKey(HWND hWnd, int id, int fsModifiers, int vlc)
            => User32.RegisterHotKey(hWnd, id, (User32.HotKeyModifiers) fsModifiers, (uint) vlc);

        internal static bool UnregisterHotKey(HWND hWnd, int id) => User32.UnregisterHotKey(hWnd, id);

        [DllImport("user32.dll")]
        internal static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref int pvParam, uint fWinIni);

        [DllImport("user32.dll")]
        internal static extern bool SystemParametersInfo(uint uiAction, uint uiParam, uint pvParam, uint fWinIni);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SystemParametersInfo(uint uiAction, uint uiParam, bool pvParam, uint fWinIni);

        internal static bool RedrawWindow(HWND hWnd, PRECT lpRectUpdate, IntPtr hrgnUpdate, User32.RedrawWindowFlags flags)
            => User32.RedrawWindow(hWnd, lpRectUpdate, hrgnUpdate, flags);

        internal static uint GetDpiForWindow(HWND hWnd) => User32.GetDpiForWindow(hWnd);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmIsCompositionEnabled(out bool enabled);

        [DllImport("dwmapi.dll", EntryPoint = "#127", PreserveSig = false)]
        internal static extern void DwmGetColorizationParameters(out DWM_COLORIZATION_PARAMS parameters);

        [DllImport("dwmapi.dll", EntryPoint = "#131", PreserveSig = false)]
        internal static extern void DwmSetColorizationParameters(ref DWM_COLORIZATION_PARAMS parameters, bool unknown);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern void DwmEnableComposition(bool bEnable);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern void DwmGetColorizationColor(out uint ColorizationColor, [MarshalAs(UnmanagedType.Bool)]out bool ColorizationOpaqueBlend);

        [DllImport("dwmapi.dll", EntryPoint = "#104")]
        public static extern int DwmpSetColorization(UInt32 ColorizationColor, bool ColorizationOpaqueBlend, UInt32 Opacity);


    }
}