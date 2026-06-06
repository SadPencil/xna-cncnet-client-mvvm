using Avalonia.Controls;

using AvClientMvvmContract.Generic;

using AvClientView.Controls;
using AvClientView.Services;


namespace AvClientView.Generic;

public partial class ExtrasWindow : UserControl, IExtrasWindowView
{
    public AvClientView.Services.IIniLayoutOverlayService? IniOverlayService { get; set; }

    public ExtrasWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BackgroundHelper.ApplyDefaultBackground(this, "extrasMenu.png", IniOverlayService);

        var iniOverlay = IniOverlayService;
        iniOverlay?.ApplyLayout(this, "ExtrasWindow");
    }

    public IExtrasWindowViewModel? ViewModel
    {
        get => DataContext as IExtrasWindowViewModel;
        set => DataContext = value;
    }
}
