using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using AvClientMvvmContract.Multiplayer.CnCNet;
using AvClientMvvmContract.Online;

using AvClientView.Converters;
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

        Log.Information("[LOG-View-CNC] Constructor");
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

        // Wire ddColor: create colored items in dropdown via code-behind
        if (ddColor != null)
        {
            Log.Information("[LOG-View-CNC] ddColor found, wiring color items");
            ddColor.DropDownOpened += OnColorDropDownOpened;
        }
    }

    private void OnColorDropDownOpened(object? sender, EventArgs e)
    {
        if (ddColor == null) return;
        Log.Information("[LOG-View-CNC] ddColor DropDownOpened, ItemCount={IC}", ddColor.ItemCount);

        // Try immediately, then retry after layout if items not ready
        TryColorItems();
        Avalonia.Threading.Dispatcher.UIThread.Post(TryColorItems, Avalonia.Threading.DispatcherPriority.Loaded);
        Avalonia.Threading.Dispatcher.UIThread.Post(TryColorItems, Avalonia.Threading.DispatcherPriority.Background);
    }

    private void TryColorItems()
    {
        if (ddColor == null) return;

        var popup = ddColor.FindControl<Popup>("PART_Popup");
        var root = popup ?? (Control)ddColor;

        int colored = 0;
        WalkAndColorItems(root, ref colored);
        if (colored > 0)
            Log.Information("[LOG-View-CNC] TryColorItems: colored {Count} items from {Source}",
                colored, popup != null ? "popup" : "combo");
    }

    private static T? FindVisualChild<T>(Control parent) where T : Control
    {
        if (parent is T t) return t;
        if (parent is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
        }
        if (parent is ContentControl cc && cc.Content is Control content)
            return FindVisualChild<T>(content);
        return null;
    }

    private void WalkAndColorItems(Control parent, ref int colored)
    {
        if (parent is ComboBoxItem cbi)
        {
            // Find the TextBlock inside this ComboBoxItem and set its Foreground
            var tb = FindVisualChild<TextBlock>(cbi);
            if (tb != null && cbi.DataContext is IIRCColor irc)
            {
                var brush = Rgb24ToBrushConverter.Instance.Convert(irc, typeof(IBrush), null,
                    System.Globalization.CultureInfo.CurrentCulture) as IBrush;
                if (brush != null)
                {
                    tb.Foreground = brush;
                    colored++;
                    Log.Information("[LOG-View-CNC] Colored '{Name}' -> #{R:X2}{G:X2}{B:X2}",
                        irc.Name, irc.R, irc.G, irc.B);
                }
            }
        }

        if (parent is Panel panel)
        {
            foreach (var child in panel.Children)
                WalkAndColorItems(child, ref colored);
        }
        if (parent is ContentControl cc && cc.Content is Control content)
            WalkAndColorItems(content, ref colored);
    }

    private void PositionInfoPanel(object? sender, EventArgs e)
    {
        if (lbGameList == null || panelGameInfo == null) return;

        var bounds = lbGameList.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        Canvas.SetLeft(panelGameInfo, bounds.Right);
        Canvas.SetTop(panelGameInfo, bounds.Top);
        panelGameInfo.MaxHeight = bounds.Height;
        panelGameInfo.Width = bounds.Width;

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
