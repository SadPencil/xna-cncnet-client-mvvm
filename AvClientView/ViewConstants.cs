using System;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientView;

internal static class ViewConstants
{
    // It is an anti-pattern of dependency injection to store the ServiceProvider variable, but we tolerate such a usage for now.
    // Starts as null so the View can show the loading screen before DI is ready.
    // Set by MainWindow after async initialization completes.
    public static ServiceProvider? ServiceProvider { get; set; }

    /// <summary>
    /// Delegate set by AvClientExe that builds the ServiceProvider.
    /// Called on a background thread by MainWindow after the loading screen appears.
    /// </summary>
    public static Func<ServiceProvider>? InitializeServices { get; set; }
}
