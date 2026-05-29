using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using DXMainClientView.Services;
using DXMainClientViewModel.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView.Generic;

public partial class MainMenu : Window, IMainMenuView
{
    public MainMenu()
    {
        InitializeComponent();
    }

    public IMainMenuViewModel? ViewModel
    {
        get => DataContext as IMainMenuViewModel;
        set => DataContext = value;
    }

    /// <summary>
    /// Sets the TopBar's ViewModel. Must be called before the window is shown.
    /// </summary>
    public void SetTopBarViewModel(ITopBarViewModel topBarViewModel)
    {
        topBar.ViewModel = topBarViewModel;
        topBarViewModel.PropertyChanged += OnTopBarPropertyChanged;
        UpdatePlayerCountVisibility(topBarViewModel.IsPlayerCountVisible);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Apply INI layout overrides for this window
        var iniOverlay = App.ServiceProvider?.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, nameof(MainMenu));
    }

    private void OnTopBarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ITopBarViewModel.IsPlayerCountVisible))
        {
            var topBarVM = (ITopBarViewModel)sender!;
            Dispatcher.UIThread.Post(() => UpdatePlayerCountVisibility(topBarVM.IsPlayerCountVisible));
        }
    }

    private void UpdatePlayerCountVisibility(bool isVisible)
    {
        if (lblCnCNetStatus != null)
            lblCnCNetStatus.IsVisible = isVisible;
        if (lblCnCNetPlayerCount != null)
            lblCnCNetPlayerCount.IsVisible = isVisible;
    }

    // ISwitchableView: Window.Show() satisfies Show(), implement Hide() and GetDisplayName()
    public new void Hide() => Close();
    public string GetDisplayName() => "Main Menu";
}
