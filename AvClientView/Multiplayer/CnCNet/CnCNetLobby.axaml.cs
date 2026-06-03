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

using Serilog;

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
        // Compute ZIndex to float above all other content in the Canvas
        int maxZ = 0;
        foreach (var child in MainCanvas.Children)
        {
            if (child.ZIndex > maxZ)
                maxZ = child.ZIndex;
        }
        panelGameInfo.ZIndex = maxZ + 1;

        _infoPanelPositioned = true;
        LayoutUpdated -= PositionInfoPanel;
    }

    private void SetupColorDropdownTemplate()
    {
        Log.Information("[LOG-View] ddColor.ItemTemplate setup START");
        var template = new FuncDataTemplate<object>((item, _) =>
        {
            Log.Information("[LOG-View] ddColor.FuncDataTemplate called: itemType={Type}, itemToString={Str}",
                item?.GetType().FullName ?? "null",
                item?.ToString() ?? "null");

            var tb = new TextBlock();
            if (item is IIRCColor color)
            {
                tb.Text = color.Name;
                tb.Foreground = new SolidColorBrush(Color.FromArgb(255, color.R, color.G, color.B));
                Log.Information("[LOG-View] ddColor.FuncDataTemplate IIRCColor match: Name={Name}, R={R}, G={G}, B={B}, TextBlock.Text={TbText}",
                    color.Name, color.R, color.G, color.B, tb.Text);
            }
            else
            {
                tb.Text = item?.ToString() ?? "null";
                Log.Warning("[LOG-View] ddColor.FuncDataTemplate NOT IIRCColor: fullType={Type}",
                    item?.GetType().FullName ?? "null");
            }
            return tb;
        }, supportsRecycling: true);

        ddColor.ItemTemplate = template;
        ddColor.SelectionBoxItemTemplate = template;
        Log.Information("[LOG-View] ddColor.ItemTemplate and SelectionBoxItemTemplate SET");
    }

    private void SetupChatMessageTemplate()
    {
        if (lbChatList == null)
        {
            Log.Warning("[LOG-View] lbChatList is null, cannot setup ChatMessage template");
            return;
        }
        Log.Information("[LOG-View] lbChatList.ItemTemplate setup START, ChatMessages.Count will be logged at VM side");

        lbChatList.ItemTemplate = new FuncDataTemplate<IChatMessage>((msg, _) =>
        {
            Log.Information("[LOG-View] ChatMessage.FuncDataTemplate called: msgType={Type}, SenderName={Sender}, Color=({R},{G},{B}), Message={Msg}",
                msg?.GetType().FullName ?? "null",
                msg?.SenderName ?? "null",
                msg?.Color.R ?? 0, msg?.Color.G ?? 0, msg?.Color.B ?? 0,
                msg?.Message?.Substring(0, Math.Min(msg?.Message?.Length ?? 0, 40)) ?? "null");

            var tb = new TextBlock { TextWrapping = TextWrapping.Wrap };
            if (msg != null)
            {
                tb.Text = FormatChatMessage(msg);
                tb.Foreground = new SolidColorBrush(Color.FromArgb(255, msg.Color.R, msg.Color.G, msg.Color.B));
                Log.Information("[LOG-View] ChatMessage.FuncDataTemplate rendered: TextBlock.Text={TbText}, Foreground.Color=({R},{G},{B})",
                    tb.Text, msg.Color.R, msg.Color.G, msg.Color.B);
            }
            else
            {
                tb.Text = "(null message)";
                Log.Warning("[LOG-View] ChatMessage.FuncDataTemplate: msg is null");
            }
            return tb;
        });
        Log.Information("[LOG-View] lbChatList.ItemTemplate SET");
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
        set
        {
            DataContext = value;
            if (value != null)
            {
                Log.Information("[LOG-View] ViewModel set: ColorOptions.Count={ColorCount}, ChatMessages.Count={ChatCount}, Games.Count={GameCount}, ChannelOptions.Count={ChanCount}",
                    value.ColorOptions?.Count ?? -1,
                    value.ChatMessages?.Count ?? -1,
                    value.Games?.Count ?? -1,
                    value.ChannelOptions?.Count ?? -1);
                Log.Information("[LOG-View] ViewModel.set: concreteType={Type}", value.GetType().FullName);
            }
        }
    }

    void ISwitchableView.Show() => IsVisible = true;
    void ISwitchableView.Hide() => IsVisible = false;
    string ISwitchableView.GetDisplayName() => "CnCNet Lobby";
}
