using Avalonia.Controls;
using Avalonia.Input;

using AvClientMvvmContract.Multiplayer.CnCNet;

using AvClientView.Controls;
using AvClientView.Services;

namespace AvClientView.Multiplayer.CnCNet;

public partial class CnCNetLobby : UserControl, ICnCNetLobbyView
{
    public IIniLayoutOverlayService? IniOverlayService { get; set; }

    public CnCNetLobby()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        gameList.DoubleTapped += (_, _) => ViewModel?.JoinSelectedGameCommand.Execute(null);
        SetUpHoverTracking();
    }

    private void SetUpHoverTracking()
    {
        gameList.AddHandler(PointerMovedEvent, (_, e) =>
        {
            if (ViewModel is not { } vm || vm.Games.Count == 0) return;
            var pos = e.GetPosition(gameList);
            // Estimate item index from Y position. The ListBox item height
            // varies with the data template, so we approximate heuristically.
            // A typical row is ~48px (padding + 3 text lines).
            const double approxRowHeight = 48.0;
            int idx = (int)(pos.Y / approxRowHeight);
            if (idx < 0) idx = 0;
            if (idx >= vm.Games.Count) idx = -1;
            vm.HoveredGameIndex = idx;
        }, handledEventsToo: true);

        gameList.AddHandler(PointerExitedEvent, (_, _) =>
        {
            if (ViewModel is { } vm)
                vm.HoveredGameIndex = -1;
        }, handledEventsToo: true);
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BackgroundHelper.ApplyDefaultBackground(this, "cncnetlobbybg.png", IniOverlayService);
    }

    public ICnCNetLobbyViewModel? ViewModel
    {
        get => DataContext as ICnCNetLobbyViewModel;
        set => DataContext = value;
    }

    void ISwitchableView.Show() => IsVisible = true;
    void ISwitchableView.Hide() => IsVisible = false;
    string ISwitchableView.GetDisplayName() => "CnCNet Lobby";
}
