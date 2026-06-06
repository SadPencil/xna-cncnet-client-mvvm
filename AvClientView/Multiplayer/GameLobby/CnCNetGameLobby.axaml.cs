using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Timers;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using AvClientMvvmContract.Multiplayer.GameLobby;

using AvClientView.Controls;
using AvClientView.Services;
using Serilog;

using Timer = System.Timers.Timer;


namespace AvClientView.Multiplayer.GameLobby;

public partial class CnCNetGameLobby : UserControl, ICnCNetGameLobbyView
{
    public AvClientView.Services.IIniLayoutOverlayService? IniOverlayService { get; set; }

    private IMapPreviewBoxViewModel? currentMapPreview;
    private IGameLobbyViewModel? lobbyViewModel;
    private readonly List<Border> indicatorElements = new();
    private IPlayerSlotObservable? watchedSlot0;
    private IPlayerSlotDropdown<IPlayerName>? watchedSlot0Name;

    public CnCNetGameLobby()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        SetupChatInputEnterKey();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        Log.Debug($"[DEBUG_HOSTSLOT_VIEW] OnDataContextChanged: DataContext={DataContext?.GetType().Name}, IsVisible={IsVisible}");
        if (currentMapPreview != null)
            currentMapPreview.PropertyChanged -= OnMapPreviewPropertyChanged;

        lobbyViewModel = DataContext as IGameLobbyViewModel;
        currentMapPreview = lobbyViewModel?.MapPreviewBox;

        // Subscribe to slot[0] changes to see if PropertyChanged fires
        if (watchedSlot0 != null)
            watchedSlot0.PropertyChanged -= OnSlot0PropertyChanged;
        if (watchedSlot0Name != null)
            watchedSlot0Name.PropertyChanged -= OnSlot0NamePropertyChanged;

        if (lobbyViewModel != null)
        {
            var slots = lobbyViewModel.PlayerSlots;
            if (slots != null && slots.Count > 0)
            {
                var slot0 = slots[0];
                Log.Debug($"[DEBUG_HOSTSLOT_VIEW] OnDataContextChanged: slot[0].PlayerName={slot0.PlayerName}, slot[0].Name.SelectedOption={slot0.Name.SelectedOption?.ToString()}, slot[0].Name.Options.Count={slot0.Name.Options.Count}");
                if (slot0.Name.Options.Count > 0)
                    Log.Debug($"[DEBUG_HOSTSLOT_VIEW] OnDataContextChanged: slot[0].Name.Options[0]={slot0.Name.Options[0]}");

                watchedSlot0 = slot0;
                watchedSlot0.PropertyChanged += OnSlot0PropertyChanged;
                watchedSlot0Name = slot0.Name;
                watchedSlot0Name.PropertyChanged += OnSlot0NamePropertyChanged;
                Log.Debug($"[DEBUG_HOSTSLOT_VIEW] Subscribed to slot[0].PropertyChanged and slot[0].Name.PropertyChanged");
            }
        }

        if (currentMapPreview != null)
        {
            currentMapPreview.PropertyChanged += OnMapPreviewPropertyChanged;
            UpdateMapPreviewImage(currentMapPreview.MapPreviewImageBytes);
            RenderIndicators();
        }
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.Debug($"[DEBUG_HOSTSLOT_VIEW] OnLoaded: DataContext={DataContext?.GetType().Name}, IsVisible={IsVisible}");

        // Log slot 0 from ViewModel
        if (lobbyViewModel != null)
        {
            var slots = lobbyViewModel.PlayerSlots;
            if (slots != null && slots.Count > 0)
            {
                var slot0 = slots[0];
                Log.Debug($"[DEBUG_HOSTSLOT_VIEW] OnLoaded slot[0]: PlayerName={slot0.PlayerName}, SelectedOption={slot0.Name.SelectedOption}, Options.Count={slot0.Name.Options.Count}, IsEnabled={slot0.Name.IsEnabled}");
            }
        }

