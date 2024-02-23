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

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Vanara.PInvoke;

namespace AeroShot
{

    internal struct ScreenshotTask
    {
        public bool CaptureMouse;
        public bool DisableClearType;
        public bool CustomGlass;
        public Color AeroColor;
        public bool ClipboardNotDisk;
        public string DiskSaveDirectory;
        public bool DoResize;
        public int ResizeX;
        public int ResizeY;
        public bool DoCanvas;
        public int CanvasX;
        public int CanvasY;
        public bool DisableShadow;
        public HWND WindowHandle;
        public bool SaveActiveDark;
        public bool SaveActiveLight;
        public bool SaveActiveTransparent;
        public bool SaveInactiveDark;
        public bool SaveInactiveLight;
        public bool SaveInactiveTransparent;
        public bool CropMode;
        public bool SaveMask;

        public ScreenshotTask(HWND window, bool clipboard, string file,
                              bool resize, int resizeX, int resizeY,
                              bool customGlass, Color aeroColor,
                              bool mouse, bool clearType, bool shadow,
                              bool saveActiveDark, bool saveActiveLight, bool saveInactiveDark,
                              bool saveInactiveLight, bool saveMask, bool saveActiveTransparent,
                              bool saveInactiveTransparent,
                              bool canvas, int canvasX, int canvasY,
                              bool cropMode)
        {
            WindowHandle = window;
            ClipboardNotDisk = clipboard;
            DiskSaveDirectory = file;
            DoResize = resize;
            ResizeX = resizeX;
            ResizeY = resizeY;
            DoCanvas = canvas;
            CanvasX = canvasX;
            CanvasY = canvasY;
            CustomGlass = customGlass;
            AeroColor = aeroColor;
            CaptureMouse = mouse;
            DisableClearType = clearType;
            DisableShadow = shadow;
            SaveActiveDark = saveActiveDark;
            SaveActiveLight = saveActiveLight;
            SaveActiveTransparent = saveActiveTransparent;
            SaveInactiveDark = saveInactiveDark;
            SaveInactiveLight = saveInactiveLight;
            SaveInactiveTransparent = saveInactiveTransparent;
            SaveMask = saveMask;
            CropMode = cropMode;
        }
    }

    internal static class Screenshot
    {
        private const uint FE_FONTSMOOTHINGCLEARTYPE = 0x2;
        private const uint FE_FONTSMOOTHINGSTANDARD = 0x1;

