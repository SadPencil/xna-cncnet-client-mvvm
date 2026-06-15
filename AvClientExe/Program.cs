using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using AvClientView.Services;

using AvClientViewModel;
using AvClientViewModel.Generic;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientExe;

internal class Program
{
    static Program()
    {
        DirectoryInfo currentDir = new FileInfo(Assembly.GetEntryAssembly()!.Location).Directory;

        string startupPath = SearchResourcesDir(currentDir!.FullName);

#if NETFRAMEWORK
        string binariesFolderName = "Binaries";
#else
        string binariesFolderName = "BinariesNET8";
#endif

        string commonLibraryPath = Path.Combine(startupPath, binariesFolderName) + Path.DirectorySeparatorChar;

#if NETFRAMEWORK
        // Native libs (e.g. libHarfBuzzSharp.dll from HarfBuzzSharp.NativeAssets.Win32)
        // ship under {COMMON_LIBRARY_PATH}/{x64|x86|arm64}/. The .NET Framework runtime
        // does not search those subfolders by default, so without help, P/Invoke
        // calls fail with "Unable to load library 'libHarfBuzzSharp'".
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            static bool areSecureDllLoadingAPIsAvailable()
            {
                var kernel32ModuleHandle = GetModuleHandle("kernel32");
                if (kernel32ModuleHandle == IntPtr.Zero)
                    throw new Exception("Failed to get handle for kernel32.dll. Is your operating system broken?", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()));

                string[] requiredFunctions = ["SetDefaultDllDirectories", "AddDllDirectory", "RemoveDllDirectory"];
                foreach (string function in requiredFunctions)
                {
                    if (GetProcAddress(kernel32ModuleHandle, function) == IntPtr.Zero)
                        return false;
                }

                return true;
            }

            if (!areSecureDllLoadingAPIsAvailable())
                throw new PlatformNotSupportedException("This application requires at least Windows 7 SP1 with KB4457144 (alternatively, KB2533623 or KB3063858) installed.");

            SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_APPLICATION_DIR | LOAD_LIBRARY_SEARCH_USER_DIRS | LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);

            string? archSubfolder = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.X86 => "x86",
                Architecture.Arm64 => "arm64",
                _ => null
            };

            if (archSubfolder is not null)
            {
                static void addDllDirectoryIfExists(string path)
                {
                    if (Directory.Exists(path))
                        AddDllDirectory(path);
                }

                addDllDirectoryIfExists(Path.Combine(commonLibraryPath, archSubfolder));
            }
        }
#endif
    }

#if NETFRAMEWORK
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern bool SetDllDirectory(string lpPathName);

    private const int LOAD_LIBRARY_SEARCH_APPLICATION_DIR = 0x00000200;
    private const int LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400;
    private const int LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;
    private const int LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;

    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern bool SetDefaultDllDirectories(int directoryFlags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr AddDllDirectory(string lpPathName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetModuleHandleW", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr GetModuleHandle([In][MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr GetProcAddress([In] IntPtr hModule, [In][MarshalAs(UnmanagedType.LPStr)] string lpProcName);
#endif

    [STAThread]
    public static void Main(string[] args)
    {
        StartupParams viewModelStartupParams = new(args);

        if (!viewModelStartupParams.MultipleInstanceMode)
        {
            // Single-instance application lock
            // Global prefix means that the mutex is global to the machine
            string mutexId = FormattableString.Invariant($"Global{Guid.Parse("1CC9F8E7-9F69-4BBC-B045-E734204027A9")}");
            using var mutex = new Mutex(false, mutexId, out _);
            bool hasHandle = false;

            try
            {
                try
                {
                    hasHandle = mutex.WaitOne(8000, false);
                    if (hasHandle == false)
                        throw new TimeoutException("Timeout waiting for exclusive access");
                }
                catch (AbandonedMutexException)
                {
                    hasHandle = true;
                }
                catch (TimeoutException)
                {
                    return;
                }

                RunStartupChain(args, viewModelStartupParams);
            }
            finally
            {
                if (hasHandle)
                    mutex.ReleaseMutex();
            }
        }
        else
        {
            RunStartupChain(args, viewModelStartupParams);
        }
    }

    private static void RunStartupChain(string[] args, StartupParams viewModelStartupParams)
    {
        // Note: we have a strict order of operations for startup:
        // 1. AvClientViewModel.PreStartup.Initialize()
        // 2. AvClientView.Startup.Run()
        // 3. AvClientViewModel.Startup.Initialize()
        // 4. AvClientView.Startup.ConfigureServices()
        // 5. AvClientViewModel.Startup.ConfigureServices()
        // 4 and 5 can be in either order.

        AvClientViewModel.PreStartup.Initialize(viewModelStartupParams, new ViewTranslationNotifierService());

        // Create the MainWindowViewModel before the window appears so
        // WindowState (FullScreen from BorderlessWindowedClient) is
        // applied from the first frame. Replaced by the DI-created
        // instance once the service provider is ready.
        var mainWindowViewModel = new MainWindowViewModel(gameInProgressViewModel: null);

        AvClientView.Startup.Run(args, mainWindowViewModel, () =>
        {
            AvClientViewModel.Startup.Initialize(viewModelStartupParams);
            return BuildServiceProvider();
        });
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        AvClientView.Startup.ConfigureServices(services);
        AvClientViewModel.Startup.ConfigureServices(services);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// This method finds the "Resources" directory by traversing the directory tree upwards from the startup path.
    /// </summary>
    /// <remarks>
    /// This method is needed by both ClientCore and DXMainClient. However, since it is usually called at the very beginning,
    /// where we cannot refer to ClientCore, this method is copied to both projects.
    /// Remember to keep <see cref="ClientCore.ProgramConstants.SearchResourcesDir"/> and this method consistent if you have modified its source codes.
    /// </remarks>
    private static string SearchResourcesDir(string startupPath)
    {
        DirectoryInfo currentDir = new(startupPath);
        for (int i = 0; i < 3; i++)
        {
            // Determine if currentDir is the "Resources" folder
            if (currentDir.Name.ToLowerInvariant() == "Resources".ToLowerInvariant())
                return currentDir.FullName;

            // Additional check. This makes developers to debug the client inside Visual Studio a little bit easier.
            DirectoryInfo? resourcesDir = currentDir.GetDirectories("Resources", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (resourcesDir is not null)
                return resourcesDir.FullName;

            currentDir = currentDir.Parent!;
        }

        throw new Exception("Could not find Resources directory.");
    }
}
