using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

using AvClientViewModel.Online;

using ClientCore;
using ClientCore.Settings;

using Rampastring.Tools;

using Serilog;

namespace AvClientViewModel.Services;

public class PreStartupSystemService : IPreStartupSystemService
{
    public void StartSystemSpecificationsCheck()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        // The query in CheckSystemSpecifications takes lots of time,
        // so we'll do it in a separate thread to make startup faster
        Thread checkSpecsThread = new Thread(CheckSystemSpecifications);
        checkSpecsThread.Start();
    }

    public void StartOnlineIdGeneration()
    {
        // Using tasks here causes crashes on Wine for some reason
        Thread onlineIdThread = new Thread(GenerateOnlineId);
        onlineIdThread.Start();
    }

    public void WriteInstallPathToRegistryIfNeeded()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            WriteInstallPathToRegistry();
    }

    /// <summary>
    /// Writes processor, graphics card and memory info to the log file.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static void CheckSystemSpecifications()
    {
        string cpu = string.Empty;
        string videoController = string.Empty;
        string memory = string.Empty;

        System.Management.ManagementObjectSearcher searcher;

        try
        {
            searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_Processor");

            foreach (var proc in searcher.Get())
            {
                cpu = cpu + proc["Name"].ToString().Trim() + " (" + proc["NumberOfCores"] + " cores) ";
            }
        }
        catch
        {
            cpu = "CPU info not found";
        }

        try
        {
            searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

            foreach (System.Management.ManagementObject mo in searcher.Get())
            {
                var currentBitsPerPixel = mo.Properties["CurrentBitsPerPixel"];
                var description = mo.Properties["Description"];
                if (currentBitsPerPixel != null && description != null)
                {
                    if (currentBitsPerPixel.Value != null)
                        videoController = videoController + "Video controller: " + description.Value.ToString().Trim() + " ";
                }
            }
        }
        catch
        {
            cpu = "Video controller info not found";
        }

        try
        {
            searcher = new System.Management.ManagementObjectSearcher("Select * From Win32_PhysicalMemory");
            ulong total = 0;

            foreach (System.Management.ManagementObject ram in searcher.Get())
            {
                total += Convert.ToUInt64(ram.GetPropertyValue("Capacity"));
            }

            if (total != 0)
                memory = "Total physical memory: " + (total >= 1073741824 ? total / 1073741824 + "GB" : total / 1048576 + "MB");
        }
        catch
        {
            cpu = "Memory info not found";
        }

        Log.Information(string.Format("Hardware info: {0} | {1} | {2}", cpu.Trim(), videoController.Trim(), memory));
    }

    /// <summary>
    /// Generate an ID for online play.
    /// </summary>
    private static void GenerateOnlineId()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                System.Management.ManagementObjectCollection mbsList = null;
                System.Management.ManagementObjectSearcher mbs = new System.Management.ManagementObjectSearcher("Select * From Win32_processor");
                mbsList = mbs.Get();
                string cpuid = "";

                foreach (System.Management.ManagementObject mo in mbsList)
                    cpuid = mo["ProcessorID"].ToString();

                System.Management.ManagementObjectSearcher mos = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
                var moc = mos.Get();
                string mbid = "";

                foreach (System.Management.ManagementObject mo in moc)
                    mbid = (string)mo["SerialNumber"];

                string sid = new System.Security.Principal.SecurityIdentifier((byte[])new System.DirectoryServices.DirectoryEntry(string.Format("WinNT://{0},Computer", Environment.MachineName)).Children.Cast<System.DirectoryServices.DirectoryEntry>().First().InvokeGet("objectSID"), 0).AccountDomainSid.Value;

                Connection.SetId(cpuid + mbid + sid);
                using Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + ClientConfiguration.Instance.InstallationPathRegKey);
                key.SetValue("Ident", cpuid + mbid + sid);
            }
            catch (Exception)
            {
                Random rn = new Random();

                using Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + ClientConfiguration.Instance.InstallationPathRegKey);
                string str = rn.Next(Int32.MaxValue - 1).ToString();

                try
                {
                    Object o = key.GetValue("Ident");
                    if (o == null)
                        key.SetValue("Ident", str);
                    else
                        str = o.ToString();
                }
                catch { }

                Connection.SetId(str);
            }
        }
        else
        {
            try
            {
                string machineId = File.ReadAllText("/var/lib/dbus/machine-id");

                Connection.SetId(machineId);
            }
            catch (Exception)
            {
                Connection.SetId(new Random().Next(int.MaxValue - 1).ToString());
            }
        }
    }

    /// <summary>
    /// Writes the game installation path to the Windows registry.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static void WriteInstallPathToRegistry()
    {
        if (!UserINISettings.Instance.WritePathToRegistry)
        {
            Log.Information("Skipping writing installation path to the Windows Registry because of INI setting.");
            return;
        }

        Log.Information("Writing installation path to the Windows registry.");

        try
        {
            using Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + ClientConfiguration.Instance.InstallationPathRegKey);
            key.SetValue("InstallPath", ProgramConstants.GamePath);
        }
        catch
        {
            Log.Warning("Failed to write installation path to the Windows registry");
        }
    }
}
