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
        // Pass the design space size (1280x720) so deferred properties
        // (DistanceFrom*, FillWidth/FillHeight) calculate positions
        // relative to the actual visible area, not the INI Size.
        iniOverlay?.ApplyLayout(this, "LoadingScreen", effectiveWidth: 1280, effectiveHeight: 720);

        // Reset explicit size so Stretch fills the Grid regardless of
        // the INI Size (e.g. YRResources uses 1920x1080).
        Width = double.NaN;
        Height = double.NaN;
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