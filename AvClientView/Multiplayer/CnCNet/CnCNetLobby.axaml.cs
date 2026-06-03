using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using AvClientMvvmContract.Multiplayer.CnCNet;

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
        PropertyChanged += (s, e) =>
        {
            if (e.Property == DataContextProperty)
            {
                Log.Information("[LOG-View-CNC] DataContext changed: new={NewType}, old={OldType}",
                    e.NewValue?.GetType().FullName ?? "null",
                    e.OldValue?.GetType().FullName ?? "null");
            }
        };
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

        Log.Information("[LOG-View-CNC] OnLoaded: ddColor={ddColor}, IsNull={IsNull}",
            ddColor?.GetType().FullName ?? "null", ddColor == null);

        if (ddColor != null)
        {
            Log.Information("[LOG-View-CNC] ddColor state: ItemCount={IC}, ItemsSourceType={IST}, DisplayMemberBindingType={DMB}",
                ddColor.ItemCount,
                ddColor.ItemsSource?.GetType().FullName ?? "null",
                ddColor.DisplayMemberBinding?.GetType().FullName ?? "null");

            // Dump first 3 items
            for (int i = 0; i < ddColor.ItemCount && i < 3; i++)
            {
                var item = ddColor.Items[i];
                Log.Information("[LOG-View-CNC] ddColor.Item[{I}]: type={T}, ToString()='{TS}', is IIRCColor={IsC}",
                    i, item?.GetType().FullName ?? "null",
                    item?.ToString() ?? "null",
                    item is AvClientMvvmContract.Online.IIRCColor);
                if (item is AvClientMvvmContract.Online.IIRCColor c)
                    Log.Information("[LOG-View-CNC]   -> Name='{N}', R={R}, G={G}, B={B}", c.Name, c.R, c.G, c.B);
            }

            // Check DataContext
            Log.Information("[LOG-View-CNC] DataContext={DC}, Is VM={IsVM}",
                DataContext?.GetType().FullName ?? "null",
                DataContext is ICnCNetLobbyViewModel);
        }

        // Second pass: after layout, items may be populated
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (ddColor != null)
            {
                Log.Information("[LOG-View-CNC] POST-OnLoaded: ddColor.ItemCount={IC}", ddColor.ItemCount);
                for (int i = 0; i < ddColor.ItemCount && i < 3; i++)
                {
                    var item = ddColor.Items[i];
                    Log.Information("[LOG-View-CNC] POST Item[{I}]: ToString()='{TS}'", i, item?.ToString() ?? "null");
                }
            }
        }, Avalonia.Threading.DispatcherPriority.Loaded);
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
