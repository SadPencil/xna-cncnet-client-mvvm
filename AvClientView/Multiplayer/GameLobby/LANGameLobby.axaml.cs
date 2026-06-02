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

using AvClientView.Services;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientView.Multiplayer.GameLobby;

public partial class LANGameLobby : UserControl, ILANGameLobbyView
{
    private IMapPreviewBoxViewModel? currentMapPreview;
    private IGameLobbyViewModel? lobbyViewModel;
    private readonly List<Border> indicatorElements = new();

    public LANGameLobby()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SetupChatInputEnterKey();
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ApplyDefaultBackground("gamelobbybg.png");

        var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "MultiplayerGameLobby");

        SetupMapListContextMenu();
    }

    private void ApplyDefaultBackground(string texturePath)
    {
        try
        {
            var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
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

    public ILANGameLobbyViewModel? ViewModel
    {
        get => DataContext as ILANGameLobbyViewModel;
        set
        {
            if (currentMapPreview != null)
                currentMapPreview.PropertyChanged -= OnMapPreviewPropertyChanged;

            lobbyViewModel = value as IGameLobbyViewModel;
            DataContext = value;

            if (value?.MapPreviewBox != null)
            {
                currentMapPreview = value.MapPreviewBox;
                currentMapPreview.PropertyChanged += OnMapPreviewPropertyChanged;
                UpdateMapPreviewImage(currentMapPreview.MapPreviewImageBytes);
                RenderIndicators();
            }
        }
    }

    // Explicit interface implementations
    IMultiplayerGameLobbyViewModel? IMultiplayerGameLobbyView.ViewModel
    {
        get => DataContext as IMultiplayerGameLobbyViewModel;
        set => ViewModel = value as ILANGameLobbyViewModel;
    }

    IGameLobbyViewModel? IGameLobbyView.ViewModel
    {
        get => DataContext as IGameLobbyViewModel;
        set => ViewModel = value as ILANGameLobbyViewModel;
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
                Background = new SolidColorBrush(Colors.Transparent),
                Tag = data.WaypointNumber
            };

            var stackPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };

            var numBlock = new TextBlock
            {
                Text = data.WaypointNumber.ToString(),
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = FontWeight.Bold,
                FontSize = 10,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            stackPanel.Children.Add(numBlock);

            if (data.Players != null && data.Players.Count > 0)
            {
                var namesStack = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Vertical,
                    Margin = new Thickness(16, 0, 0, 0)
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
            if (slot.SelectedNameIndex < 0)
                continue;
            if (slot.PlayerName == null)
                continue;

            string playerName;
            if (i < lobbyViewModel.PlayerNames.Count)
                playerName = lobbyViewModel.PlayerNames[i];
            else
            {
                playerName = slot.PlayerName;
                if (slot.SelectedNameIndex > 0 && slot.SelectedNameIndex < slot.NameOptions.Count)
                    playerName = slot.NameOptions[slot.SelectedNameIndex];
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
    public string GetDisplayName() => "LAN Game Lobby";
}
