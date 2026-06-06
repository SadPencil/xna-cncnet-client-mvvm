#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using AvClientMvvmContract.ViewServices;

using ClientCore;
using ClientCore.Extensions;

using Microsoft.Win32;

using Rampastring.Tools;

using Serilog;

namespace AvClientViewModel.Domain;

/// <summary>
/// Handles checking and fixing DirectDraw compatibility issues.
/// </summary>
[SupportedOSPlatform("windows")]
public static class DirectDrawCompatibilityChecker
{
    private static readonly IReadOnlyList<string> OSCompatibilityValues = [
        "WIN8RTM", "WIN7RTM", "VISTASP2", "VISTASP1", "VISTARTM", "WINXPSP3", "WINXPSP2", "WIN98", "WIN95"
    ];

    public static IEnumerable<string> GetExecutableFilePathsToCheck()
    {
        List<string> executablePaths = ClientConfiguration.Instance.GetCompatibilityCheckExecutables()
            .Select(executableName => SafePath.CombineFilePath(ProgramConstants.GamePath, executableName))
            .ToList();

        // clientdx.exe, clientogl.exe, or clientxna.exe
        string currentExePath = SafePath.GetFile(ProgramConstants.StartupExecutable).FullName;

        executablePaths.Add(currentExePath);

        Log.Information("Checking compatibility settings for executables: " +
                   string.Join(", ", executablePaths));

        return executablePaths;
    }

    public static void Examine(out bool requireFix, out bool requireAdmin, out IEnumerable<string> problematicExeNames)
    {
        RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);

        using RegistryKey? hkcuKey = hkcu.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers");
        using RegistryKey? hklmKey = hklm.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers");

        static bool IsFixRequired(object? regValue)
            => regValue is string regValueString
               && regValueString.Split([' ']).Intersect(OSCompatibilityValues).Any();

        bool anyHkcuRequireFix = false;
        bool anyHklmRequireFix = false;

        var problematicExeNameHashSet = new HashSet<string>();
        foreach (string exeFullPath in GetExecutableFilePathsToCheck())
        {
            object? hkcuValue = hkcuKey?.GetValue(exeFullPath);
            object? hklmValue = hklmKey?.GetValue(exeFullPath);

            if (IsFixRequired(hkcuValue))
            {
                Log.Information($"Executable '{exeFullPath}' has problematic compatibility settings in HKCU. Value: {hkcuValue}");
                anyHkcuRequireFix = true;
                problematicExeNameHashSet.Add(Path.GetFileName(exeFullPath));
            }

            if (IsFixRequired(hklmValue))
            {
                Log.Information($"Executable '{exeFullPath}' has problematic compatibility settings in HKLM. Value: {hklmValue}");
                anyHklmRequireFix = true;
                problematicExeNameHashSet.Add(Path.GetFileName(exeFullPath));
            }
        }

