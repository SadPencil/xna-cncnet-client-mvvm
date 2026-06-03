using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

using AvClientMvvmContract.Campaign;
using AvClientMvvmContract.Generic;
using AvClientMvvmContract.Generic.OptionPanels;
using AvClientMvvmContract.Multiplayer;
using AvClientMvvmContract.Multiplayer.CnCNet;
using AvClientMvvmContract.Multiplayer.GameLobby;
using AvClientMvvmContract.ViewServices;

using AvClientViewModel.Campaign;
using AvClientViewModel.Domain;
using AvClientViewModel.Domain.Multiplayer;
using AvClientViewModel.Domain.Multiplayer.CnCNet;
using AvClientViewModel.Generic;
using AvClientViewModel.Generic.OptionPanels;
using AvClientViewModel.LAN;
using AvClientViewModel.Multiplayer;
using AvClientViewModel.Multiplayer.CnCNet;
using AvClientViewModel.Multiplayer.GameLobby;
using AvClientViewModel.Online;
using AvClientViewModel.Services;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;
using ClientCore.I18N;
using ClientCore.INIProcessing;
using ClientCore.PlatformShim;
using ClientCore.Settings;

using ClientUpdater;

using Microsoft.Extensions.DependencyInjection;

using Rampastring.Tools;

using Serilog;

using Steamworks;

namespace AvClientViewModel;

/// <summary>
/// Contains client startup parameters.
/// </summary>
public struct StartupParams
{
    public StartupParams(bool noAudio, bool multipleInstanceMode,
        List<string> unknownParams)
    {
        NoAudio = noAudio;
        MultipleInstanceMode = multipleInstanceMode;
        UnknownStartupParams = unknownParams ?? new List<string>();
    }

    public bool NoAudio { get; }
    public bool MultipleInstanceMode { get; }
    public List<string> UnknownStartupParams { get; }
}

/// <summary>
/// Initializes client systems before the UI starts.
/// Replicates the original DXMainClient PreStartup + Startup initialization chain.
/// </summary>
public static class PreStartup
{
    private static readonly Stopwatch startupStopwatch = Stopwatch.StartNew();
    public static TimeSpan StartupElapsed => startupStopwatch.Elapsed;

    /// <summary>
    /// Initializes all non-UI systems.
    /// </summary>
    public static void Initialize(StartupParams parameters = default)
    {
        // --- Culture (same as DXMainClient PreStartup lines 60-61) ---
        Translation.InitialUICulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(ProgramConstants.HARDCODED_LOCALE_CODE);

        IniFile.DisallowDesktopIni = true;

        // --- Exception handling (same as DXMainClient PreStartup lines 65-69) ---
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            HandleException(sender, (Exception)args.ExceptionObject);

        // --- Working directory (same as DXMainClient PreStartup lines 71-73) ---
        DirectoryInfo gameDirectory = SafePath.GetDirectory(ProgramConstants.GamePath);
        Environment.CurrentDirectory = gameDirectory.FullName;

        // --- Check permissions (same as DXMainClient PreStartup lines 75-76) ---
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            CheckPermissions();

        // --- Logger setup (same as DXMainClient PreStartup lines 78-97) ---
        DirectoryInfo clientUserFilesDirectory = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath);
        FileInfo clientLogFile = SafePath.GetFile(clientUserFilesDirectory.FullName, "client.log");
        ProgramConstants.LogFileName = clientLogFile.FullName;

        if (clientLogFile.Exists)
        {
            // Copy client.log file as client_previous.log. Override client_previous.log if it exists.
            FileInfo clientPrevLogFile = SafePath.GetFile(clientUserFilesDirectory.FullName, "client_previous.log");
            if (clientPrevLogFile.Exists)
                File.Delete(clientPrevLogFile.FullName);
            File.Move(clientLogFile.FullName, clientPrevLogFile.FullName);
        }

        if (!clientUserFilesDirectory.Exists)
            clientUserFilesDirectory.Create();

        ClientCore.PreStartup.InitializeLogger(clientUserFilesDirectory.FullName, clientLogFile.Name);
        MainClientConstants.LoggerInitialized = true;

