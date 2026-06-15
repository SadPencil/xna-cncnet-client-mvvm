using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using AvClientMvvmContract.Multiplayer.GameLobby;
using AvClientMvvmContract.ViewServices;

using AvClientView.Controls;
using AvClientView.Converters;
using AvClientView.Services;

using ClientCore;
using ClientCore.Extensions;


namespace AvClientView.Multiplayer.GameLobby;

public partial class CnCNetGameLobby : UserControl, ICnCNetGameLobbyView
{
    public AvClientView.Services.IIniLayoutOverlayService? IniOverlayService { get; set; }

    private IMapPreviewBoxViewModel? currentMapPreview;
    private IGameLobbyViewModel? lobbyViewModel;
    private readonly List<Border> indicatorElements = new();

    public CnCNetGameLobby()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        SetupChatInputEnterKey();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (currentMapPreview != null)
            currentMapPreview.PropertyChanged -= OnMapPreviewPropertyChanged;

        lobbyViewModel = DataContext as IGameLobbyViewModel;
        currentMapPreview = lobbyViewModel?.MapPreviewBox;

        if (currentMapPreview != null)
        {
            currentMapPreview.PropertyChanged += OnMapPreviewPropertyChanged;
            UpdateMapPreviewImage(currentMapPreview.MapPreviewImage);
            RenderIndicators();
        }
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BackgroundHelper.ApplyDefaultBackground(this, "gamelobbybg.png", IniOverlayService);

        var iniOverlay = IniOverlayService;
        iniOverlay?.ApplyLayout(this, "MultiplayerGameLobby");

        SetupMapListContextMenu();
        SetupMapListHoverTracking();
        SetupSearchContextMenu();
        SetupPlayerNameContextMenus();

