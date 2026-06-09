using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;

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
        AutoScrollToEnd(chatList);
        AutoScrollToEnd(playerList);
    }

    /// <summary>
    /// Auto-scrolls the list to the end when new items arrive, but only if the user
    /// is already at the bottom. If they've scrolled up to read history, don't jump.
    /// </summary>
    private static void AutoScrollToEnd(ListBox listBox)
    {
        listBox.TemplateApplied += (_, _) =>
        {
            var sv = listBox.FindDescendantOfType<ScrollViewer>();
            if (sv is null) return;

            bool isAtBottom = true;

            sv.ScrollChanged += (_, _) =>
            {
                isAtBottom = sv.Offset.Y >= sv.Extent.Height - sv.Viewport.Height - 2;
            };

            var col = listBox.Items as System.Collections.Specialized.INotifyCollectionChanged;
            if (col is not null)
            {
                col.CollectionChanged += (_, _) =>
                {
                    if (isAtBottom)
                        sv.ScrollToEnd();
                };
            }

            // initial scroll
            sv.ScrollToEnd();
        };
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