        internal static void CaptureWindow(ref ScreenshotTask data)
        {
            var start = User32.FindWindow("Button", "Start");
            var taskbar = User32.FindWindow("Shell_TrayWnd", null);
            if (data.ClipboardNotDisk || Directory.Exists(data.DiskSaveDirectory))
            {
                try
                {
                    // Hide the taskbar, just incase it gets in the way
                    if (data.WindowHandle != start &&
                        data.WindowHandle != taskbar)
                    {
                        User32.ShowWindow(start, 0);
                        User32.ShowWindow(taskbar, 0);
                        Application.DoEvents();
                    }
                    bool ClearTypeToggled = false;
                    if (data.DisableClearType && ClearTypeEnabled())
                    {
                        WindowsApi.SystemParametersInfo((uint) User32.SPI.SPI_SETFONTSMOOTHINGTYPE, 0, FE_FONTSMOOTHINGSTANDARD, (uint) User32.SPIF.SPIF_UPDATEINIFILE | (uint) User32.SPIF.SPIF_SENDCHANGE);
                        User32.RedrawWindow(data.WindowHandle, null, IntPtr.Zero, User32.RedrawWindowFlags.RDW_FRAME | User32.RedrawWindowFlags.RDW_INVALIDATE | User32.RedrawWindowFlags.RDW_UPDATENOW | User32.RedrawWindowFlags.RDW_ALLCHILDREN);
                        ClearTypeToggled = true;
                    }
                    User32.SetForegroundWindow(data.WindowHandle);

					bool AeroColorToggled = false;
                    uint ColorizationColor = 0;
                    bool ColorizationOpaqueBlend = false;

					DwmApi.DWM_COLORIZATION_PARAMS originalParameters = new DwmApi.DWM_COLORIZATION_PARAMS();
					if (Environment.OSVersion.Version.Major >= 6)
					{
                        try
                        {
                            if (data.CustomGlass && AeroEnabled())
                            {
                                if (VersionHelpers.IsWindowsVista())
                                {
                                    DwmApi.DwmGetColorizationColor(out ColorizationColor, out ColorizationOpaqueBlend);
                                    WindowsApi.DwmpSetColorization(ColorToBgra(data.AeroColor), true, 0xFF);
                                }
                                else
                                {
                                    // Original colorization parameters
                                    DwmApi.DwmpGetColorizationParameters(out originalParameters);

                                    // Custom colorization parameters
                                    DwmApi.DWM_COLORIZATION_PARAMS parameters;
                                    DwmApi.DwmpGetColorizationParameters(out parameters);
                                    parameters.clrAfterGlowBalance = UintToQUAD(2);
                                    parameters.clrBlurBalance = UintToQUAD(29);
                                    parameters.clrColor = data.AeroColor;
                                    parameters.nIntensity = 69;

                                    // Call the DwmSetColorizationParameters to make the change take effect.
                                    DwmApi.DwmpSetColorizationParameters(parameters);
                                }
                                AeroColorToggled = true;
                            }
                        }
                        catch (Exception)
                        {
                            AeroColorToggled = false; //workaround for Vista Betas
                        }
					}


					bool ShadowToggled = false;
                    if (data.DisableShadow && ShadowEnabled())
                    {
                        WindowsApi.SystemParametersInfo((uint) User32.SPI.SPI_SETDROPSHADOW, 0, false, 0);
                        ShadowToggled = true;
                    }

                    var r = RECT.Empty;
                    if (data.DoResize)
                    {
                        SmartResizeWindow(ref data, out r);
                        Thread.Sleep(100);
                    }

                    int length = User32.GetWindowTextLength(data.WindowHandle);
                    var sb = new StringBuilder(length + 1);
                    User32.GetWindowText(data.WindowHandle, sb, sb.Capacity);

                    string name = sb.ToString();

                    foreach (char inv in Path.GetInvalidFileNameChars())
                        name = name.Replace(inv.ToString(), string.Empty);

                    Bitmap[] s = CaptureCompositeScreenshot(ref data);
					
                    if (AeroColorToggled && Environment.OSVersion.Version.Major >= 6)
                    {
                        if (VersionHelpers.IsWindowsVista())
                        {
                            WindowsApi.DwmpSetColorization(ColorizationColor, ColorizationOpaqueBlend, 0xFF);
                        }
                        else
                            DwmApi.DwmpSetColorizationParameters(originalParameters);
                    }

                    if (ClearTypeToggled)
                    {
                        WindowsApi.SystemParametersInfo((uint) User32.SPI.SPI_SETFONTSMOOTHINGTYPE, 0, FE_FONTSMOOTHINGCLEARTYPE, (uint) User32.SPIF.SPIF_UPDATEINIFILE | (uint) User32.SPIF.SPIF_SENDCHANGE);
                        User32.RedrawWindow(data.WindowHandle, null, IntPtr.Zero, User32.RedrawWindowFlags.RDW_FRAME | User32.RedrawWindowFlags.RDW_INVALIDATE | User32.RedrawWindowFlags.RDW_UPDATENOW | User32.RedrawWindowFlags.RDW_ALLCHILDREN);
                    }

                    if (ShadowToggled)
                    {
                        WindowsApi.SystemParametersInfo((uint) User32.SPI.SPI_SETDROPSHADOW, 0, true, 0);
                    }

                    // Show the taskbar again
                    if (data.WindowHandle != start &&
                        data.WindowHandle != taskbar)
                    {
                        User32.ShowWindow(start, ShowWindowCommand.SW_NORMAL);
                        User32.ShowWindow(taskbar, ShowWindowCommand.SW_NORMAL);
                    }

                    if (s == null || s[0] == null)
                    {
                        MessageBox.Show("The screenshot taken was blank, it will not be saved.",
                            "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        if (data.ClipboardNotDisk)
                        {
                            var whiteS = new Bitmap(s[0].Width, s[0].Height, PixelFormat.Format24bppRgb);
                            using (Graphics graphics = Graphics.FromImage(whiteS))
                            {
                                graphics.Clear(Color.White);
                                graphics.DrawImage(s[0], 0, 0, s[0].Width, s[0].Height);
                            }
                            using (var pngStream = new MemoryStream())
                            using (var dibStream = new MemoryStream())
                            {
                                // Save screenshot in clipboard as PNG which some applications support (eg. Microsoft Office)
                                s[0].Save(pngStream, ImageFormat.Png);
                                var pngClipboardData = new DataObject("PNG", pngStream);
                                Byte[] dibData = DIBConverter.ConvertToDib(s[0]);
                                dibStream.Write(dibData, 0, dibData.Length);
                                pngClipboardData.SetData(DataFormats.Dib, false, dibStream);
                                //var pngClipboardData = new DataObject(DataFormats.Dib, false, dibStream);
                                // Add fallback for applications that don't support PNG from clipboard (eg. Photoshop or Paint)
                                //pngClipboardData.SetData(DataFormats.Bitmap, s[0]);
                                Clipboard.Clear();
                                Clipboard.SetDataObject(pngClipboardData, true);
                            }
                            whiteS.Dispose();
                        }
                        else
                        {
                            name = name.Trim();
                            if (name == string.Empty)
                                name = "AeroShot";
                            string pathActive = Path.Combine(data.DiskSaveDirectory, name + "_b1.png");
                            string pathInactive = Path.Combine(data.DiskSaveDirectory, name + "_b2.png");
							string pathWhiteActive = Path.Combine(data.DiskSaveDirectory, name + "_w1.png");
							string pathWhiteInactive = Path.Combine(data.DiskSaveDirectory, name + "_w2.png");
							string pathMask = Path.Combine(data.DiskSaveDirectory, name + "_mask.png");
                            string pathTransparentActive = Path.Combine(data.DiskSaveDirectory, name + "_t1.png");
                            string pathTransparentInactive = Path.Combine(data.DiskSaveDirectory, name + "_t2.png");

                            if (File.Exists(pathActive) || File.Exists(pathInactive) || File.Exists(pathWhiteActive) || File.Exists(pathWhiteInactive) || File.Exists(pathMask) || File.Exists(pathTransparentActive) || File.Exists(pathTransparentInactive))
                            {
                                for (int i = 1; i < 9999; i++)
                                {
                                    pathActive = Path.Combine(data.DiskSaveDirectory, name + " " + i + "_b1.png");
                                    pathInactive = Path.Combine(data.DiskSaveDirectory, name + " " + i + "_b2.png");
									pathWhiteActive = Path.Combine(data.DiskSaveDirectory, name + " " + i + "_w1.png");
									pathWhiteInactive = Path.Combine(data.DiskSaveDirectory, name + " " + i + "_w2.png");
									pathMask = Path.Combine(data.DiskSaveDirectory, name + " " + i + "_mask.png");
                                    pathTransparentActive = Path.Combine(data.DiskSaveDirectory, name + " " + i + "_t1.png");
                                    pathTransparentInactive = Path.Combine(data.DiskSaveDirectory, name + " " + i + "_t2.png");
                                    if (!File.Exists(pathActive) && !File.Exists(pathInactive) && !File.Exists(pathWhiteActive) && !File.Exists(pathWhiteInactive) && !File.Exists(pathMask) && !File.Exists(pathTransparentActive) && !File.Exists(pathTransparentInactive))
                                        break;
                                }
                            }
                            if (data.SaveActiveDark)
                                s[0].Save(pathActive, ImageFormat.Png);

                            if (data.SaveInactiveDark)
                                s[1].Save(pathInactive, ImageFormat.Png);

                            if (data.SaveActiveLight)
                                s[2].Save(pathWhiteActive, ImageFormat.Png);

                            if (data.SaveInactiveLight)
                                s[3].Save(pathWhiteInactive, ImageFormat.Png);

                            if (data.SaveMask)
                                s[4].Save(pathMask, ImageFormat.Png);

                            if (data.SaveActiveTransparent)
                                s[5].Save(pathTransparentActive, ImageFormat.Png);

                            if (data.SaveInactiveTransparent)
                                s[6].Save(pathTransparentInactive, ImageFormat.Png);
                        }
                        if (s[0] != null)
                            s[0].Dispose();
                        if (s[1] != null)
                            s[1].Dispose();
                        if (s[2] != null)
                            s[2].Dispose();
                        if (s[3] != null)
                            s[3].Dispose();
                        if (s[4] != null)
                            s[4].Dispose();
                    }

                    if (data.DoResize)
                    {
                        if ((User32.GetWindowLong(data.WindowHandle, User32.WindowLongFlags.GWL_STYLE) & (long) User32.WindowStyles.WS_SIZEBOX) == (long) User32.WindowStyles.WS_SIZEBOX)
                        {
                            HWND hWndInsertAfter = (IntPtr)0;
                            User32.SetWindowPos(data.WindowHandle, hWndInsertAfter, r.X, r.Y, r.Width, r.Height, User32.SetWindowPosFlags.SWP_SHOWWINDOW);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (data.WindowHandle != start && data.WindowHandle != taskbar)
                    {
                        User32.ShowWindow(start, ShowWindowCommand.SW_NORMAL);
                        User32.ShowWindow(taskbar, ShowWindowCommand.SW_NORMAL);
                    }

                    MessageBox.Show("An error occurred while trying to take a screenshot.\r\n\r\nPlease make sure you have selected a valid window.\r\n\r\n" + e.ToString(),
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Invalid directory chosen.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private static bool AeroEnabled()
        {
            bool aeroEnabled = true;
            DwmApi.DwmIsCompositionEnabled(out aeroEnabled);
            return aeroEnabled;
        }

        private static bool ShadowEnabled()
        {
            return System.Windows.SystemParameters.DropShadow;
        }

        // Helper method to convert from a .NET color to a Win32 BGRA-format color.
        private static uint ColorToBgra(Color color)
        {
            return (uint)(color.B | (color.G << 8) | (color.R << 16) | (color.A << 24));
        }

        private static bool ClearTypeEnabled()
        {
            int sv = 0;
            /* Call to systemparametersinfo to get the font smoothing value. */
            WindowsApi.SystemParametersInfo((uint) User32.SPI.SPI_GETFONTSMOOTHING, 0, ref sv, 0);

            int stv = 0;
            /* Call to systemparametersinfo to get the font smoothing Type value. */
            WindowsApi.SystemParametersInfo((uint) User32.SPI.SPI_GETFONTSMOOTHINGTYPE, 0, ref stv, 0);

            if (sv > 0 && stv == 2) //if smoothing is on, and is set to cleartype
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void SmartResizeWindow(ref ScreenshotTask data, out RECT oldWindowSize)
        {
            oldWindowSize = RECT.Empty;
            if ((User32.GetWindowLong(data.WindowHandle, User32.WindowLongFlags.GWL_STYLE) & (long) User32.WindowStyles.WS_SIZEBOX) != (long) User32.WindowStyles.WS_SIZEBOX)
                return;

            User32.GetWindowRect(data.WindowHandle, out var r);
            oldWindowSize = r;

            Bitmap f = CaptureCompositeScreenshot(ref data)[0];
            if (f != null)
            {
                HWND hWndInsertAfter = (IntPtr)0;
                int width = data.ResizeX - (f.Width - (r.Width));
                int height = data.ResizeY - (f.Height - r.Height);
                User32.SetWindowPos(data.WindowHandle, hWndInsertAfter, r.X, r.Y, width, height, User32.SetWindowPosFlags.SWP_SHOWWINDOW);
                f.Dispose();
            }
            else
            {
                HWND hWndInsertAfter = (IntPtr)0;
                User32.SetWindowPos(data.WindowHandle, hWndInsertAfter, r.X, r.Y, data.ResizeX, data.ResizeY, User32.SetWindowPosFlags.SWP_SHOWWINDOW);
            }
        }

        private static void RefreshBackdrop()
        {
            Application.DoEvents();
            Thread.Sleep(33); //Waiting for DWM to refresh, assuming 30 Hz refresh rate to be safe
        }

        private static unsafe Bitmap[] CaptureCompositeScreenshot(ref ScreenshotTask data)
        {
			// Generate a rectangle with the size of all monitors combined
            Rectangle totalSize = Rectangle.Empty;
            foreach (Screen s in Screen.AllScreens)
                totalSize = Rectangle.Union(totalSize, s.Bounds);


            if (DwmApi.DwmGetWindowAttribute(data.WindowHandle, 
                    DwmApi.DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
                    out RECT rct) != 0)
                // DwmGetWindowAttribute() failed, usually means Aero is disabled so we fall back to GetWindowRect()
                User32.GetWindowRect(data.WindowHandle, out rct);
            else
            {
                // DwmGetWindowAttribute() succeeded
                
            }

            // Get DPI of the window (this only works properly on Win 10 1607+) but it is not really needed until Win 11 anyway
            uint DPI = 96;
            try
            {
                DPI = User32.GetDpiForWindow(data.WindowHandle);
            }
            catch { }

            // Adjust margin for DPI
            double scalingFactor = DPI / 96;
            int backdropOffset = Convert.ToInt32(100 * scalingFactor);

            // Add a 100px margin for window shadows. Excess transparency is trimmed out later
            rct.left -= backdropOffset;
            rct.right += backdropOffset;
            rct.top -= backdropOffset;
            rct.bottom += backdropOffset;

            // These next 4 checks handle if the window is outside of the visible screen
            if (rct.left < totalSize.Left)
                rct.left = totalSize.Left;
            if (rct.right > totalSize.Right)
                rct.right = totalSize.Right;
            if (rct.top < totalSize.Top)
                rct.top = totalSize.Top;
            if (rct.bottom > totalSize.Bottom)
                rct.bottom = totalSize.Bottom;

            // Spawning backdrop
            // Handling as much as possible in the constructor makes this easier to render, which makes capture less likely to fail on underpowered PCs
            Color tmpColor = Color.White;
            var backdrop = new Form
            {
                BackColor = tmpColor,
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                //Opacity = 0,
                Size = rct.Size,
                StartPosition = FormStartPosition.Manual,
                Location = rct.Location
            };

            User32.ShowWindow(backdrop.Handle, ShowWindowCommand.SW_SHOWNOACTIVATE);
            if (!User32.SetWindowPos(backdrop.Handle, data.WindowHandle, rct.X, rct.Y, rct.Width, rct.Height, User32.SetWindowPosFlags.SWP_NOACTIVATE))
            {
                // We are unable to put backdrop directly behind the window, so we will put it into the foreground and then put the original window on top of it
                // This likely happens because the program we're trying to capture is running as administrator
                User32.SetWindowPos(backdrop.Handle, IntPtr.Zero, rct.X, rct.Y, rct.Width, rct.Height, User32.SetWindowPosFlags.SWP_NOACTIVATE);
                User32.SetForegroundWindow(data.WindowHandle);
            }

            RefreshBackdrop();
            
            var crop = new Rectangle(rct.X, rct.Y, rct.Width, rct.Height);

            // Capture screenshot with white background
            Bitmap whiteShot = CaptureScreenRegion(crop);

			backdrop.BackColor = Color.Black;
            RefreshBackdrop();

            // Capture screenshot with black background
            Bitmap blackShot = CaptureScreenRegion(crop);

            Bitmap transparentImage;
            Bitmap transparentInactiveImage = null;
            Bitmap transparentWhiteImage = null;
            Bitmap transparentWhiteInactiveImage = null;
            Bitmap transparentMaskImage = null;
            Bitmap transparentTransparentImage = null;
            Bitmap transparentTransparentInactiveImage = null;

            transparentImage = DifferentiateAlpha(whiteShot, blackShot, false);
            if (data.SaveActiveLight) transparentWhiteImage = DifferentiateAlpha(whiteShot, blackShot, true);

            whiteShot.Dispose();
            blackShot.Dispose();

            //Capture black mask screenshot
            if (data.SaveMask)
            {
                int minAlpha = 0;
                bool isCompositing = false;
                bool ShadowToggled = false;
                bool ColorToggled = false;

                try
                {
                    DwmApi.DwmIsCompositionEnabled(out isCompositing);
                }
                catch (Exception)
                {
                    //OS doesn't have a supported version of DWM
                }

                //We can't disable shadows on Vista without disabling DWM, which would cause the mask to be inaccurate
                UInt32 ColorizationColor = 0;
                bool fOpaque = true;
                if (isCompositing && (VersionHelpers.IsWindowsVista() || VersionHelpers.IsWindows11()))
                {
                    minAlpha = 254;

                    DwmApi.DwmGetColorizationColor(out ColorizationColor, out fOpaque);

                    if (fOpaque == false)
                    {
                        WindowsApi.DwmpSetColorization(ColorizationColor, true, 0xFF);
                        ColorToggled = true;
                    }
                    
                }
                else if(ShadowEnabled())
                {
                    WindowsApi.SystemParametersInfo((uint) User32.SPI.SPI_SETDROPSHADOW, 0, false, 0);
                    ShadowToggled = true;
                }

                backdrop.BackColor = Color.White;
                RefreshBackdrop();
                Bitmap whiteMaskShot = CaptureScreenRegion(crop);

                backdrop.BackColor = Color.Black;
                RefreshBackdrop();
                Bitmap blackMaskShot = CaptureScreenRegion(crop);

                transparentMaskImage = CreateMask(DifferentiateAlpha(whiteMaskShot, blackMaskShot, false), minAlpha);

                if (ShadowToggled)
                {
                    WindowsApi.SystemParametersInfo((uint) User32.SPI.SPI_SETDROPSHADOW, 0, true, 0);
                }

                if (ColorToggled)
                {
                    WindowsApi.DwmpSetColorization(ColorizationColor, fOpaque, 0xFF);
                }

                whiteMaskShot.Dispose();
                blackMaskShot.Dispose();

            }

            //Capture active fully transparent
            if (data.SaveActiveTransparent)
            {
                try
                {
                    //win 7
                    DwmApi.DWM_COLORIZATION_PARAMS parameters, originalParameters = new DwmApi.DWM_COLORIZATION_PARAMS();
                    //win vista
                    UInt32 ColorizationColor = 0;
                    bool fOpaque = true;


                    if (VersionHelpers.IsWindowsVista())
                    {

                        DwmApi.DwmGetColorizationColor(out ColorizationColor, out fOpaque);

                        if (fOpaque == false)
                        {
                            WindowsApi.DwmpSetColorization(0xFFFFFF, false, 0xFF);
                        }

                    }
                    else
                    {
                        DwmApi.DwmpGetColorizationParameters(out parameters);
                        DwmApi.DwmpGetColorizationParameters(out originalParameters);

                        //Set custom fully transparent parameters
                        parameters.clrAfterGlowBalance = UintToQUAD(0);
                        parameters.clrBlurBalance = UintToQUAD(100);
                        parameters.nIntensity = 0;

                        // Call the DwmSetColorizationParameters to make the change take effect.
                        DwmApi.DwmpSetColorizationParameters(parameters);
                    }
                    

                    backdrop.BackColor = Color.White;
                    RefreshBackdrop();
                    Bitmap whiteTransparentShot = CaptureScreenRegion(crop);

                    backdrop.BackColor = Color.Black;
                    RefreshBackdrop();
                    Bitmap blackTransparentShot = CaptureScreenRegion(crop);

                    transparentTransparentImage = DifferentiateAlpha(whiteTransparentShot, blackTransparentShot, false);
                    whiteTransparentShot.Dispose();
                    blackTransparentShot.Dispose();

                    if (VersionHelpers.IsWindowsVista())
                    {
                        WindowsApi.DwmpSetColorization(ColorizationColor, fOpaque, 0xFF);
                    }
                    else
                    {
                        DwmApi.DwmpSetColorizationParameters(originalParameters);
                    }
                }
                catch (Exception)
                {
                    transparentTransparentImage = new Bitmap(transparentImage);
                }
            }

            //Show form to steal focus
            var emptyForm = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                Opacity = 0,
            };

            User32.ShowWindow(emptyForm.Handle, ShowWindowCommand.SW_SHOW);
            User32.SetWindowPos(emptyForm.Handle, data.WindowHandle, rct.X, rct.Y, rct.Width, rct.Height, 0);
            User32.SetForegroundWindow(emptyForm.Handle);

            // Capture inactive screenshots
            if (data.SaveInactiveDark || data.SaveInactiveLight)
            {
                backdrop.BackColor = Color.White;
                RefreshBackdrop();

                // Capture inactive screenshot with white background
                Bitmap whiteInactiveShot = CaptureScreenRegion(crop);

                backdrop.BackColor = Color.Black;
                RefreshBackdrop();

                // Capture inactive screenshot with black background
                Bitmap blackInactiveShot = CaptureScreenRegion(crop);


                if (data.SaveInactiveDark) transparentInactiveImage = DifferentiateAlpha(whiteInactiveShot, blackInactiveShot, false);
                if (data.SaveInactiveLight) transparentWhiteInactiveImage = DifferentiateAlpha(whiteInactiveShot, blackInactiveShot, true);

                whiteInactiveShot.Dispose();
                blackInactiveShot.Dispose();
            }

            //Capture inactive fully transparent
            if (data.SaveInactiveTransparent)
            {
                try
                {
                    //Get original colorization parameters
                    DwmApi.DWM_COLORIZATION_PARAMS parameters, originalParameters;
                    DwmApi.DwmpGetColorizationParameters(out parameters);
                    DwmApi.DwmpGetColorizationParameters(out originalParameters);

                    //Set custom fully transparent parameters
                    parameters.clrAfterGlowBalance = UintToQUAD(0);
                    parameters.clrBlurBalance = UintToQUAD(100);
                    parameters.nIntensity = 0;

                    // Call the DwmSetColorizationParameters to make the change take effect.
                    DwmApi.DwmpSetColorizationParameters(parameters);

                    backdrop.BackColor = Color.White;
                    RefreshBackdrop();
                    Bitmap whiteTransparentInactiveShot = CaptureScreenRegion(crop);

                    backdrop.BackColor = Color.Black;
                    RefreshBackdrop();
                    Bitmap blackTransparentInactiveShot = CaptureScreenRegion(crop);

                    transparentTransparentInactiveImage = DifferentiateAlpha(whiteTransparentInactiveShot, blackTransparentInactiveShot, false);
                    whiteTransparentInactiveShot.Dispose();
                    blackTransparentInactiveShot.Dispose();

                    DwmApi.DwmpSetColorizationParameters(originalParameters);
                }
                catch (Exception)
                {
                    transparentTransparentInactiveImage = new Bitmap(transparentInactiveImage);
                }
            }

            backdrop.Dispose();
            emptyForm.Dispose();
            
			if (data.CaptureMouse)
                DrawCursorToBitmap(transparentImage, new Point(rct.Left, rct.Top));
			Bitmap[] final = CropEmptyEdges(new[] { transparentImage, transparentInactiveImage, transparentWhiteImage, transparentWhiteInactiveImage, transparentMaskImage, transparentTransparentImage, transparentTransparentInactiveImage }, Color.FromArgb(0, 0, 0, 0), ref data);
            
			// Returns a bitmap with transparency, calculated by differentiating the white and black screenshots
            return final;
        }

        private static void DrawCursorToBitmap(Image windowImage, Point offsetLocation)
        {
            // TODO: Fix mouse offset
            var ci = new User32.CURSORINFO();
            ci.cbSize = (uint) Marshal.SizeOf(ci);
            if (User32.GetCursorInfo(ref ci))
            {
                if (ci.flags == User32.CursorState.CURSOR_SHOWING)
                {
                    var hicon = User32.CopyCursor(ci.hCursor);
                    var cursor = new Cursor(hicon.DangerousGetHandle());

                    var loc = ci.ptScreenPos - (Size) offsetLocation;
                    
                    using (var g = Graphics.FromImage(windowImage))
                    {
                        cursor.Draw(g,new Rectangle(loc, cursor.Size));
                    }
                }
            }
            else
            {
                throw new Exception("Can't get cursor info");
            }
        }

        private static Bitmap CaptureScreenRegion(Rectangle crop)
        {
            var src = Gdi32.CreateDC("DISPLAY");
            var dest = Gdi32.CreateCompatibleDC(src);
            var hbitmap = Gdi32.CreateCompatibleBitmap(src, crop.Width, crop.Height);
            var old = Gdi32.SelectObject(dest, hbitmap);
            Gdi32.BitBlt(dest, 0, 0, crop.Width, crop.Height, src, crop.X, crop.Y,
                Gdi32.RasterOperationMode.SRCCOPY | Gdi32.RasterOperationMode.CAPTUREBLT);
            var bmp = hbitmap.ToBitmap();
            Gdi32.SelectObject(dest, old);
            return bmp.Clone(new Rectangle(Point.Empty, bmp.Size), PixelFormat.Format24bppRgb);
        }

        private static unsafe Bitmap GenerateChecker(int s)
        {
            var b1 = new Bitmap(s * 2, s * 2, PixelFormat.Format24bppRgb);
            var b = new UnsafeBitmap(b1);
            b.LockImage();
            PixelData* pixel;
            for (int x = 0, y = 0; x < s * 2 && y < s * 2;)
            {
                pixel = b.GetPixel(x, y);
                if ((x >= 0 && x <= s - 1) && (y >= 0 && y <= s - 1))
                    pixel->SetAll(255);
                if ((x >= s && x <= s * 2 - 1) && (y >= 0 && y <= s - 1))
                    pixel->SetAll(200);
                if ((x >= 0 && x <= s - 1) && (y >= s && y <= s * 2 - 1))
                    pixel->SetAll(200);
                if ((x >= s && x <= s * 2 - 1) && (y >= s && y <= s * 2 - 1))
                    pixel->SetAll(255);
                if (x == s * 2 - 1)
                {
                    y++;
                    x = 0;
                    continue;
                }
                x++;
            }
            b.UnlockImage();
            return b1;
        }

        private static unsafe Bitmap[] CropEmptyEdges(Bitmap[] b1, Color trimColor, ref ScreenshotTask data)
        {
            if (b1 == null)
                return null;

            int sizeX = b1[0].Width;
            int sizeY = b1[0].Height;
            var b = new UnsafeBitmap(b1[0]);
            b.LockImage();

            int left = -1;
            int top = -1;
            int right = -1;
            int bottom = -1;

            PixelData* pixel;

            for (int x = 0, y = 0; ;)
            {
                pixel = b.GetPixel(x, y);
                if (left == -1)
                {
                    if ((trimColor.A == 0 && pixel->Alpha != 0) || (trimColor.R != pixel->Red & trimColor.G != pixel->Green & trimColor.B != pixel->Blue))
                    {
                        left = x;
                        x = 0;
                        y = 0;
                        continue;
                    }
                    if (y == sizeY - 1)
                    {
                        x++;
                        y = 0;
                    }
                    else
                        y++;

                    continue;
                }
                if (top == -1)
                {
                    if ((trimColor.A == 0 && pixel->Alpha != 0) || (trimColor.R != pixel->Red & trimColor.G != pixel->Green & trimColor.B != pixel->Blue))
                    {
                        top = y;
                        x = sizeX - 1;
                        y = 0;
                        continue;
                    }
                    if (x == sizeX - 1)
                    {
                        y++;
                        x = 0;
                    }
                    else
                        x++;

                    continue;
                }
                if (right == -1)
                {
                    if ((trimColor.A == 0 && pixel->Alpha != 0) || (trimColor.R != pixel->Red & trimColor.G != pixel->Green & trimColor.B != pixel->Blue))
                    {
                        right = x + 1;
                        x = 0;
                        y = sizeY - 1;
                        continue;
                    }
                    if (y == sizeY - 1)
                    {
                        x--;
                        y = 0;
                    }
                    else
                        y++;

                    continue;
                }
                if (bottom == -1)
                {
                    if ((trimColor.A == 0 && pixel->Alpha != 0) || (trimColor.R != pixel->Red & trimColor.G != pixel->Green & trimColor.B != pixel->Blue))
                    {
                        bottom = y + 1;
                        break;
                    }
                    if (x == sizeX - 1)
                    {
                        y--;
                        x = 0;
                    }
                    else
                        x++;

                    continue;
                }
            }
            b.UnlockImage();
            if (left >= right || top >= bottom)
                return null;

			int rightSize = right - left;
			int bottomSize = bottom - top;

            if (rightSize >= b1[0].Width || bottomSize >= b1[0].Height)
                MessageBox.Show("Background removal very likely failed, please check the final screenshot after capture.");

            if (data.CropMode) //if crop mode: keep window centered
			{
                if (b1[0].Width - rightSize - left > left)
                    rightSize = b1[0].Width - (2 * left);
				else
                {
                    int oldLeft = left;
                    left = b1[0].Width - rightSize - left;
                    rightSize += (oldLeft - left);
                }

                if (b1[0].Height - bottomSize - top > top)
                    bottomSize = b1[0].Height - (2 * top);
				else
                {
                    int oldTop = top;
                    top = b1[0].Height - bottomSize - top;
                    bottomSize += (oldTop - top);

                }

            }
            
            int canvasRightSize = rightSize % 2 == 0 ? rightSize : rightSize + 1;
            int canvasBottomSize = bottomSize % 2 == 0 ? bottomSize : bottomSize + 1;

            if (data.DoCanvas)
            {
                canvasRightSize = data.CanvasX;
                canvasBottomSize = data.CanvasY;
            }

            Bitmap[] final = new Bitmap[b1.Length];
			for(int i = 0; i < b1.Length; i++)
			{
                if (b1[i] == null)
                    continue;


                final[i] = b1[i].Clone(new Rectangle(left, top, rightSize, bottomSize), b1[i].PixelFormat);
                
                Bitmap temp = new Bitmap(canvasRightSize, canvasBottomSize);
                using (Graphics grD = Graphics.FromImage(temp))
                {
                    grD.DrawImage(final[i], new Rectangle(0, 0, final[i].Width, final[i].Height), new Rectangle(0, 0, final[i].Width, final[i].Height), GraphicsUnit.Pixel);
                }
                final[i].Dispose();
                final[i] = temp;
                
				b1[i].Dispose();
			}
			
            return final;
        }

        private static unsafe Bitmap DifferentiateAlpha(Bitmap whiteBitmap, Bitmap blackBitmap, bool useWhiteMath)
        {
            if (whiteBitmap == null || blackBitmap == null || whiteBitmap.Width != blackBitmap.Width || whiteBitmap.Height != blackBitmap.Height)
                return null;
            int sizeX = whiteBitmap.Width;
            int sizeY = whiteBitmap.Height;
            var final = new Bitmap(sizeX, sizeY, PixelFormat.Format32bppArgb);
            var a = new UnsafeBitmap(whiteBitmap);
            var b = new UnsafeBitmap(blackBitmap);
            var f = new UnsafeBitmap(final);
            a.LockImage();
            b.LockImage();
            f.LockImage();

            bool empty = true;

            for (int x = 0, y = 0; x < sizeX && y < sizeY;)
            {
                PixelData* pixelA = a.GetPixel(x, y);
                PixelData* pixelB = b.GetPixel(x, y);
                PixelData* pixelF = f.GetPixel(x, y);

                pixelF->Alpha = ToByte((pixelB->Red - pixelA->Red + 255 + pixelB->Green - pixelA->Green + 255 + pixelB->Blue - pixelA->Blue + 255) / 3);
                if (pixelF->Alpha > 0)
                {
					if (useWhiteMath)
					{
						// Following math creates an image optimized to be displayed on a white background
						pixelF->Red = ToByte(255 * (pixelA->Red + pixelF->Alpha - 255) / pixelF->Alpha);
						pixelF->Green = ToByte(255 * (pixelA->Green + pixelF->Alpha - 255) / pixelF->Alpha);
						pixelF->Blue = ToByte(255 * (pixelA->Blue + pixelF->Alpha - 255) / pixelF->Alpha);

					}
					else
					{
						// Following math creates an image optimized to be displayed on a black background
						pixelF->Red = ToByte(255 * pixelB->Red / pixelF->Alpha);
						pixelF->Green = ToByte(255 * pixelB->Green / pixelF->Alpha);
						pixelF->Blue = ToByte(255 * pixelB->Blue / pixelF->Alpha);
					}

					
                }
                if (empty && pixelF->Alpha > 0)
                    empty = false;

                if (x == sizeX - 1)
                {
                    y++;
                    x = 0;
                    continue;
                }
                x++;
            }

            a.UnlockImage();
            b.UnlockImage();
            f.UnlockImage();
            return empty ? null : final;
        }

        private static unsafe Bitmap CreateMask(Bitmap bitmap, int minAlpha)
        {
            if (bitmap == null)
                return null;
            int sizeX = bitmap.Width;
            int sizeY = bitmap.Height;
            var final = new Bitmap(sizeX, sizeY, PixelFormat.Format32bppArgb);
            var a = new UnsafeBitmap(bitmap);
            var f = new UnsafeBitmap(final);
            a.LockImage();
            f.LockImage();

            for (int x = 0, y = 0; x < sizeX && y < sizeY;)
            {
                PixelData* pixelA = a.GetPixel(x, y);
                PixelData* pixelF = f.GetPixel(x, y);

                if(pixelA->Alpha > minAlpha)
                {
                    pixelF->Blue = 0;
                    pixelF->Green = 0;
                    pixelF->Red = 0;
                    pixelF->Alpha = 255;
                }

                if (x == sizeX - 1)
                {
                    y++;
                    x = 0;
                    continue;
                }
                x++;
            }

            a.UnlockImage();
            f.UnlockImage();
            return final;
        }

        private static byte ToByte(int i)
        {
            return (byte)(i > 255 ? 255 : (i < 0 ? 0 : i));
        }

        private static Gdi32.RGBQUAD UintToQUAD(uint i)
        {
            return new Gdi32.RGBQUAD
            {
                rgbBlue = (byte) i,
                rgbGreen = (byte) (i >> 8),
                rgbRed = (byte) (i >> 16),
                rgbReserved = (byte) (i >> 24)
            };
        }
    }


}