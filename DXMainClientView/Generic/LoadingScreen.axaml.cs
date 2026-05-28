using System;
using Avalonia.Controls;
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
        set => DataContext = value;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Apply INI layout overrides for this window
        var iniOverlay = App.ServiceProvider?.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, nameof(LoadingScreen));
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
