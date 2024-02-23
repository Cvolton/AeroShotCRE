/*  AeroShot - Transparent screenshot utility for Windows
    Copyright (C) 2021 Cvolton, starfrost
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
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using Vanara.PInvoke;

namespace AeroShot
{
    public sealed partial class MainForm : Form
    {
        private const uint SPI_GETFONTSMOOTHING = 0x004A;
        private const uint SPI_GETFONTSMOOTHINGTYPE = 0x200A;

        private int hotkeyKey;
        private int hotkeyModifier;
        private bool needsRestart = false;

        private readonly RegistryKey _registryKey;
        Settings _settings = new Settings();

        public MainForm()
        {
            DoubleBuffered = true;
            Icon = new Icon(typeof(MainForm), "icon.ico");
            InitializeComponent();

            Text = string.Format(Text, Application.ProductName, Application.ProductVersion);

            folderTextBox.Text = _settings.folderTextBox;
            clipboardButton.Checked = _settings.clipboardButton;
            diskButton.Checked = _settings.diskButton;
            resizeCheckbox.Checked = _settings.resizeCheckbox;
            windowWidth.Value = _settings.windowWidth;
            windowHeight.Value = _settings.windowHeight;
            canvasSizeCheckbox.Checked = _settings.canvasSizeCheckbox;
            canvasWidth.Value = _settings.canvasWidth;
            canvasHeight.Value = _settings.canvasHeight;
            aeroColorCheckbox.Checked = _settings.aeroColorCheckbox;
            aeroColorHexBox.Text = _settings.aeroColorHexBox;
            mouseCheckbox.Checked = _settings.mouseCheckbox;
            delayCheckbox.Checked = _settings.delayCheckbox;
            delaySeconds.Value = _settings.delaySeconds;
            clearTypeCheckbox.Checked = _settings.clearTypeCheckbox;
            shadowCheckbox.Checked = _settings.shadowCheckbox;

            cropModeRemoveAllButton.Checked = _settings.cropModeRemoveAllButton;
            cropModeKeepCenteredButton.Checked = _settings.cropModeKeepCenteredButton;

            saveActiveDarkCheckbox.Checked = _settings.saveActiveDarkCheckbox;
            saveActiveLightCheckbox.Checked = _settings.saveActiveLightCheckbox;
            saveInactiveDarkCheckbox.Checked = _settings.saveInactiveDarkCheckbox;
            saveInactiveLightCheckbox.Checked = _settings.saveInactiveLightCheckbox;
            saveMaskCheckbox.Checked = _settings.saveMaskCheckbox;
            saveActiveTransparentCheckbox.Checked = _settings.saveActiveTransparentCheckbox;
            saveInactiveTransparentCheckbox.Checked = _settings.saveInactiveTransparentCheckbox;

            if (!GlassAvailable())
            {
                aeroColorCheckbox.Checked = false;
                aeroColorCheckbox.Enabled = false;
            }

            if (!ClearTypeEnabled())
            {
                clearTypeCheckbox.Checked = false;
                clearTypeCheckbox.Enabled = false;
            }

            if (!ShadowEnabled())
            {
                shadowCheckbox.Checked = false;
                shadowCheckbox.Enabled = false;
            }

            resizeGroupBox.Enabled = resizeCheckbox.Checked;
            mouseGroupBox.Enabled = mouseCheckbox.Checked;
            delayGroupBox.Enabled = delayCheckbox.Checked;
            clearTypeGroupBox.Enabled = clearTypeCheckbox.Checked;
            aeroColorGroupBox.Enabled = aeroColorCheckbox.Checked;
            shadowGroupBox.Enabled = shadowCheckbox.Checked;

            hotkeyKey = _settings.hotkeyKey;
            hotkeyModifier = _settings.hotkeyModifier;

            _registryKey = Registry.CurrentUser.CreateSubKey(@"Software\AeroShot");

            UpdateVisibleHotkey();
        }

        private static bool GlassAvailable()
        {
            if ((Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor > 1) || Environment.OSVersion.Version.Major >= 10 || Environment.OSVersion.Version.Major < 6) return false;

            bool aeroEnabled = true;
            try {
                DwmApi.DwmIsCompositionEnabled(out aeroEnabled);
            }
            catch (Exception)
            {
                //This is fallback intended for beta versions of Windows Vista
            }
            return aeroEnabled;
        }

        private static bool ClearTypeEnabled()
        {
            int sv = 0;
            /* Call to systemparametersinfo to get the font smoothing value. */
            WindowsApi.SystemParametersInfo(SPI_GETFONTSMOOTHING, 0, ref sv, 0);

            int stv = 0;
            /* Call to systemparametersinfo to get the font smoothing Type value. */
            WindowsApi.SystemParametersInfo(SPI_GETFONTSMOOTHINGTYPE, 0, ref stv, 0);

            if (sv > 0 && stv == 2) //if smoothing is on, and is set to cleartype
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool ShadowEnabled()
        {
            return System.Windows.SystemParameters.DropShadow;
        }

        private void BrowseButtonClick(object sender, EventArgs e)
        {
            if (folderSelectionDialog.ShowDialog() == DialogResult.OK)
            {
                folderTextBox.Text = folderSelectionDialog.SelectedPath;
            }
        }

        private void AeroColorDisplayClick(object sender, EventArgs e)
        {
            if (aeroColorDialog.ShowDialog() == DialogResult.OK)
            {
                aeroColorDisplay.Color = aeroColorDialog.Color;

                var hex = new StringBuilder(6);
                hex.AppendFormat("{0:X2}", aeroColorDisplay.Color.R);
                hex.AppendFormat("{0:X2}", aeroColorDisplay.Color.G);
                hex.AppendFormat("{0:X2}", aeroColorDisplay.Color.B);
                aeroColorHexBox.Text = hex.ToString();
            }
        }

        private void ResizeCheckboxStateChange(object sender, EventArgs e)
        {
            resizeGroupBox.Enabled = resizeCheckbox.Checked;
        }

        private void AeroColorCheckboxStateChange(object sender, EventArgs e)
        {
            aeroColorGroupBox.Enabled = aeroColorCheckbox.Checked;
        }

        private void MouseCheckboxStateChange(object sender, EventArgs e)
        {
            mouseGroupBox.Enabled = mouseCheckbox.Checked;
        }

        private void DelayCheckboxStateChange(object sender, EventArgs e)
        {
            delayGroupBox.Enabled = delayCheckbox.Checked;
        }

        private void ClearTypeCheckboxStateChange(object sender, EventArgs e)
        {
            clearTypeGroupBox.Enabled = clearTypeCheckbox.Checked;
        }

        private void shadowCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            shadowGroupBox.Enabled = shadowCheckbox.Checked;
        }

        private void ClipboardButtonStateChange(object sender, EventArgs e)
        {
            if (!clipboardButton.Checked)
                return;
            diskButton.Checked = false;
            folderTextBox.Enabled = false;
            browseButton.Enabled = false;
        }

        private void DiskButtonStateChange(object sender, EventArgs e)
        {
            if (!diskButton.Checked)
                return;
            clipboardButton.Checked = false;
            folderTextBox.Enabled = true;
            browseButton.Enabled = true;
        }

        private void AeroColorTextboxTextChange(object sender, EventArgs e)
        {
            var c = new[] {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'a', 'b', 'c', 'd', 'e', 'f'
            };
            foreach (char v in aeroColorHexBox.Text)
            {
                bool b = false;
                foreach (char v1 in c)
                {
                    if (v == v1)
                    {
                        b = true;
                        break;
                    }
                }
                if (!b)
                    aeroColorHexBox.Text = aeroColorHexBox.Text.Replace(v.ToString(), string.Empty);
            }

            aeroColorDisplay.Color = Color.FromArgb(Convert.ToInt32("FF" + aeroColorHexBox.Text, 16));
            aeroColorDialog.Color = Color.FromArgb(Convert.ToInt32("FF" + aeroColorHexBox.Text, 16));
        }

        private void OkButtonClick(object sender, EventArgs e)
        {
            if (clipboardButton.Checked)
                _registryKey.SetValue("LastPath", "*" + folderTextBox.Text);
            else
                _registryKey.SetValue("LastPath", folderTextBox.Text);

            // Save resizing settings in an 8-byte long
            var b = new byte[8];
            b[0] = (byte)(resizeCheckbox.Checked ? 1 : 0);

            b[3] = (byte)((int)windowWidth.Value & 0xff);
            b[2] = (byte)(((int)windowWidth.Value >> 8) & 0xff);
            b[1] = (byte)(((int)windowWidth.Value >> 16) & 0xff);

            b[6] = (byte)((int)windowHeight.Value & 0xff);
            b[5] = (byte)(((int)windowHeight.Value >> 8) & 0xff);
            b[4] = (byte)(((int)windowHeight.Value >> 16) & 0xff);

            long data = BitConverter.ToInt64(b, 0);
            _registryKey.SetValue("WindowSize", data, RegistryValueKind.QWord);

            // Save canvas settings in an 8-byte long
            b = new byte[8];
            b[0] = (byte)(canvasSizeCheckbox.Checked ? 1 : 0);

            b[3] = (byte)((int)canvasWidth.Value & 0xff);
            b[2] = (byte)(((int)canvasWidth.Value >> 8) & 0xff);
            b[1] = (byte)(((int)canvasWidth.Value >> 16) & 0xff);

            b[6] = (byte)((int)canvasHeight.Value & 0xff);
            b[5] = (byte)(((int)canvasHeight.Value >> 8) & 0xff);
            b[4] = (byte)(((int)canvasHeight.Value >> 16) & 0xff);

            data = BitConverter.ToInt64(b, 0);
            _registryKey.SetValue("CanvasSize", data, RegistryValueKind.QWord);

            data = BitConverter.ToInt64(b, 0);
            _registryKey.SetValue("Opaque", data, RegistryValueKind.QWord);

            // Save background color settings in an 8-byte long
            b = new byte[8];
            b[0] = (byte)(aeroColorCheckbox.Checked ? 1 : 0);

            b[1] = aeroColorDialog.Color.R;
            b[2] = aeroColorDialog.Color.G;
            b[3] = aeroColorDialog.Color.B;

            data = BitConverter.ToInt64(b, 0);
            _registryKey.SetValue("AeroColor", data, RegistryValueKind.QWord);

            _registryKey.SetValue("CapturePointer", mouseCheckbox.Checked ? 1 : 0, RegistryValueKind.DWord);

            _registryKey.SetValue("ClearType", clearTypeCheckbox.Checked ? 1 : 0, RegistryValueKind.DWord);

            _registryKey.SetValue("Shadow", shadowCheckbox.Checked ? 1 : 0, RegistryValueKind.DWord);

            _registryKey.SetValue("SaveActiveDark", saveActiveDarkCheckbox.Checked ? 1 : 0, RegistryValueKind.DWord);

            _registryKey.SetValue("SaveActiveLight", saveActiveLightCheckbox.Checked ? 1 : 0, RegistryValueKind.DWord);

            _registryKey.SetValue("SaveInactiveDark", saveInactiveDarkCheckbox.Checked ? 1 : 0, RegistryValueKind.DWord);

            _registryKey.SetValue("SaveInactiveLight", saveInactiveLightCheckbox.Checked ? 1 : 0, RegistryValueKind.DWord);

            _registryKey.SetValue("SaveMask", saveMaskCheckbox.Checked ? 1 : 0, RegistryValueKind.DWord);

            _registryKey.SetValue("SaveActiveTransparent", saveActiveTransparentCheckbox.Checked ? 1 : 0, RegistryValueKind.DWord);

            _registryKey.SetValue("SaveInactiveTransparent", saveInactiveTransparentCheckbox.Checked ? 1 : 0, RegistryValueKind.DWord);

            _registryKey.SetValue("CropMode", cropModeKeepCenteredButton.Checked ? 1 : 0, RegistryValueKind.DWord);

            _registryKey.SetValue("HotkeyKey", hotkeyKey, RegistryValueKind.DWord);

            _registryKey.SetValue("HotkeyModifier", hotkeyModifier, RegistryValueKind.DWord);

            // Save delay settings in an 8-byte long
            b = new byte[8];
            b[0] = (byte)(delayCheckbox.Checked ? 1 : 0);

            b[1] = (byte)(((int)delaySeconds.Value) & 0xff);

            data = BitConverter.ToInt64(b, 0);
            _registryKey.SetValue("Delay", data, RegistryValueKind.QWord);

            if (needsRestart)
                Application.Restart();

            this.Close();
        }

        private void CancelButtonClick(object sender, EventArgs e)
        {
            this.Close();
        }

        private void UpdateVisibleHotkey()
		{
            keyboardShortcutButton.Text = $"{KeyHelpers.modifiers[hotkeyModifier]} + {((Keys)hotkeyKey).ToString()}";
        }

		private void keyboardShortcutButton_Click(object sender, EventArgs e)
		{
            Button sndr = sender as Button;
            sndr.Text = "...";
		}

		private void keyboardShortcutButton_KeyUp(object sender, KeyEventArgs e)
		{
            Button sndr = sender as Button;
            //TODO: can i just detect if its a modifier key?
            if (sndr.Text == "..."
                && !(e.KeyCode == Keys.Alt || e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
                && KeyHelpers.modifierCodes.ContainsKey(e.Modifiers))
			{
                hotkeyModifier = KeyHelpers.modifierCodes[e.Modifiers];
                hotkeyKey = (int)e.KeyCode;
                needsRestart = true;
            }

            UpdateVisibleHotkey();
        }
	}

	public class ColorDisplay : UserControl
    {
        private readonly SolidBrush _border = new SolidBrush(SystemColors.Window);

        private SolidBrush _brush;
        private Color _color = Color.Black;

        public Color Color
        {
            get { return _color; }
            set
            {
                _color = value;
                Invalidate();
                Update();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle rect = e.ClipRectangle;
            rect.X = 1;
            rect.Y = 1;
            rect.Width -= 2;
            rect.Height -= 2;

            if (Enabled)
                _brush = new SolidBrush(_color);
            else
            {
                var grayScale = (byte)(((_color.R * .3) + (_color.G * .59) + (_color.B * .11)));
                _brush = new SolidBrush(ControlPaint.Light(Color.FromArgb(grayScale, grayScale, grayScale)));
            }
            e.Graphics.FillRectangle(_border, e.ClipRectangle);
            e.Graphics.FillRectangle(_brush, rect);
        }
    }
}