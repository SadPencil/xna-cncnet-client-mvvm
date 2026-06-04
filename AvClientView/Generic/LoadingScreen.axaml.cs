using System;
using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Threading;

using AvClientMvvmContract.Generic;

using AvClientView.Controls;
using AvClientView.Services;

namespace AvClientView.Generic;

public partial class LoadingScreen : UserControl
{
    private bool _iniLayoutApplied;

    public event EventHandler? Completed;

    public LoadingScreen(IIniLayoutOverlayService iniOverlay)
    {
        IniOverlayService = iniOverlay;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    internal IIniLayoutOverlayService IniOverlayService { get; set; }

    internal void TryApplyIniOverlay()
    {
        BackgroundHelper.ApplyDefaultBackground(this, "loadingscreen.png", IniOverlayService);

        IniOverlayService?.ApplyLayout(this, "LoadingScreen", effectiveWidth: ViewConstants.DesignResolutionWidth, effectiveHeight: ViewConstants.DesignResolutionHeight);

        Width = double.NaN;
        Height = double.NaN;

        _iniLayoutApplied = true;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        TryApplyIniOverlay();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ILoadingScreenViewModel.IsLoading))
        {
            var loadingScreenVM = (ILoadingScreenViewModel)sender!;
            if (!loadingScreenVM.IsLoading)
            {
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

                if (!_iniLayoutApplied)
                    TryApplyIniOverlay();
            }
        }
    }
}
