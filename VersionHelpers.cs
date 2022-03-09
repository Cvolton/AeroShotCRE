using System;
using System.Diagnostics;
using System.Linq;

namespace AeroShot
{
    public static class VersionHelpers
    {
        public static bool AeroGlassForWin8IsRunning()
        {
            return Process.GetProcessesByName("aerohost").Any();
        }

        public static bool HasAeroAfterglow()
        {
            return (Environment.OSVersion.Version.Build >= 6730
                    && Environment.OSVersion.Version.Build < 8432)
                   || AeroGlassForWin8IsRunning();
        }

        public static bool HasAeroTransparency()
        {
            return (Environment.OSVersion.Version.Major == 6
                    && Environment.OSVersion.Version.Build > 5001 // pre-reset is hell, 500x does't have Aero
                    && Environment.OSVersion.Version.Build < 8432)
                   || AeroGlassForWin8IsRunning();
        }

        public static bool IsWindowsVista() // 6.0.5048-6.0.6469
            => Environment.OSVersion.Version.Major == 6 &&
               Environment.OSVersion.Version.Minor == 0;

        public static bool IsWindows7() // 6.1.6519-6.2.7850 
            => Environment.OSVersion.Version.Major == 6 &&
               Environment.OSVersion.Version.Minor == 1;

        public static bool IsWindows8() // 6.2.7875-6.3.9299
            => Environment.OSVersion.Version.Major == 6 &&
               Environment.OSVersion.Version.Minor == 2;

        public static bool IsWindows81() // 6.3.9364-6.3.9785
            => Environment.OSVersion.Version.Major == 6 &&
               Environment.OSVersion.Version.Minor == 3;

        public static bool IsWindows10() // 6.4.9821-10.0.21390.2025
            => (Environment.OSVersion.Version.Major == 6 &&
                Environment.OSVersion.Version.Minor == 4)
               || (Environment.OSVersion.Version.Major == 10
                   && Environment.OSVersion.Version.Build < 21900);

        public static bool IsWindows11()
            => Environment.OSVersion.Version.Build >= 21900;
    }
}