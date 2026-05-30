using DXMainClientMVVMContract.Services;
using System;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;


namespace DXMainClientView.Services;

public class ApplicationLifecycleService : IApplicationLifecycleService
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
