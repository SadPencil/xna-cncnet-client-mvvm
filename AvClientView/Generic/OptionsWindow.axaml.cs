using Avalonia.Controls;

using AvClientMvvmContract.Generic;

using AvClientView.Controls;
using AvClientView.Services;

namespace AvClientView.Generic;

public partial class OptionsWindow : UserControl, IOptionsWindowView
{
    public IIniLayoutOverlayService? IniOverlayService { get; set; }

    public OptionsWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BackgroundHelper.ApplyDefaultBackground(this, "optionsbg.png", IniOverlayService);
        IniOverlayService?.ApplyLayout(this, "OptionsWindow");
    }

    public IOptionsWindowViewModel? ViewModel
    {
        get => DataContext as IOptionsWindowViewModel;
        set => DataContext = value;
    }
}