        Log.Information("***Logfile for " + MainClientConstants.GAME_NAME_LONG + " client***");

        // --- Version logging (same as DXMainClient PreStartup lines 100-110) ---
        string clientVersion = GitVersionInformation.AssemblySemVer;
#if DEVELOPMENT_BUILD
        clientVersion = $"{GitVersionInformation.CommitDate} {GitVersionInformation.BranchName}@{GitVersionInformation.ShortSha}";
#endif

        Log.Information($"Client version: {clientVersion}");
        Log.Information(GitVersionInformation.InformationalVersion);

#if DEVELOPMENT_BUILD
        Log.Information("This is a development build of the client. Stability and reliability may not be fully guaranteed.");
#endif

        // --- Client configuration (same as DXMainClient PreStartup line 111) ---
        MainClientConstants.Initialize();

        // --- Startup params logging (same as DXMainClient PreStartup lines 114-125) ---
        if (parameters.NoAudio)
        {
            Log.Information("Startup parameter: No audio");

            // TODO fix
            throw new NotImplementedException("-NOAUDIO is currently not implemented, please run the client without it.".L10N("Client:Main:NoAudio"));
        }

        if (parameters.MultipleInstanceMode)
            Log.Information("Startup parameter: Allow multiple client instances");

        parameters.UnknownStartupParams?.ForEach(p => Log.Information("Unknown startup parameter: " + p));

        Log.Information("Loading settings.");

        // --- Settings initialization (same as DXMainClient PreStartup lines 127-129) ---
        UserINISettings.Initialize(ClientConfiguration.Instance.SettingsIniName);

        // --- Translation loading (same as DXMainClient PreStartup lines 131-164) ---
        try
        {
            Translation translation;
            FileInfo translationThemeFile = SafePath.GetFile(UserINISettings.Instance.TranslationThemeFolderPath, ClientConfiguration.Instance.TranslationIniName);
            FileInfo translationFile = SafePath.GetFile(UserINISettings.Instance.TranslationFolderPath, ClientConfiguration.Instance.TranslationIniName);

            if (translationFile.Exists)
            {
                Log.Information($"Loading generic translation file at {translationFile.FullName}");
                translation = new Translation(translationFile.FullName, UserINISettings.Instance.Translation);
                if (translationThemeFile.Exists)
                {
                    Log.Information($"Loading theme-specific translation file at {translationThemeFile.FullName}");
                    translation.AppendValuesFromIniFile(translationThemeFile.FullName);
                }

                Translation.Instance = translation;
            }

            Log.Information("Loaded translation: " + Translation.Instance.Name);
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to load the translation file. " + ex.ToString());
            Translation.Instance = new Translation(UserINISettings.Instance.Translation);
        }

        CultureInfo.CurrentUICulture = Translation.Instance.Culture;

