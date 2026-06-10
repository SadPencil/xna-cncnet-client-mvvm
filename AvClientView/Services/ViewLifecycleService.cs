using System;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

using AvClientMvvmContract.ViewServices;


namespace AvClientView.Services;

public class ViewLifecycleService : IViewLifecycleService
{
    public bool IsClosing { get; private set; }
    public event EventHandler? Closing;

    public void Shutdown()
    {
        if (IsClosing)
            return;

        IsClosing = true;
        Closing?.Invoke(this, EventArgs.Empty);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}
