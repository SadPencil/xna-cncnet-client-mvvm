using Avalonia.Controls;

using AvClientMvvmContract.Generic;

using AvClientView.Controls;
using AvClientView.Services;


namespace AvClientView.Generic;

public partial class GameLoadingWindow : UserControl, IGameLoadingWindowView
{
    public AvClientView.Services.IIniLayoutOverlayService? IniOverlayService { get; set; }

    public GameLoadingWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BackgroundHelper.ApplyDefaultBackground(this, "loadmissionbg.png", IniOverlayService);

        var iniOverlay = IniOverlayService;
        iniOverlay?.ApplyLayout(this, "GameLoadingWindow");
    }

    public IGameLoadingWindowViewModel? ViewModel
    {
        get => DataContext as IGameLoadingWindowViewModel;
        set => DataContext = value;
    }
}