        // --- Translation stub generation (same as DXMainClient PreStartup lines 166-192) ---
        try
        {
            if (UserINISettings.Instance.GenerateTranslationStub)
            {
                string stubPath = SafePath.CombineFilePath(
                    ProgramConstants.ClientUserFilesPath, ClientConfiguration.Instance.TranslationIniName);

                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    Log.Information("Writing the translation stub file.");
                    var ini = Translation.Instance.DumpIni(UserINISettings.Instance.GenerateOnlyNewValuesInTranslationStub);
                    ini.WriteIniFile(stubPath);
                };

                Log.Information("Translation stub generation feature is now enabled. The stub file will be written when the client exits.");

                // Lookup all compile-time available strings
                ClientCore.Generated.TranslationNotifier.Register();
                ClientUpdater.Generated.TranslationNotifier.Register();
                AvClientViewModel.Generated.TranslationNotifier.Register();
            }
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to generate the translation stub: " + ex.ToString());
        }

        // --- Resource path (same as DXMainClient Startup.Execute lines 37-41) ---
        ProgramConstants.RESOURCES_DIR = SafePath.CombineDirectoryPath(
            ProgramConstants.BASE_RESOURCE_PATH,
            UserINISettings.Instance.ThemeFolderPath);

        DirectoryInfo resourcesDirectory = SafePath.GetDirectory(ProgramConstants.GetResourcePath());
        if (!resourcesDirectory.Exists)
            throw new DirectoryNotFoundException("Theme directory not found!" + Environment.NewLine + ProgramConstants.RESOURCES_DIR);

        Log.Information("Resource path: " + ProgramConstants.GetResourcePath());
        Log.Information("Base resource path: " + ProgramConstants.GetBaseResourcePath());

        // --- Client resolution initialization (same as DXMainClient Startup.Execute lines 133-147) ---
        if (!UserINISettings.Instance.BorderlessWindowedClient)
        {
            var (bestWidth, bestHeight) = GetBestRecommendedResolution();
            UserINISettings.Instance.ClientResolutionX = new IntSetting(UserINISettings.Instance.SettingsIni, UserINISettings.VIDEO, "ClientResolutionX", bestWidth);
            UserINISettings.Instance.ClientResolutionY = new IntSetting(UserINISettings.Instance.SettingsIni, UserINISettings.VIDEO, "ClientResolutionY", bestHeight);
        }
        else
        {
            var (safeWidth, safeHeight) = GetSafeFullScreenResolution();
            UserINISettings.Instance.ClientResolutionX = new IntSetting(UserINISettings.Instance.SettingsIni, UserINISettings.VIDEO, "ClientResolutionX", safeWidth);
            UserINISettings.Instance.ClientResolutionY = new IntSetting(UserINISettings.Instance.SettingsIni, UserINISettings.VIDEO, "ClientResolutionY", safeHeight);
        }

        // --- Refresh settings (same as DXMainClient Startup.Execute line 129) ---
        ClientConfiguration.Instance.RefreshSettings();
    }

    // ===================================================================
    // Exception handling (same as DXMainClient PreStartup lines 239-286)
    // ===================================================================

    public static void LogException(Exception ex, bool innerException = false)
    {
        if (!innerException)
            Log.Error("KABOOOOOOM!!! Info:");
        else
            Log.Error("InnerException info:");

        Log.Error("Type: " + ex.GetType());
        Log.Error("Message: " + ex.Message);
        Log.Error("Source: " + ex.Source);
        Log.Error("TargetSite.Name: " + ex.TargetSite?.Name);
        Log.Error("Stacktrace: " + ex.StackTrace);

        if (ex.InnerException is not null)
            LogException(ex.InnerException, true);
    }

    public static void HandleException(object sender, Exception ex)
    {
        LogException(ex, innerException: false);

        string errorLogPath = SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "ClientCrashLogs", FormattableString.Invariant($"ClientCrashLog{DateTime.Now.ToString("_yyyy_MM_dd_HH_mm")}.txt"));
        bool crashLogCopied = false;

        try
        {
            DirectoryInfo crashLogsDirectoryInfo = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "ClientCrashLogs");

            if (!crashLogsDirectoryInfo.Exists)
                crashLogsDirectoryInfo.Create();

            File.Copy(SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "client.log"), errorLogPath, true);
            crashLogCopied = true;
        }
        catch { }

        string error = string.Format("{0} has crashed. Error message:".L10N("Client:Main:FatalErrorText1") + Environment.NewLine + Environment.NewLine +
            ex.Message + Environment.NewLine + Environment.NewLine + (crashLogCopied ?
            "A crash log has been saved to the following file:".L10N("Client:Main:FatalErrorText2") + " " + Environment.NewLine + Environment.NewLine +
            errorLogPath + Environment.NewLine + Environment.NewLine : "") +
            (crashLogCopied ? "If the issue is repeatable, contact the {1} staff at {2} and provide the crash log file.".L10N("Client:Main:FatalErrorText3") :
            "If the issue is repeatable, contact the {1} staff at {2}.".L10N("Client:Main:FatalErrorText4")),
            MainClientConstants.GAME_NAME_LONG,
            MainClientConstants.GAME_NAME_SHORT,
            MainClientConstants.SUPPORT_URL_SHORT);

        MainClientConstants.DisplayErrorAction("KABOOOOOOOM".L10N("Client:Main:FatalErrorTitle"), error, true);
    }

    // ===================================================================
    // Permissions check (same as DXMainClient PreStartup lines 288-378)
    // ===================================================================

    [SupportedOSPlatform("windows")]
    private static void CheckPermissions()
    {
        if (UserHasDirectoryAccessRights(ProgramConstants.GamePath, FileSystemRights.Modify))
            return;

        string error = string.Format(("You seem to be running {0} from a write-protected directory.\n\n" +
            "For {1} to function properly when run from a write-protected directory, it needs administrative privileges.\n\n" +
            "Please also make sure that your security software isn't blocking {1}.").L10N("Client:Main:AdminRequiredExplanation"),
            MainClientConstants.GAME_NAME_LONG, MainClientConstants.GAME_NAME_SHORT);

        string title = "Administrative privileges required".L10N("Client:Main:AdminRequiredTitle");

        MainClientConstants.DisplayErrorAction(title, error, true);

        Environment.Exit(1);
    }

    /// <summary>
    /// Checks whether the client has specific file system rights to a directory.
    /// See ssds's answer at https://stackoverflow.com/questions/1410127/c-sharp-test-if-user-has-write-access-to-a-folder
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <param name="accessRights">The file system rights.</param>
    [SupportedOSPlatform("windows")]
    private static bool UserHasDirectoryAccessRights(string path, FileSystemRights accessRights)
    {
        var currentUser = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(currentUser);

        // If the user is not running the client with administrator privileges in Program Files, they need to be prompted to do so.
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            string progfiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string progfilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (ProgramConstants.GamePath.Contains(progfiles) || ProgramConstants.GamePath.Contains(progfilesx86))
                return false;
        }

        var isInRoleWithAccess = false;

        try
        {
            var di = new DirectoryInfo(path);
            var acl = di.GetAccessControl();
            var rules = acl.GetAccessRules(true, true, typeof(NTAccount));

            foreach (AuthorizationRule rule in rules)
            {
                var fsAccessRule = rule as FileSystemAccessRule;
                if (fsAccessRule == null)
                    continue;

                if ((fsAccessRule.FileSystemRights & accessRights) > 0)
                {
                    var ntAccount = rule.IdentityReference as NTAccount;
                    if (ntAccount == null)
                        continue;

                    try
                    {
                        if (principal.IsInRole(ntAccount.Value))
                        {
                            if (fsAccessRule.AccessControlType == AccessControlType.Deny)
                                return false;
                            isInRoleWithAccess = true;
                        }
                    }
                    catch (Exception)
                    {
                        //IsInRole may throw for selected roles when running in Wine, keep iterating other rules
                        continue;
                    }
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        return isInRoleWithAccess;
    }

    // ===================================================================
    // Resolution helpers (same as DXMainClient ScreenResolution)
    // ===================================================================

    /// <summary>
    /// Gets the best recommended resolution from ClientConfiguration.
    /// Replicates ScreenResolution.GetBestRecommendedResolution() without XNA dependency.
    /// </summary>
    private static (int Width, int Height) GetBestRecommendedResolution()
    {
        var recommended = ClientConfiguration.Instance.RecommendedResolutions
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .ToList();

        if (recommended.Count > 0)
        {
            string best = recommended[0];
            int bestArea = 0;
            foreach (var res in recommended)
            {
                var parts = res.Split('x');
                if (parts.Length == 2
                    && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
                {
                    int area = w * h;
                    if (area > bestArea)
                    {
                        best = res;
                        bestArea = area;
                    }
                }
            }
            var bestParts = best.Split('x');
            if (bestParts.Length == 2
                && int.TryParse(bestParts[0], out int bw) && int.TryParse(bestParts[1], out int bh))
                return (bw, bh);
        }

        return (1920, 1080);
    }

    /// <summary>
    /// Gets a safe full-screen resolution default.
    /// Replicates ScreenResolution.SafeFullScreenResolution without XNA dependency.
    /// </summary>
    private static (int Width, int Height) GetSafeFullScreenResolution()
    {
        return (3840, 2160);
    }
}
