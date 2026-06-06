using Avalonia.Controls;

using AvClientMvvmContract.Multiplayer;

using AvClientView.Controls;
using AvClientView.Services;


namespace AvClientView.Multiplayer;

public partial class LANGameCreationWindow : UserControl, ILANGameCreationWindowView
{
    public AvClientView.Services.IIniLayoutOverlayService? IniOverlayService { get; set; }

    public LANGameCreationWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BackgroundHelper.ApplyDefaultBackground(this, "gamecreationoptionsbg.png", IniOverlayService);

        var iniOverlay = IniOverlayService;
        iniOverlay?.ApplyLayout(this, "GenericWindow");
    }

    public ILANGameCreationWindowViewModel? ViewModel
    {
        get => DataContext as ILANGameCreationWindowViewModel;
        set => DataContext = value;
    }
}
