using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using AvClientMvvmContract.Multiplayer.CnCNet;
using AvClientMvvmContract.Online;

using AvClientView.Services;

namespace AvClientView.Multiplayer.CnCNet;

public partial class CnCNetLobby : UserControl, ICnCNetLobbyView
{
    public IIniLayoutOverlayService? IniOverlayService { get; set; }

    public CnCNetLobby()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SetupChatInputEnterKey();
        SetupGameListHover();
        SetupColorDropdownTemplate();
        SetupChatMessageTemplate();
    }

    private bool _infoPanelPositioned;

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ApplyDefaultBackground("cncnetlobbybg.png");
        var iniOverlay = IniOverlayService;
        iniOverlay?.ApplyLayout(this, "CnCNetLobby");

        if (!_infoPanelPositioned)
        {
            LayoutUpdated += PositionInfoPanel;
        }
    }

    private void PositionInfoPanel(object? sender, EventArgs e)
    {
        if (lbGameList == null || panelGameInfo == null) return;

        var bounds = lbGameList.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        // Position to the right of the game list (which may have been moved/resized by INI layout).
        // Matches the old client: panelGameInformation.X = gameList.Right, .Y = gameList.Y
        Canvas.SetLeft(panelGameInfo, bounds.Right);
        Canvas.SetTop(panelGameInfo, bounds.Top);
        panelGameInfo.MaxHeight = bounds.Height;
        panelGameInfo.Width = bounds.Width;

        _infoPanelPositioned = true;
        LayoutUpdated -= PositionInfoPanel;
    }

    private void SetupColorDropdownTemplate()
    {
        ddColor.ItemTemplate = new FuncDataTemplate<IIRCColor>((color, _) =>
        {
            var tb = new TextBlock();
            tb.Bind(TextBlock.TextProperty, new Binding("Name"));
            if (color != null)
                tb.Foreground = new SolidColorBrush(Color.FromArgb(255, color.R, color.G, color.B));
            return tb;
        });
    }

    private void SetupChatMessageTemplate()
    {
        if (lbChatList == null) return;

        lbChatList.ItemTemplate = new FuncDataTemplate<IChatMessage>((msg, _) =>
        {
            var tb = new TextBlock { TextWrapping = TextWrapping.Wrap };
            if (msg != null)
            {
                tb.Text = FormatChatMessage(msg);
                tb.Foreground = new SolidColorBrush(Color.FromArgb(255, msg.Color.R, msg.Color.G, msg.Color.B));
            }
            return tb;
        });
    }

    private static string FormatChatMessage(IChatMessage msg)
    {
        string timestamp = msg.DateTime.ToShortTimeString();
        if (string.IsNullOrEmpty(msg.SenderName))
            return $"[{timestamp}] {msg.Message}";
        return $"[{timestamp}] {msg.SenderName}: {msg.Message}";
    }

    private void ApplyDefaultBackground(string texturePath)
    {
        try
        {
            var iniOverlay = IniOverlayService;
            if (iniOverlay == null) return;
            var fullPath = iniOverlay.FindTextureFile(texturePath);
            if (fullPath != null)
            {
                var bitmap = new Bitmap(fullPath);
                Background = new ImageBrush { Source = bitmap, Stretch = Stretch.UniformToFill };
            }
        }
        catch { }
    }

    private void SetupGameListHover()
    {
        if (lbGames == null) return;

        lbGames.AddHandler(PointerMovedEvent, (s, e) =>
        {
            var vm = ViewModel;
            if (vm == null) return;

            var point = e.GetPosition(lbGames);
            const double itemHeight = 18.0;
            int index = (int)(point.Y / itemHeight);
            if (index < 0 || index >= vm.Games.Count)
                vm.HoveredGameIndex = -1;
            else
                vm.HoveredGameIndex = index;
        }, handledEventsToo: true);

        lbGames.AddHandler(PointerExitedEvent, (s, e) =>
        {
            var vm = ViewModel;
            if (vm != null)
                vm.HoveredGameIndex = -1;
        }, handledEventsToo: true);
    }

    private void SetupChatInputEnterKey()
    {
        if (tbChatInput != null)
        {
            tbChatInput.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Return && ViewModel?.SendChatMessageCommand.CanExecute(null) == true)
                {
                    ViewModel.SendChatMessageCommand.Execute(null);
                    e.Handled = true;
                }
            };
        }
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
