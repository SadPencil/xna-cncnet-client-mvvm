using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using DXMainClientViewModel.Generic;

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
