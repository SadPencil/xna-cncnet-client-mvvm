using Avalonia.Controls;

using AvClientMvvmContract.Multiplayer.CnCNet;

using AvClientView.Services;


namespace AvClientView.Multiplayer.CnCNet;

public partial class PrivateMessagingWindow : UserControl, IPrivateMessagingWindowView
{
    public AvClientView.Services.IIniLayoutOverlayService? IniOverlayService { get; set; }

    public PrivateMessagingWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var iniOverlay = IniOverlayService;
        iniOverlay?.ApplyLayout(this, "PrivateMessagingWindow");
    }

    public IPrivateMessagingWindowViewModel? ViewModel
    {
        get => DataContext as IPrivateMessagingWindowViewModel;
        set => DataContext = value;
    }

    void ISwitchableView.Show() => IsVisible = true;
    void ISwitchableView.Hide() => IsVisible = false;
    string ISwitchableView.GetDisplayName() => "Private Messages";
}
