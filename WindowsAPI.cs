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
    internal static class WindowsApi
    {
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

        internal static bool RedrawWindow(HWND hWnd, PRECT lpRectUpdate, IntPtr hrgnUpdate,
            User32.RedrawWindowFlags flags)
            => User32.RedrawWindow(hWnd, lpRectUpdate, hrgnUpdate, flags);

        internal static uint GetDpiForWindow(HWND hWnd) => User32.GetDpiForWindow(hWnd);

        internal static HRESULT DwmIsCompositionEnabled(out bool enabled)
            => DwmApi.DwmIsCompositionEnabled(out enabled);

        internal static void DwmGetColorizationParameters(out DwmApi.DWM_COLORIZATION_PARAMS parameters)
            => DwmApi.DwmpGetColorizationParameters(out parameters);

        internal static void DwmSetColorizationParameters(ref DwmApi.DWM_COLORIZATION_PARAMS parameters)
            => DwmApi.DwmpSetColorizationParameters(parameters);

        public static void DwmGetColorizationColor(out uint ColorizationColor, out bool ColorizationOpaqueBlend)
            => DwmApi.DwmGetColorizationColor(out ColorizationColor, out ColorizationOpaqueBlend);

        [DllImport("dwmapi.dll", EntryPoint = "#104")] // Only for Windows Vista
        public static extern int DwmpSetColorization(UInt32 ColorizationColor, bool ColorizationOpaqueBlend, UInt32 Opacity);
    }
}