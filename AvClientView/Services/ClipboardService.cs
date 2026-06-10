using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

using AvClientMvvmContract.ViewServices;

namespace AvClientView.Services
{
    public class ClipboardService : IClipboardService
    {
        public async void SetTextAsync(string text)
        {
            IClipboard? clipboard = null;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is { } window)
            {
                clipboard = window.Clipboard;
            }

            if (clipboard != null)
                await clipboard.SetTextAsync(text);
        }
    }
}