        if (currentMapPreview != null)
            RenderIndicators();
    }

    private void SetupPlayerNameContextMenus()
    {
        var nameSlots = new[] { playerNameSlot0, playerNameSlot1, playerNameSlot2, playerNameSlot3,
                                playerNameSlot4, playerNameSlot5, playerNameSlot6, playerNameSlot7 };

        foreach (var cb in nameSlots)
        {
            cb.ContextRequested += (s, e) =>
            {
                e.Handled = true;
                ShowPlayerNameContextMenu(cb, e);
            };
        }
    }

    private void ShowPlayerNameContextMenu(ComboBox comboBox, ContextRequestedEventArgs e)
    {
        var vm = ViewModel;
        if (vm == null)
            return;

        vm.SelectedContextPlayerName = comboBox.SelectedItem as string ?? string.Empty;

        var contextMenu = new ContextMenu();
        RenderContextMenuItems(contextMenu, vm.PlayerContextMenuItems);
        if (contextMenu.Items.Count > 0)
            contextMenu.Open(comboBox);
    }

    private void SetupChatInputEnterKey()
    {
        tbChatInput.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter && ViewModel != null)
            {
                ViewModel.SendChatMessageCommand.Execute(null);
                e.Handled = true;
            }
        };
    }

    public ICnCNetGameLobbyViewModel? ViewModel
    {
        get => DataContext as ICnCNetGameLobbyViewModel;
        set
        {
            if (DataContext != value)
                DataContext = value;
        }
    }

    IMultiplayerGameLobbyViewModel? IMultiplayerGameLobbyView.ViewModel
    {
        get => DataContext as IMultiplayerGameLobbyViewModel;
        set => ViewModel = value as ICnCNetGameLobbyViewModel;
    }

    IGameLobbyViewModel? IGameLobbyView.ViewModel
    {
        get => DataContext as IGameLobbyViewModel;
        set => ViewModel = value as ICnCNetGameLobbyViewModel;
    }

    private void OnMapPreviewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IMapPreviewBoxViewModel.MapPreviewImage)
            && sender is IMapPreviewBoxViewModel preview)
        {
            UpdateMapPreviewImage(preview.MapPreviewImage);
        }
        else if (e.PropertyName == nameof(IMapPreviewBoxViewModel.StartingLocationIndicators))
        {
            RenderIndicators();
        }
    }

    private void UpdateMapPreviewImage(SixLabors.ImageSharp.Image? image)
    {
        mapPreviewImage.Source = ImageSharpToBitmapConverter.Instance.Convert(image, typeof(Avalonia.Media.Imaging.Bitmap), null, System.Globalization.CultureInfo.InvariantCulture) as Avalonia.Media.Imaging.Bitmap;
    }

    // --- Indicator rendering ---

    private const double INDICATOR_SIZE = 20.0;

    private void RenderIndicators()
    {
        foreach (var el in indicatorElements)
            indicatorsCanvas.Children.Remove(el);
        indicatorElements.Clear();

        if (currentMapPreview == null)
            return;

        var indicators = currentMapPreview.StartingLocationIndicators;

        foreach (var data in indicators)
        {
            if (!data.IsVisible)
                continue;

            var indicatorPanel = new Border
            {
                Width = INDICATOR_SIZE + 60,
                Height = INDICATOR_SIZE + 8,
                Padding = new Thickness(2),
                Background = Brushes.Transparent,
                Tag = data.WaypointNumber
            };

            var stackPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                IsHitTestVisible = false
            };

            var numBorder = new Border
            {
                Width = INDICATOR_SIZE,
                Height = INDICATOR_SIZE,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromRgb(0, 0, 0), 0.6),
                Child = new TextBlock
                {
                    Text = data.WaypointNumber.ToString(),
                    Foreground = new SolidColorBrush(Colors.White),
                    FontWeight = FontWeight.Bold,
                    FontSize = 10,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                }
            };
            stackPanel.Children.Add(numBorder);

            if (data.Players != null && data.Players.Count > 0)
            {
                var namesStack = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Vertical,
                    Margin = new Thickness(4, 0, 0, 0)
                };
                foreach (var player in data.Players)
                {
                    namesStack.Children.Add(new TextBlock
                    {
                        Text = player.Name,
                        FontSize = 9,
                        Foreground = new SolidColorBrush(Colors.White)
                    });
                }
                stackPanel.Children.Add(namesStack);
            }

            indicatorPanel.Child = stackPanel;

            Canvas.SetLeft(indicatorPanel, data.X);
            Canvas.SetTop(indicatorPanel, data.Y);

            IndicatorSetupHandlers(indicatorPanel, data.WaypointNumber);

            indicatorsCanvas.Children.Add(indicatorPanel);
            indicatorElements.Add(indicatorPanel);
        }
    }

    private void IndicatorSetupHandlers(Border indicator, int waypointNumber)
    {
        indicator.PointerPressed += (s, e) =>
        {
            if (currentMapPreview == null)
                return;

            var point = e.GetCurrentPoint(indicator);
            var props = point.Properties;

            if (props.IsLeftButtonPressed)
            {
                HandleIndicatorLeftClick(waypointNumber);
                e.Handled = true;
            }
            else if (props.IsRightButtonPressed)
            {
                HandleIndicatorRightClick(waypointNumber);
                e.Handled = true;
            }
        };
    }

    private void HandleIndicatorLeftClick(int waypointNumber)
    {
        if (currentMapPreview == null)
            return;

        currentMapPreview.SelectedStartingLocationIndex = waypointNumber;

        if (currentMapPreview.EnableContextMenu)
        {
            ShowIndicatorContextMenu(waypointNumber);
        }
        else
        {
            currentMapPreview.SelectStartingLocationCommand.Execute(null);
        }
    }

    private void HandleIndicatorRightClick(int waypointNumber)
    {
        if (currentMapPreview == null)
            return;

        currentMapPreview.SelectedStartingLocationIndex = waypointNumber;
        currentMapPreview.ClearStartingLocationCommand.Execute(null);
    }

    // --- Context menus ---

    private void SetupMapListContextMenu()
    {
        mapListBox.ContextRequested += (s, e) =>
        {
            e.Handled = true;
            ShowMapContextMenu();
        };
    }

    private void SetupMapListHoverTracking()
    {
        LobbyHelper.SetUpHoverTracking(mapListBox, idx =>
        {
            if (lobbyViewModel != null)
                lobbyViewModel.SetHoveredMapIndexCommand.Execute(idx);
        }, rowHeight: 20.0);
    }

    private void SetupSearchContextMenu()
    {
        tbMapSearch.ContextRequested += (s, e) =>
        {
            e.Handled = true;
            ShowSearchContextMenu();
        };
    }

    private void ShowSearchContextMenu()
    {
        if (lobbyViewModel == null)
            return;

        var contextMenu = new ContextMenu();
        var toggleSearchItem = new MenuItem { Header = "Toggle Search All Game Modes".L10N("Client:UI:ToggleSearchAllGameModes") };
        toggleSearchItem.Click += (s, e) => lobbyViewModel.ToggleSearchAllModesCommand.Execute(null);
        contextMenu.Items.Add(toggleSearchItem);
        contextMenu.Open(tbMapSearch);
    }

    private void ShowMapContextMenu()
    {
        if (lobbyViewModel == null)
            return;

        var contextMenu = new ContextMenu();
        RenderContextMenuItems(contextMenu, lobbyViewModel.MapContextMenuItems);
        contextMenu.Open(mapListBox);
    }

    private void ShowIndicatorContextMenu(int waypointNumber)
    {
        if (lobbyViewModel == null || currentMapPreview == null)
            return;

        var contextMenu = new ContextMenu();
        RenderContextMenuItems(contextMenu, lobbyViewModel.StartingLocationAssignMenuItems);
        contextMenu.Open(mapPreviewPanel);
    }

    public void Show() => IsVisible = true;
    public void Hide() => IsVisible = false;
    public string GetDisplayName() => "CnCNet Game Lobby";

    private static void RenderContextMenuItems(ContextMenu menu, System.Collections.Generic.IReadOnlyList<IContextMenuItem> items)
    {
        menu.Items.Clear();
        foreach (var item in items)
        {
            if (item.IsSeparator)
                menu.Items.Add(new Separator());
            else if (item.IsVisible)
                menu.Items.Add(new MenuItem { Header = item.Text, Command = item.Command, IsEnabled = item.IsEnabled });
        }
    }
}
