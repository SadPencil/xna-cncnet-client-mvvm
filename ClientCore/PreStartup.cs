using System;
using System.Diagnostics;
using System.IO;

using Serilog;
using SerilogTraceListener;

namespace ClientCore;

public static class PreStartup
{
    private static bool _isInitialized;

    /// <summary>
    /// Initializes Serilog with console and file sinks.
    /// Call this once at application startup before any logging.
    /// </summary>
    public static void InitializeLogger(string logDirectory, string logFileName)
    {
        if (_isInitialized)
            return;
        _isInitialized = true;

        // Add SerilogTraceListener to capture System.Diagnostics.Trace calls
        _ = Trace.Listeners.Add(new SerilogTraceListener.SerilogTraceListener());

        string logFilePath = Path.Combine(logDirectory, logFileName);

        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(SerilogHelper.LoggingLevelSwitch)
            .WriteTo.Console(outputTemplate: SerilogHelper.OutputTemplate)
            .WriteTo.File(
                path: logFilePath,
                outputTemplate: SerilogHelper.OutputTemplate,
                fileSizeLimitBytes: null,
                shared: false,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();

        // Ensure logs are flushed when the process exits
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            ShutdownLogger();
        };

        AppDomain.CurrentDomain.UnhandledException += (_, _) =>
        {
            ShutdownLogger();
        };
    }

    /// <summary>
    /// Flush and close the Serilog logger. Call before the application exits.
    /// Safe to call multiple times.
    /// </summary>
    public static void ShutdownLogger()
    {
#if NET8_0_OR_GREATER
        Serilog.Log.CloseAndFlushAsync().GetAwaiter().GetResult();
#else
        Serilog.Log.CloseAndFlush();
#endif
    }
}