        // Delayed check after bindings settle
        var timer = new Timer(3000);
        timer.AutoReset = false;
        timer.Elapsed += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                Log.Debug($"[DEBUG_HOSTSLOT_VIEW] DelayedCheck(3s): IsVisible={IsVisible}, DataContext={DataContext?.GetType().Name}");
                if (lobbyViewModel != null)
                {
                    var slots = lobbyViewModel.PlayerSlots;
                    if (slots != null && slots.Count > 0)
                    {
                        var slot0 = slots[0];
                        Log.Debug($"[DEBUG_HOSTSLOT_VIEW] DelayedCheck(3s) slot[0]: PlayerName={slot0.PlayerName}, SelectedOption={slot0.Name.SelectedOption}, Options.Count={slot0.Name.Options.Count}, IsEnabled={slot0.Name.IsEnabled}");
                    }
                }
            });
        };
        timer.Start();

        BackgroundHelper.ApplyDefaultBackground(this, "gamelobbybg.png", IniOverlayService);

        var iniOverlay = IniOverlayService;
        iniOverlay?.ApplyLayout(this, "MultiplayerGameLobby");

        SetupMapListContextMenu();
        SetupSearchContextMenu();

        if (currentMapPreview != null)
            RenderIndicators();
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
        if (e.PropertyName == nameof(IMapPreviewBoxViewModel.MapPreviewImageBytes)
            && sender is IMapPreviewBoxViewModel preview)
        {
            UpdateMapPreviewImage(preview.MapPreviewImageBytes);
        }
        else if (e.PropertyName == nameof(IMapPreviewBoxViewModel.StartingLocationIndicators))
        {
            RenderIndicators();
        }
    }

    private void UpdateMapPreviewImage(byte[]? imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            mapPreviewImage.Source = null;
            return;
        }

        try
        {
            using var ms = new MemoryStream(imageBytes);
            mapPreviewImage.Source = new Bitmap(ms);
        }
        catch
        {
            mapPreviewImage.Source = null;
        }
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
        var toggleSearchItem = new MenuItem { Header = "Toggle Search All Game Modes" };
        toggleSearchItem.Click += (s, e) => lobbyViewModel.ToggleSearchAllModesCommand.Execute(null);
        contextMenu.Items.Add(toggleSearchItem);
        contextMenu.Open(tbMapSearch);
    }

    private void ShowMapContextMenu()
    {
        if (lobbyViewModel == null)
            return;

        var contextMenu = new ContextMenu();
        PopulateMapContextMenu(contextMenu);
        contextMenu.Open(mapListBox);
    }

    private void PopulateMapContextMenu(ContextMenu menu)
    {
        if (lobbyViewModel == null)
            return;

        menu.Items.Clear();

        var toggleFavItem = new MenuItem
        {
            Header = currentMapPreview?.IsFavorite == true ? "Remove Favorite" : "Add Favorite"
        };
        toggleFavItem.Click += (s, e) => lobbyViewModel.ToggleFavoriteCommand.Execute(null);
        menu.Items.Add(toggleFavItem);

        var deleteItem = new MenuItem { Header = "Delete Map" };
        deleteItem.Click += (s, e) => lobbyViewModel.DeleteMapCommand.Execute(null);
        menu.Items.Add(deleteItem);

        var showFolderItem = new MenuItem { Header = "Show in Folder" };
        showFolderItem.Click += (s, e) => lobbyViewModel.ShowMapInFolderCommand.Execute(null);
        menu.Items.Add(showFolderItem);
    }

    private void ShowIndicatorContextMenu(int waypointNumber)
    {
        if (lobbyViewModel == null || currentMapPreview == null)
            return;

        var contextMenu = new ContextMenu();

        int menuId = 1;
        for (int i = 0; i < lobbyViewModel.PlayerSlots.Count; i++)
        {
            var slot = lobbyViewModel.PlayerSlots[i];
            if (slot.Name.SelectedOption == null || slot.Name.SelectedOption.Index < 1)
                continue;

            string playerName;
            if (i < lobbyViewModel.PlayerNames.Count)
                playerName = lobbyViewModel.PlayerNames[i];
            else
            {
                playerName = slot.PlayerName ?? string.Empty;
                if (slot.Name.SelectedOption != null)
                    playerName = slot.Name.SelectedOption.Name;
            }

            if (string.IsNullOrEmpty(playerName))
                continue;

            var displayName = $"{menuId}. {playerName}";
            var playerIndex = i;
            var item = new MenuItem { Header = displayName };
            item.Click += (s, e) =>
            {
                if (currentMapPreview != null)
                {
                    currentMapPreview.SelectedPlayerIndex = playerIndex;
                    currentMapPreview.AssignStartingLocationCommand.Execute(null);
                }
            };
            contextMenu.Items.Add(item);
            menuId++;
        }

        contextMenu.Open(mapPreviewPanel);
    }

    public void Show() => IsVisible = true;
    public void Hide() => IsVisible = false;
    public string GetDisplayName() => "CnCNet Game Lobby";

    private void OnSlot0PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var slot = (IPlayerSlotObservable?)sender;
        Log.Debug($"[DEBUG_HOSTSLOT_VIEW] slot[0].PropertyChanged: {e.PropertyName}, PlayerName={slot?.PlayerName}");
    }

    private void OnSlot0NamePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var name = (IPlayerSlotDropdown<IPlayerName>?)sender;
        Log.Debug($"[DEBUG_HOSTSLOT_VIEW] slot[0].Name.PropertyChanged: {e.PropertyName}, SelectedOption={name?.SelectedOption}, Options.Count={name?.Options.Count}");
    }
}
