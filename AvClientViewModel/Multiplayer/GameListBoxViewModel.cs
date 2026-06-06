using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;

using AvClientMvvmContract.Multiplayer;

using AvClientViewModel.Domain.Multiplayer;
using AvClientViewModel.Domain.Multiplayer.CnCNet;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;
using ClientCore.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Timer = System.Timers.Timer;

namespace AvClientViewModel.Multiplayer;

/// <summary>
/// ViewModel for the game list box.
/// Contains all business logic from GameListBox.cs except XNA UI rendering.
/// </summary>
public partial class GameListBoxViewModel : ObservableObject, IGameListBoxViewModel
{
    private const double GAME_REFRESH_INTERVAL_MS = 1000.0;
    private const double DEFAULT_GAME_LIFETIME_SECONDS = 35.0;

    private readonly MapLoader mapLoader;
    private readonly string localGameIdentifier;
    private readonly GameCollection gameCollection;
    private readonly Predicate<GenericHostedGame>? gameMatchesFilter;
    private readonly Action<int>? onJoinGame;
    private Timer? refreshTimer;

    public double GameLifetimeSeconds { get; set; } = DEFAULT_GAME_LIFETIME_SECONDS;

    // --- State ---

    private readonly List<GenericHostedGame> hostedGames = new();

    // --- Observable state ---

    [ObservableProperty]
    public partial int SelectedGameIndex { get; set; }  = -1;

    [ObservableProperty]
    public partial string? SelectedGameName { get; set; }

    [ObservableProperty]
    public partial int SelectedSortOptionIndex { get; set; }

    // --- Observable collections ---

    private readonly ObservableCollection<string> _gameNames = new();
    public IReadOnlyList<string> GameNames => _gameNames;

    private readonly ObservableCollection<string> _sortOptions = new();
    public IReadOnlyList<string> SortOptions => _sortOptions;

    // --- Constructor ---

    public GameListBoxViewModel(MapLoader mapLoader, string localGameIdentifier, GameCollection gameCollection, Predicate<GenericHostedGame>? gameMatchesFilter = null, Action<int>? onJoinGame = null)
    {
        this.mapLoader = mapLoader;
        this.localGameIdentifier = localGameIdentifier;
        this.gameCollection = gameCollection;
        this.gameMatchesFilter = gameMatchesFilter;
        this.onJoinGame = onJoinGame;

        // Initialize sort options
        _sortOptions.Add("A-Z".L10N("Client:Main:SortAZ"));
        _sortOptions.Add("Z-A".L10N("Client:Main:SortZA"));

        SelectedSortOptionIndex = UserINISettings.Instance.SortState.Value;
    }

    // --- Commands ---

    [RelayCommand]
    private void JoinSelectedGame()
    {
        if (SelectedGameIndex < 0 || SelectedGameIndex >= hostedGames.Count)
            return;

        onJoinGame?.Invoke(SelectedGameIndex);
    }

    [RelayCommand]
    private void RefreshGames()
    {
        RemoveExpiredGames();
        RefreshGameList();
    }

    // --- Property change handlers ---

    partial void OnSelectedGameIndexChanged(int value)
    {
        if (value >= 0 && value < hostedGames.Count)
            SelectedGameName = hostedGames[value].RoomName;
        else
            SelectedGameName = null;
    }

    partial void OnSelectedSortOptionIndexChanged(int value)
    {
        UserINISettings.Instance.SortState.Value = value;
        RefreshGameList();
    }

    // --- Lifecycle (called by parent ViewModel, not View) ---

    public void Initialize()
    {
        StartRefreshTimer();
    }

    public void Cleanup()
    {
        StopRefreshTimer();
    }

    // --- Game management (called by parent ViewModel) ---

    public void AddGame(GenericHostedGame game)
    {
        hostedGames.Add(game);

        // Early notify the map preview cache
        mapLoader.PrefetchCachedPreviewImageFromMap(mapLoader.FindMapByHash(game.MapHash));

        RefreshGameList();
    }

    public void RemoveGame(int index)
    {
        if (index < 0 || index >= hostedGames.Count)
            return;

        hostedGames.RemoveAt(index);
        RefreshGameList();
    }

    public void ClearGames()
    {
        hostedGames.Clear();
        _gameNames.Clear();
        SelectedGameIndex = -1;
    }

    public GenericHostedGame? GetGameAtIndex(int index)
    {
        return index >= 0 && index < hostedGames.Count ? hostedGames[index] : null;
    }

    public int GetGameCount() => hostedGames.Count;

    // --- Helpers ---

    private void RefreshGameList()
    {
        var sortedGames = GetSortedAndFilteredGames().ToList();

        _gameNames.Clear();
        foreach (var game in sortedGames)
            _gameNames.Add(game.RoomName);

        // Update hosted games to match sorted order
        hostedGames.Clear();
        hostedGames.AddRange(sortedGames);

        // Try to preserve selection
        if (SelectedGameIndex >= hostedGames.Count)
            SelectedGameIndex = hostedGames.Count > 0 ? hostedGames.Count - 1 : -1;
    }

    private IEnumerable<GenericHostedGame> GetSortedAndFilteredGames()
    {
        var sortedGames = GetSortedGames();
        return gameMatchesFilter == null ? sortedGames : sortedGames.Where(hg => gameMatchesFilter(hg));
    }

    private IEnumerable<GenericHostedGame> GetSortedGames()
    {
        var sortedGames =
            hostedGames
                .OrderBy(hg => hg.Locked)
                .ThenBy(hg => string.Equals(hg.Game.InternalName, localGameIdentifier, StringComparison.InvariantCultureIgnoreCase))
                .ThenBy(hg => hg.GameVersion != ProgramConstants.GAME_VERSION)
                .ThenBy(hg => hg.Passworded);

        switch ((SortDirection)SelectedSortOptionIndex)
        {
            case SortDirection.Asc:
                sortedGames = sortedGames.ThenBy(hg => hg.RoomName);
                break;
            case SortDirection.Desc:
                sortedGames = sortedGames.ThenByDescending(hg => hg.RoomName);
                break;
        }

        return sortedGames;
    }

    private void RemoveExpiredGames()
    {
        for (int i = hostedGames.Count - 1; i >= 0; i--)
        {
            if (DateTime.Now - hostedGames[i].LastRefreshTime > TimeSpan.FromSeconds(GameLifetimeSeconds))
                hostedGames.RemoveAt(i);
        }
    }

    // --- Timer management ---

    private void StartRefreshTimer()
    {
        StopRefreshTimer();
        refreshTimer = new Timer(GAME_REFRESH_INTERVAL_MS);
        refreshTimer.AutoReset = true;
        refreshTimer.Elapsed += RefreshTimer_Elapsed;
        refreshTimer.Start();
    }

    private void StopRefreshTimer()
    {
        if (refreshTimer != null)
        {
            refreshTimer.Stop();
            refreshTimer.Dispose();
            refreshTimer = null;
        }
    }

    private void RefreshTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        RemoveExpiredGames();
        RefreshGameList();
    }
}

