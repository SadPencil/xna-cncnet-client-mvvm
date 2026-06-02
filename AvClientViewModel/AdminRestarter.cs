#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;

using ClientCore;

using Rampastring.Tools;

using Serilog;

namespace AvClientViewModel
{
    /// <summary>
    /// Utility for restarting the client with administrator privileges.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class AdminRestarter
    {
        /// <summary>
        /// Checks if the application is running with administrator privileges.
        /// </summary>
        /// <returns>True if running as administrator, false otherwise.</returns>
        public static bool IsRunningAsAdministrator()
        {
            try
            {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Restarts the current application with administrator privileges.
        /// </summary>
        /// <returns>True if the restart was initiated successfully, false otherwise.</returns>
        public static bool RestartAsAdmin()
        {
            bool runNativeWindowsExe = true;
#if !NETFRAMEWORK
            runNativeWindowsExe = false;
#endif

            try
            {
                if (runNativeWindowsExe)
                {
                    using var _ = Process.Start(new ProcessStartInfo
                    {
                        FileName = SafePath.CombineFilePath(ProgramConstants.StartupExecutable),
                        Verb = "runas",
                        UseShellExecute = true,
                    });
                }
                else
                {
                    // Calling dotnet.exe has a disadvantage:
                    // We need to specify `UseShellExecute = true` for the `Runas` verb, which means we cannot hide the console window despite setting `CreateNoWindow = true`.

                    // Therefore, we calls the launcher exe with the argument of current platform. This makes the client tightly coupled with the launcher, which is not ideal but acceptable for now.

                    string arguments = "-NET8 -Av";

                    using var _ = Process.Start(new ProcessStartInfo
                    {
                        FileName = SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.LauncherExe),
                        Verb = "runas",
                        Arguments = arguments,
                        UseShellExecute = true,
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Information("Failed to restart with admin privileges: " + ex.ToString());
                return false;
            }
        }
    }
}
