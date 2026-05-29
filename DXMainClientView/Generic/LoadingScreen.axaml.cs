using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using DXMainClientView.Services;
using DXMainClientViewModel.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView.Generic;

public partial class LoadingScreen : Window, ILoadingScreenView
{
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

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var iniOverlay = App.ServiceProvider?.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, nameof(LoadingScreen));
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ILoadingScreenViewModel.IsLoading)
            && ViewModel is { IsLoading: false })
        {
            Dispatcher.UIThread.Post(TransitionToMainMenu);
        }
    }

    private void TransitionToMainMenu()
    {
        var provider = App.ServiceProvider;
        if (provider == null)
            return;

        var mainMenuVM = provider.GetRequiredService<IMainMenuViewModel>();
        var topBarVM = provider.GetRequiredService<ITopBarViewModel>();

        var mainMenu = new MainMenu();
        mainMenu.ViewModel = mainMenuVM;
        mainMenu.SetTopBarViewModel(topBarVM);

        if (Application.Current?.ApplicationLifetime is
            IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = mainMenu;
        }

        mainMenu.Show();
        Close();
    }

    void ILoadingScreenView.Show()
    {
        this.Show();
    }

    void ILoadingScreenView.Hide()
    {
        Close();
    }
}
