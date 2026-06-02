using System;
using System.Diagnostics;
using System.IO;

using Serilog;
using Serilog.Core;
using Serilog.Events;

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

        string logFilePath = Path.Combine(logDirectory, logFileName);

        var loggingLevelSwitch = new LoggingLevelSwitch()
        {
#if DEBUG
            MinimumLevel = LogEventLevel.Debug
#else
            MinimumLevel = LogEventLevel.Information
#endif
        };

        string outputTemplate = "{Timestamp:dd.MM. HH:mm:ss.fff}    {Message:lj}{NewLine}{Exception}";

        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(loggingLevelSwitch)
            .WriteTo.Console(outputTemplate: outputTemplate)
            .WriteTo.Trace(outputTemplate: outputTemplate)
            .WriteTo.File(
                path: logFilePath,
                outputTemplate: outputTemplate,
                fileSizeLimitBytes: null,
                shared: false,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();

        // Ensure logs are flushed when the process exits
        AppDomain.CurrentDomain.ProcessExit += (_, _) => ShutdownLogger();
        AppDomain.CurrentDomain.UnhandledException += (_, _) => ShutdownLogger();
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
