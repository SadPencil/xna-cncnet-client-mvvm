using System;
using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using AvClientMvvmContract.Generic;

using AvClientView.Services;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientView.Generic;

public partial class LoadingScreen : UserControl
{
    public event EventHandler Completed;

    private bool backgroundApplied;

    public LoadingScreen()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        TryApplyIniOverlay();
    }

    private void TryApplyIniOverlay()
    {
        ApplyDefaultBackground("loadingscreen.png");

        var iniOverlay = ViewConstants.ServiceProvider?.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "LoadingScreen");

        // Keep the logical design size. LoadingScreen sits inside a Viewbox so
        // changing its size would cause everything to scale.
        Width = 800;
        Height = 600;
    }

    private void ApplyDefaultBackground(string texturePath)
    {
        try
        {
            var iniOverlay = ViewConstants.ServiceProvider?.GetService<IIniLayoutOverlayService>();
            if (iniOverlay == null) return;
            var fullPath = iniOverlay.FindTextureFile(texturePath);
            if (fullPath != null)
            {
                var bitmap = new Bitmap(fullPath);
                Background = new ImageBrush { Source = bitmap, Stretch = Stretch.UniformToFill };
                backgroundApplied = true;
            }
        }
        catch { }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ILoadingScreenViewModel.IsLoading))
        {
            var loadingScreenVM = (ILoadingScreenViewModel)sender!;
            if (!loadingScreenVM.IsLoading)
            {
                // TODO: should I wrap it in UIThread?
                Dispatcher.UIThread.Post(() => Completed?.Invoke(this, EventArgs.Empty));
            }
        }
    }

    public ILoadingScreenViewModel? ViewModel
    {
        get => field;
        set
        {
            if (field != null)
                field.PropertyChanged -= OnViewModelPropertyChanged;

            field = value;
            DataContext = value;

            if (field != null)
            {
                field.PropertyChanged += OnViewModelPropertyChanged;

                // ServiceProvider is now available — apply INI overlay
                // if it wasn't applied in OnLoaded (when SP was null).
                if (!backgroundApplied)
                    TryApplyIniOverlay();
            }
        }
    }
}