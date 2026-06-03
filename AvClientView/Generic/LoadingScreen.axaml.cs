using System;
using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using AvClientMvvmContract.Generic;

using AvClientView.Services;

namespace AvClientView.Generic;

public partial class LoadingScreen : UserControl
{
    private bool _backgroundApplied;

    public event EventHandler? Completed;

    public LoadingScreen() : this(null) { }

    public LoadingScreen(IIniLayoutOverlayService? iniOverlay)
    {
        IniOverlayService = iniOverlay;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    /// <summary>
    /// Set by MainWindow after DI is ready to re-apply INI layout.
    /// </summary>
    internal IIniLayoutOverlayService? IniOverlayService { get; set; }

    /// <summary>
    /// Called by MainWindow after DI is ready to apply INI layout
    /// if it wasn't applied in OnLoaded.
    /// </summary>
    internal void TryApplyIniOverlay()
    {
        ApplyDefaultBackground("loadingscreen.png");

        IniOverlayService?.ApplyLayout(this, "LoadingScreen", effectiveWidth: 1280, effectiveHeight: 720);

        Width = double.NaN;
        Height = double.NaN;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        TryApplyIniOverlay();
    }

    private void ApplyDefaultBackground(string texturePath)
    {
        try
        {
            if (IniOverlayService == null) return;
            var fullPath = IniOverlayService.FindTextureFile(texturePath);
            if (fullPath != null)
            {
                var bitmap = new Bitmap(fullPath);
                Background = new ImageBrush { Source = bitmap, Stretch = Stretch.UniformToFill };
                _backgroundApplied = true;
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

                if (!_backgroundApplied)
                    TryApplyIniOverlay();
            }
        }
    }
}
