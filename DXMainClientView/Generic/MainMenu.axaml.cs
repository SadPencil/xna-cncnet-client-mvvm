using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using DXMainClientView.Services;
using DXMainClientViewModel.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView.Generic;

public partial class MainMenu : Window, IMainMenuView
{
    private IMainMenuViewModel? _viewModel;
    private ITopBarViewModel? _topBarViewModel;

    public MainMenu()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the TopBar's ViewModel. Must be called before the window is shown.
    /// </summary>
    public void SetTopBarViewModel(ITopBarViewModel topBarViewModel)
    {
        _topBarViewModel = topBarViewModel;
        topBar.ViewModel = topBarViewModel;

        if (topBarViewModel != null)
        {
            topBarViewModel.PropertyChanged += OnTopBarPropertyChanged;
            UpdatePlayerCountVisibility(topBarViewModel.IsPlayerCountVisible);
        }
    }

    public IMainMenuViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            _viewModel = value;
            DataContext = value;

            if (_viewModel != null)
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Apply INI layout overrides for this window
        var iniOverlay = App.ServiceProvider?.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, nameof(MainMenu));
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // No additional property change handling needed - all bindings are in AXAML
    }

    private void OnTopBarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ITopBarViewModel.IsPlayerCountVisible) && _topBarViewModel != null)
        {
            Dispatcher.UIThread.Post(() => UpdatePlayerCountVisibility(_topBarViewModel.IsPlayerCountVisible));
        }
    }

    private void UpdatePlayerCountVisibility(bool isVisible)
    {
        if (lblCnCNetStatus != null)
            lblCnCNetStatus.IsVisible = isVisible;
        if (lblCnCNetPlayerCount != null)
            lblCnCNetPlayerCount.IsVisible = isVisible;
    }

    // ISwitchableView implementation
    // Window.Show() already satisfies ISwitchableView.Show()
    public new void Hide() => Close();

    public string GetDisplayName() => "Main Menu";
}
