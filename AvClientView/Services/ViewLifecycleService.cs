using System;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

using AvClientMvvmContract.ViewServices;


namespace AvClientView.Services;

public class ViewLifecycleService : IViewLifecycleService
{
    public event EventHandler? ApplicationClosing;

    public void Shutdown()
    {
        ApplicationClosing?.Invoke(this, EventArgs.Empty);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
