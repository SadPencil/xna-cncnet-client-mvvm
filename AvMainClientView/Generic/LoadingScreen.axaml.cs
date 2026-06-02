using System;
using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using AvMainClientMvvmContract.Generic;

using AvMainClientView.Services;

using Microsoft.Extensions.DependencyInjection;

namespace AvMainClientView.Generic;

public partial class LoadingScreen : UserControl
{
    public event EventHandler Completed;

    public LoadingScreen()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ApplyDefaultBackground("loadingscreen.png");

        var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "LoadingScreen");
    }

    private void ApplyDefaultBackground(string texturePath)
    {
        try
        {
            var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
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
                field.PropertyChanged += OnViewModelPropertyChanged;
        }
    }
}