        requireFix = anyHkcuRequireFix || anyHklmRequireFix;
        requireAdmin = anyHklmRequireFix;
        problematicExeNames = problematicExeNameHashSet;
    }

    public static string FixCompatLayerString(string value) => string.Join(" ",
            value
                .SplitWithCleanup(new[] { ' ' })
                .Where(v => !OSCompatibilityValues.Contains(v, StringComparer.InvariantCultureIgnoreCase)));

    public static void Fix()
    {
        void FixRegValue(object? regValue, out bool success, out string newRegValue)
        {
            if (regValue is string regValueString)
            {
                newRegValue = FixCompatLayerString(regValueString);
                success = true;
            }
            else
            {
                success = false;
                newRegValue = string.Empty;
            }
        }

        void FixRegistryKey(RegistryKey rootKey, string subKeyPath)
        {
            try
            {
                using RegistryKey? key = rootKey.OpenSubKey(subKeyPath, writable: true);
                if (key == null)
                    return;

                foreach (string exeFullPath in GetExecutableFilePathsToCheck())
                {
                    object? value = key.GetValue(exeFullPath);

                    FixRegValue(value, out bool success, out string newValue);

                    if (success)
                    {
                        if (string.IsNullOrEmpty(newValue))
                            key.DeleteValue(exeFullPath, false);
                        else
                            key.SetValue(exeFullPath, newValue, RegistryValueKind.String);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to fix registry key {rootKey.Name}\\{subKeyPath}: {ex.Message}");
            }
        }

        string subKeyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";

        RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);

        FixRegistryKey(hkcu, subKeyPath);

        FixRegistryKey(hklm, subKeyPath);
    }

    /// <summary>
    /// Fixes the __COMPAT_LAYER environment variable for the current process.
    /// </summary>
    public static void FixEnvironmentVariable()
    {
        string compatLayerEnv = Environment.GetEnvironmentVariable("__COMPAT_LAYER") ?? string.Empty;
        string fixedCompatLayerEnv = FixCompatLayerString(compatLayerEnv);
        if (compatLayerEnv != fixedCompatLayerEnv)
        {
            Log.Information("Fixing __COMPAT_LAYER environment variable. Previous value: " +
                       $"'{compatLayerEnv}', new value: '{fixedCompatLayerEnv}'");
            Environment.SetEnvironmentVariable("__COMPAT_LAYER", fixedCompatLayerEnv);
        }
    }

    /// <summary>
    /// Checks for DirectDraw compatibility issues and prompts the user to fix them.
    /// If admin privileges are needed, offers to restart the client with admin rights.
    /// </summary>
    /// <param name="dialogService">Service for showing Yes/No dialogs.</param>
    /// <param name="lifecycleService">Service for shutting down the application after admin restart.</param>
    public static async Task CheckAndPromptFixAsync(
        Services.DialogService dialogService,
        IRestartService restartService)
    {
        // Fix environment variable __COMPAT_LAYER first, for the client itself.
        FixEnvironmentVariable();

        // Now check registry compatibility settings for all relevant executables.
        try
        {
            Examine(out bool requireFix, out bool requireAdmin, out IEnumerable<string> problematicExeNames);

            if (!requireFix)
                return;

            Log.Information("DirectDraw compatibility issue detected.");

            string localizedMessage = "Problematic Windows compatibility mode settings have been detected that may interfere with the game."
                .L10N("Client:Main:ProblematicCompatibilityText1") + "\n\n"
                + "Affected executables:".L10N("Client:Main:ProblematicCompatibilityText2")
                + "\n- " + string.Join("\n- ", problematicExeNames) + "\n\n" +
                "Would you like to remove these compatibility settings now?".L10N("Client:Main:ProblematicCompatibilityText3");

            if (requireAdmin && !AdminRestarter.IsRunningAsAdministrator())
            {
                localizedMessage += "\n\n" + ("Note: Administrator privileges are required to remove compatibility settings." + " " +
                    "Clicking Yes will relaunch the client with administrator permissions.").L10N("Client:Main:ProblematicCompatibilityText4");
            }

            bool yes = await dialogService.ShowYesNoDialog(
                "Problematic Compatibility Settings Detected".L10N("Client:Main:ProblematicCompatibilityTitle"),
                localizedMessage);

            if (!yes)
                return;

            if (requireAdmin && !AdminRestarter.IsRunningAsAdministrator())
            {
                Log.Information("Administrator privileges required. Restart with elevated privileges.");

                restartService.RestartAsAdmin();
                restartService.Shutdown();
            }
            else
            {
                Log.Information("Attempting to fix DirectDraw compatibility settings.");
                Fix();
                Log.Information("DirectDraw compatibility settings fixed successfully.");

                _ = dialogService.ShowOKDialog(
                    "Fix Applied".L10N("Client:Main:CompatibilityFixAppliedTitle"),
                    "The compatibility settings have been removed successfully.\n\n" +
                    "Please note that these settings might be re-applied after a Windows Update, so you may need to use this fix again in the future.".L10N("Client:Main:CompatibilityFixAppliedText"));
            }
        }
        catch (Exception ex)
        {
            Log.Warning("Checking or fixing DirectDraw compatibility failed: " + ex.ToString());
        }
    }
}
