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
using System.Text;
using Microsoft.Win32;

namespace AeroShot
{
    public class Settings
    {
        public bool firstRun;
        public int checkerValue = 8;
        public string opaqueColorHexBox;
        public string folderTextBox;
        public bool opaqueCheckbox;
        public byte opaqueType;
        public bool aeroColorCheckbox;
        public string aeroColorHexBox;
        public bool resizeCheckbox;
        public int windowHeight = 640;
        public int windowWidth = 480;
        public bool diskButton;
        public bool clipboardButton;
        public bool mouseCheckbox;
        public bool delayCheckbox;
        public byte delaySeconds = 3;
        public bool clearTypeCheckbox;
        public bool shadowCheckbox;
        public bool saveActiveDarkCheckbox;
        public bool saveActiveLightCheckbox;
        public bool saveInactiveDarkCheckbox;
        public bool saveInactiveLightCheckbox;
        private readonly RegistryKey _registryKey;


        public Settings()
        {
            object value;
            _registryKey = Registry.CurrentUser.CreateSubKey(@"Software\AeroShot");

            if ((value = _registryKey.GetValue("FirstRun")) == null)
            {
                firstRun = true;
            }


            if ((value = _registryKey.GetValue("LastPath")) != null && value.GetType() == (typeof(string)))
            {
                if (((string)value).Substring(0, 1) == "*")
                {
                    folderTextBox = ((string)value).Substring(1);
                    clipboardButton = true;
                }
                else
                {
                    folderTextBox = (string)value;
                    diskButton = true;
                }
            }
            else
            {
                folderTextBox =
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            if ((value = _registryKey.GetValue("WindowSize")) != null && value.GetType() == (typeof(long)))
            {
                var b = new byte[8];
                for (int i = 0; i < 8; i++)
                    b[i] = (byte)(((long)value >> (i * 8)) & 0xff);
                resizeCheckbox = (b[0] & 1) == 1;
                windowWidth = b[1] << 16 | b[2] << 8 | b[3];
                windowHeight = b[4] << 16 | b[5] << 8 | b[6];
            }

            if ((value = _registryKey.GetValue("Opaque")) != null && value.GetType() == (typeof(long)))
            {
                var b = new byte[8];
                for (int i = 0; i < 8; i++)
                    b[i] = (byte)(((long)value >> (i * 8)) & 0xff);
                opaqueCheckbox = (b[0] & 1) == 1;
                if ((b[0] & 2) == 2)
                    opaqueType = 0;
                if ((b[0] & 4) == 4)
                    opaqueType = 1;

                checkerValue = b[1] + 2;

                var hex = new StringBuilder(6);
                hex.AppendFormat("{0:X2}", b[2]);
                hex.AppendFormat("{0:X2}", b[3]);
                hex.AppendFormat("{0:X2}", b[4]);
                opaqueColorHexBox = hex.ToString();
            }
            else
                opaqueType = 0;

            if ((value = _registryKey.GetValue("AeroColor")) != null && value.GetType() == (typeof(long)))
            {
                var b = new byte[8];
                for (int i = 0; i < 8; i++)
                    b[i] = (byte)(((long)value >> (i * 8)) & 0xff);
                aeroColorCheckbox = (b[0] & 1) == 1;

                var hex = new StringBuilder(6);
                hex.AppendFormat("{0:X2}", b[1]);
                hex.AppendFormat("{0:X2}", b[2]);
                hex.AppendFormat("{0:X2}", b[3]);
                aeroColorHexBox = hex.ToString();
            }
            else
                opaqueType = 0;

            if ((value = _registryKey.GetValue("CapturePointer")) != null && value.GetType() == (typeof(int)))
                mouseCheckbox = ((int)value & 1) == 1;

            if ((value = _registryKey.GetValue("ClearType")) != null && value.GetType() == (typeof(int)))
                clearTypeCheckbox = ((int)value & 1) == 1;

            if ((value = _registryKey.GetValue("Shadow")) != null && value.GetType() == (typeof(int)))
                shadowCheckbox = ((int)value & 1) == 1;

            if ((value = _registryKey.GetValue("SaveActiveDark")) != null && value.GetType() == (typeof(int)))
                saveActiveDarkCheckbox = ((int)value & 1) == 1;

            if ((value = _registryKey.GetValue("SaveActiveLight")) != null && value.GetType() == (typeof(int)))
                saveActiveLightCheckbox = ((int)value & 1) == 1;

            if ((value = _registryKey.GetValue("SaveInactiveDark")) != null && value.GetType() == (typeof(int)))
                saveInactiveDarkCheckbox = ((int)value & 1) == 1;

            if ((value = _registryKey.GetValue("SaveInactiveLight")) != null && value.GetType() == (typeof(int)))
                saveInactiveLightCheckbox = ((int)value & 1) == 1;

            if ((value = _registryKey.GetValue("Delay")) != null && value.GetType() == (typeof(long)))
            {
                var b = new byte[8];
                for (int i = 0; i < 8; i++)
                    b[i] = (byte)(((long)value >> (i * 8)) & 0xff);
                delayCheckbox = (b[0] & 1) == 1;
                delaySeconds = b[1];
            }
        }
    }
}