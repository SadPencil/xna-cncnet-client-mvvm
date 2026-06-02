using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;

using AvClientView.Generic;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientView;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Emit a log through Avalonia's own logger at Warning level so it
        // appears on stderr via LogToTrace() → ConsoleTraceListener.
        Logger.TryGet(LogEventLevel.Warning, "AvClient")?.Log(this, "Avalonia diagnostics service active.");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            mainWindow.ShowMainWindow();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
