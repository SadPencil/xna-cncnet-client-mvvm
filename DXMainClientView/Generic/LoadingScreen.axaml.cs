using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using Avalonia.Threading;
using DXMainClientView.Services;
using DXMainClientViewModel.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView.Generic;

public partial class LoadingScreen : UserControl
{
    /// <summary>
    /// Raised when loading completes (IsLoading becomes false).
    /// </summary>
    public event EventHandler? Completed;

    public LoadingScreen()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Set default background (matching original: AssetLoader.LoadTexture("loadingscreen.png"))
        ApplyDefaultBackground("loadingscreen.png");

        // Apply INI layout overrides (LoadingScreen.ini + GenericWindow.ini)
        var iniOverlay = App.ServiceProvider?.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "LoadingScreen");
    }

    private void ApplyDefaultBackground(string texturePath)
    {
        try
        {
            var iniOverlay = App.ServiceProvider?.GetService<IIniLayoutOverlayService>();
            if (iniOverlay == null) return;
            var fullPath = iniOverlay.FindTextureFile(texturePath);
            if (fullPath != null)
            {
                var bitmap = new Bitmap(fullPath);
                Background = new ImageBrush { Source = bitmap, Stretch = Stretch.UniformToFill };
            }
        }
        catch { }
    }

    public ILoadingScreenViewModel? ViewModel
    {
        get => DataContext as ILoadingScreenViewModel;
        set
        {
            if (DataContext is ILoadingScreenViewModel old)
                old.PropertyChanged -= OnViewModelPropertyChanged;

            DataContext = value;

            if (value != null)
                value.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ILoadingScreenViewModel.IsLoading))
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (ViewModel is { IsLoading: false })
                    Completed?.Invoke(this, EventArgs.Empty);
            });
        }
    }
}
