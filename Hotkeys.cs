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
using System.Threading;
using System.Windows.Forms;
using Vanara.PInvoke;

namespace AeroShot
{
    public class Hotkeys : Form
    {
        private const int WM_HOTKEY = 0x0312;
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private readonly int[] _windowId;
        private Thread _worker;
        private bool _busyCapturing;
        Settings _settings;

        public Hotkeys()
        {
            _settings = new Settings();
            _windowId = new[] { GetHashCode(), GetHashCode() ^ 327 };
            User32.RegisterHotKey(Handle, _windowId[0], (User32.HotKeyModifiers) _settings.hotkeyModifier, (uint) _settings.hotkeyKey);
            //WindowsApi.RegisterHotKey(Handle, _windowId[1], MOD_ALT | MOD_CONTROL, (int)Keys.PrintScreen);

			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormClose);
        }

        private void FormClose(object sender, FormClosingEventArgs e)
        {
            foreach (var id in _windowId)
            {
                User32.UnregisterHotKey(Handle, id);
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_HOTKEY)
            {
                _settings = new Settings();

                if (_busyCapturing)
                    return;
                ScreenshotTask info = GetParamteresFromUI();
                var CtrlAlt = (m.LParam.ToInt32() & (MOD_ALT | MOD_CONTROL)) == (MOD_ALT | MOD_CONTROL);
                _busyCapturing = true;
                _worker = new Thread(() =>
                {
                    if (CtrlAlt)
                        Thread.Sleep(TimeSpan.FromSeconds(3));
                    else if (_settings.delayCheckbox)
                        Thread.Sleep(TimeSpan.FromSeconds(_settings.delaySeconds));
                    try
                    {
                        Screenshot.CaptureWindow(ref info);
                    }
                    finally
                    {
                        _busyCapturing = false;
                    }
                })
                {
                    IsBackground = true
                };
                _worker.SetApartmentState(ApartmentState.STA);
                _worker.Start();
            }
        }

        private ScreenshotTask GetParamteresFromUI()
        {
            return
                new ScreenshotTask(
                    User32.GetForegroundWindow(),
                    _settings.clipboardButton,
                    _settings.folderTextBox,
                    _settings.resizeCheckbox,
                    _settings.windowWidth,
                    _settings.windowHeight,
                    _settings.aeroColorCheckbox,
                    Color.FromArgb(Convert.ToInt32("FF" + _settings.aeroColorHexBox, 16)),
                    _settings.mouseCheckbox,
                    _settings.clearTypeCheckbox,
                    _settings.shadowCheckbox,
                    _settings.saveActiveDarkCheckbox,
                    _settings.saveActiveLightCheckbox,
                    _settings.saveInactiveDarkCheckbox,
                    _settings.saveInactiveLightCheckbox,
                    _settings.saveMaskCheckbox,
                    _settings.saveActiveTransparentCheckbox,
                    _settings.saveInactiveTransparentCheckbox,
                    _settings.canvasSizeCheckbox,
                    _settings.canvasWidth,
                    _settings.canvasHeight,
                    _settings.cropModeKeepCenteredButton);
        }
    }
}