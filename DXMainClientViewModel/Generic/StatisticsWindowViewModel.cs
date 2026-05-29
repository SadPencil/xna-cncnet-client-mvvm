
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClientCore;
using ClientCore.Extensions;
using ClientCore.Statistics;
using DXMainClientViewModel.Domain.Multiplayer;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DXMainClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the statistics window.
    /// Handles statistics reading, filtering, and calculation.
    /// </summary>
    public partial class StatisticsWindowViewModel : ObservableObject, IStatisticsWindowViewModel
    {
        private readonly MapLoader mapLoader;
        private StatisticsManager sm;
        private List<int> listedGameIndexes = new List<int>();
        private (string Name, string UIName)[] sides;
        private List<MultiplayerColor> mpColors;
        private bool initialized = false;

        [ObservableProperty]
        private List<string> gameModeNames = new();

        [ObservableProperty]
        private int selectedGameModeIndex;

        [ObservableProperty]
        private List<string> gameClassNames = new()
        {
            "All games", "Online games", "Online PvP", "Online Co-Op", "Skirmish"
        };

        [ObservableProperty]
        private int selectedGameClassIndex;

        [ObservableProperty]
        private bool includeSpectatedGames = true;

        [ObservableProperty]
        private List<string> statisticEntrySummaries = new();

        [ObservableProperty]
        private bool isVisible;

        [ObservableProperty]
        private bool showClearConfirmation;

        /// <summary>
        /// Detailed statistics for the selected game.
        /// </summary>
        [ObservableProperty]
        private List<GamePlayerStatistics> selectedGamePlayers = new();

        /// <summary>
        /// Total statistics values.
        /// </summary>
        [ObservableProperty]
        private TotalStatistics totalStatistics = new();

        public StatisticsWindowViewModel(MapLoader mapLoader)
        {
            this.mapLoader = mapLoader;
        }

        partial void OnSelectedGameModeIndexChanged(int value)
        {
            if (initialized)
                ListGames();
        }

        partial void OnSelectedGameClassIndexChanged(int value)
        {
            if (initialized)
                ListGames();
        }

        partial void OnIncludeSpectatedGamesChanged(bool value)
        {
            if (initialized)
                ListGames();
        }

        partial void OnIsVisibleChanged(bool value)
        {
            if (value && initialized)
                ListGames();
        }

        public void Initialize()
        {
            sm = StatisticsManager.Instance;

            sides = ClientConfiguration.Instance.Sides.Split(',')
                .Select(s => (Name: s, UIName: s.L10N($"INI:Sides:{s}"))).ToArray();

            mpColors = MultiplayerColor.LoadColors();

            ReadStatistics();
            ListGameModes();

            StatisticsManager.Instance.GameAdded += OnGameAdded;

            initialized = true;
        }

        private void OnGameAdded(object sender, EventArgs e)
        {
            ListGames();
        }

        [RelayCommand]
        private void Refresh()
        {
            ListGames();
        }

        [RelayCommand]
        private void ClearStatistics()
        {
            ShowClearConfirmation = true;
        }

        [RelayCommand]
        private void ConfirmClear()
        {
            ShowClearConfirmation = false;
            StatisticsManager.Instance.ClearDatabase();
            ReadStatistics();
            ListGameModes();
            ListGames();
        }

        [RelayCommand]
        private void Return()
        {
            IsVisible = false;
        }

        /// <summary>
        /// Selects a game from the list and loads its player statistics.
        /// </summary>
        [RelayCommand]
        private void SelectGame(int listIndex)
        {
            SelectedGamePlayers.Clear();

            if (listIndex < 0 || listIndex >= listedGameIndexes.Count)
                return;

            MatchStatistics ms = sm.GetMatchByIndex(listedGameIndexes[listIndex]);

            var players = new List<GamePlayerStatistics>();

            for (int i = 0; i < ms.GetPlayerCount(); i++)
            {
                PlayerStatistics ps = ms.GetPlayer(i);

                var player = new GamePlayerStatistics
                {
                    Name = ps.IsAI ? ProgramConstants.GetAILevelName(ps.AILevel) : ps.Name,
                    Kills = ps.Kills,
                    Losses = ps.Losses,
                    Economy = ps.Economy,
                    Score = ps.Score,
                    Won = ps.Won,
                    WasSpectator = ps.WasSpectator,
                    IsAI = ps.IsAI,
                    Side = ps.Side,
                    Team = ps.Team,
                    SideName = ps.Side > 0 && ps.Side <= sides.Length ? sides[ps.Side - 1].UIName : "Unknown",
                    ColorR = ps.Color >= 0 && ps.Color < mpColors.Count ? mpColors[ps.Color].R : 255,
                    ColorG = ps.Color >= 0 && ps.Color < mpColors.Count ? mpColors[ps.Color].G : 255,
                    ColorB = ps.Color >= 0 && ps.Color < mpColors.Count ? mpColors[ps.Color].B : 255,
                    SawCompletion = ms.SawCompletion
                };

                players.Add(player);
            }

            SelectedGamePlayers = players.OrderByDescending(p => p.Score).ToList();
        }

        private void ReadStatistics()
        {
            sm.ReadStatistics(ProgramConstants.GamePath);
        }

        private void ListGameModes()
        {
            int gameCount = sm.GetMatchCount();

            var gameModes = new List<string>();

            for (int i = 0; i < gameCount; i++)
            {
                MatchStatistics ms = sm.GetMatchByIndex(i);
                if (!gameModes.Contains(ms.GameMode))
                    gameModes.Add(ms.GameMode);
            }

            gameModes.Sort();

            gameModeOriginalNames = gameModes;

            var names = new List<string> { "All" };
            foreach (string gm in gameModes)
                names.Add(gm.L10N($"INI:GameModes:{gm}:UIName"));

            GameModeNames = names;
            SelectedGameModeIndex = 0;
        }

        private void ListGames()
        {
            if (!IsVisible || !initialized)
                return;

            listedGameIndexes.Clear();

            switch (SelectedGameClassIndex)
            {
                case 0:
                    ListAllGames();
                    break;
                case 1:
                    ListOnlineGames();
                    break;
                case 2:
                    ListPvPGames();
                    break;
                case 3:
                    ListCoOpGames();
                    break;
                case 4:
                    ListSkirmishGames();
                    break;
            }

            listedGameIndexes.Reverse();

            SetTotalStatistics();

            var entries = new List<string>();
            foreach (int gameIndex in listedGameIndexes)
            {
                MatchStatistics ms = sm.GetMatchByIndex(gameIndex);
                string dateTime = ms.DateAndTime.ToShortDateString() + " " + ms.DateAndTime.ToShortTimeString();
                string mapName = mapLoader.TranslatedMapNames.ContainsKey(ms.MapName)
                    ? mapLoader.TranslatedMapNames[ms.MapName]
                    : ms.MapName;
                string gameMode = ms.GameMode.L10N($"INI:GameModes:{ms.GameMode}:UIName");
                string fps = ms.AverageFPS == 0 ? "-" : ms.AverageFPS.ToString();
                string duration = TimeSpan.FromSeconds(ms.LengthInSeconds).ToString();
                string completed = Conversions.BooleanToString(ms.SawCompletion, BooleanStringStyle.YESNO);

                entries.Add($"{dateTime} | {mapName} | {gameMode} | {fps} | {duration} | {completed}");
            }

            StatisticEntrySummaries = entries;
        }

        private void ListAllGames()
        {
            int gameCount = sm.GetMatchCount();

            for (int i = 0; i < gameCount; i++)
            {
                ListGameIndexIfPrerequisitesMet(i);
            }
        }

        private void ListOnlineGames()
        {
            int gameCount = sm.GetMatchCount();

            for (int i = 0; i < gameCount; i++)
            {
                MatchStatistics ms = sm.GetMatchByIndex(i);

                int pCount = ms.GetPlayerCount();
                int hpCount = 0;

                for (int j = 0; j < pCount; j++)
                {
                    PlayerStatistics ps = ms.GetPlayer(j);

                    if (!ps.IsAI)
                    {
                        hpCount++;

                        if (hpCount > 1)
                        {
                            ListGameIndexIfPrerequisitesMet(i);
                            break;
                        }
                    }
                }
            }
        }

        private void ListPvPGames()
        {
            int gameCount = sm.GetMatchCount();

            for (int i = 0; i < gameCount; i++)
            {
                MatchStatistics ms = sm.GetMatchByIndex(i);

                int pCount = ms.GetPlayerCount();
                int pTeam = -1;

                for (int j = 0; j < pCount; j++)
                {
                    PlayerStatistics ps = ms.GetPlayer(j);

                    if (!ps.IsAI && !ps.WasSpectator)
                    {
                        if (pTeam > -1 && (ps.Team != pTeam || ps.Team == 0))
                        {
                            ListGameIndexIfPrerequisitesMet(i);
                            break;
                        }

                        pTeam = ps.Team;
                    }
                }
            }
        }

        private void ListCoOpGames()
        {
            int gameCount = sm.GetMatchCount();

            for (int i = 0; i < gameCount; i++)
            {
                MatchStatistics ms = sm.GetMatchByIndex(i);

                int pCount = ms.GetPlayerCount();
                int hpCount = 0;
                int pTeam = -1;
                bool add = true;

                for (int j = 0; j < pCount; j++)
                {
                    PlayerStatistics ps = ms.GetPlayer(j);

                    if (!ps.IsAI && !ps.WasSpectator)
                    {
                        hpCount++;

                        if (pTeam > -1 && (ps.Team != pTeam || ps.Team == 0))
                        {
                            add = false;
                            break;
                        }

                        pTeam = ps.Team;
                    }
                }

                if (add && hpCount > 1)
                {
                    ListGameIndexIfPrerequisitesMet(i);
                }
            }
        }

        private void ListSkirmishGames()
        {
            int gameCount = sm.GetMatchCount();

            for (int i = 0; i < gameCount; i++)
            {
                MatchStatistics ms = sm.GetMatchByIndex(i);

                int hpCount = 0;
                bool add = true;

                foreach (PlayerStatistics ps in ms.Players)
                {
                    if (!ps.IsAI)
                    {
                        hpCount++;

                        if (hpCount > 1)
                        {
                            add = false;
                            break;
                        }
                    }
                }

                if (add)
                {
                    ListGameIndexIfPrerequisitesMet(i);
                }
            }
        }

        private void ListGameIndexIfPrerequisitesMet(int gameIndex)
        {
            MatchStatistics ms = sm.GetMatchByIndex(gameIndex);

            if (SelectedGameModeIndex != 0)
            {
                // Get the original game mode name from the list
                // GameModeNames[0] is "All", rest are localized names
                // We need to match against the original game mode name
                string selectedGameMode = GetOriginalGameModeName(SelectedGameModeIndex);
                if (selectedGameMode != null && ms.GameMode != selectedGameMode)
                    return;
            }

            PlayerStatistics ps = ms.Players.Find(p => p.IsLocalPlayer);

            if (ps != null && !IncludeSpectatedGames)
            {
                if (ps.WasSpectator)
                    return;
            }

            listedGameIndexes.Add(gameIndex);
        }

        private string GetOriginalGameModeName(int index)
        {
            if (index <= 0 || index > gameModeOriginalNames.Count)
                return null;

            return gameModeOriginalNames[index - 1];
        }

        private List<string> gameModeOriginalNames = new();

        private void SetTotalStatistics()
        {
            int gamesStarted = 0;
            int gamesFinished = 0;
            int gamesPlayed = 0;
            int wins = 0;
            int gameLosses = 0;
            TimeSpan timePlayed = TimeSpan.Zero;
            int numEnemies = 0;
            int numAllies = 0;
            int totalKills = 0;
            int totalLosses = 0;
            int totalScore = 0;
            int totalEconomy = 0;
            int[] sideGameCounts = new int[sides.Length];
            int numEasyAIs = 0;
            int numMediumAIs = 0;
            int numHardAIs = 0;

            foreach (int gameIndex in listedGameIndexes)
            {
                MatchStatistics ms = sm.GetMatchByIndex(gameIndex);

                gamesStarted++;

                if (ms.SawCompletion)
                    gamesFinished++;

                timePlayed += TimeSpan.FromSeconds(ms.LengthInSeconds);

                PlayerStatistics localPlayer = FindLocalPlayer(ms);

                if (localPlayer != null && !localPlayer.WasSpectator)
                {
                    totalKills += localPlayer.Kills;
                    totalLosses += localPlayer.Losses;
                    totalScore += localPlayer.Score;
                    totalEconomy += localPlayer.Economy;

                    if (localPlayer.Side > 0 && localPlayer.Side <= sides.Length)
                        sideGameCounts[localPlayer.Side - 1]++;

                    if (!ms.SawCompletion)
                        continue;

                    if (localPlayer.Won)
                        wins++;
                    else
                        gameLosses++;

                    gamesPlayed++;

                    for (int i = 0; i < ms.GetPlayerCount(); i++)
                    {
                        PlayerStatistics ps = ms.GetPlayer(i);

                        if (!ps.WasSpectator && (!ps.IsLocalPlayer || ps.IsAI))
                        {
                            if (ps.Team == 0 || localPlayer.Team != ps.Team)
                                numEnemies++;
                            else
                                numAllies++;

                            if (ps.IsAI)
                            {
                                if (ps.AILevel == 0)
                                    numEasyAIs++;
                                else if (ps.AILevel == 1)
                                    numMediumAIs++;
                                else
                                    numHardAIs++;
                            }
                        }
                    }
                }
            }

            TotalStatistics = new TotalStatistics
            {
                GamesStarted = gamesStarted,
                GamesFinished = gamesFinished,
                Wins = wins,
                Losses = gameLosses,
                WinLossRatio = gameLosses > 0 ? Math.Round(wins / (double)gameLosses, 2) : 0,
                AverageGameLength = gamesStarted > 0 ? TimeSpan.FromSeconds((int)timePlayed.TotalSeconds / gamesStarted) : TimeSpan.Zero,
                TotalTimePlayed = timePlayed,
                AverageEnemyCount = gamesPlayed > 0 ? Math.Round(numEnemies / (double)gamesPlayed, 2) : 0,
                AverageAllyCount = gamesPlayed > 0 ? Math.Round(numAllies / (double)gamesPlayed, 2) : 0,
                TotalKills = totalKills,
                KillsPerGame = gamesPlayed > 0 ? totalKills / gamesPlayed : 0,
                TotalLosses = totalLosses,
                LossesPerGame = gamesPlayed > 0 ? totalLosses / gamesPlayed : 0,
                KillLossRatio = totalLosses > 0 ? Math.Round(totalKills / (double)totalLosses, 2) : 0,
                TotalScore = totalScore,
                AverageEconomy = gamesPlayed > 0 ? totalEconomy / gamesPlayed : 0,
                FavouriteSide = sides[GetHighestIndex(sideGameCounts)].UIName,
                AverageAILevel = numEasyAIs >= numMediumAIs && numEasyAIs >= numHardAIs ? "Easy"
                    : numMediumAIs >= numEasyAIs && numMediumAIs >= numHardAIs ? "Medium" : "Hard"
            };
        }

        private PlayerStatistics FindLocalPlayer(MatchStatistics ms)
        {
            int pCount = ms.GetPlayerCount();

            for (int pId = 0; pId < pCount; pId++)
            {
                PlayerStatistics ps = ms.GetPlayer(pId);

                if (!ps.IsAI && ps.IsLocalPlayer)
                    return ps;
            }

            return null;
        }

        private int GetHighestIndex(int[] t)
        {
            int highestIndex = -1;
            int highest = Int32.MinValue;

            for (int i = 0; i < t.Length; i++)
            {
                if (t[i] > highest)
                {
                    highest = t[i];
                    highestIndex = i;
                }
            }

            return highestIndex;
        }
    }

    /// <summary>
    /// Represents a player's statistics in a game.
    /// </summary>
    public class GamePlayerStatistics
    {
        public string Name { get; set; }
        public int Kills { get; set; }
        public int Losses { get; set; }
        public int Economy { get; set; }
        public int Score { get; set; }
        public bool Won { get; set; }
        public bool WasSpectator { get; set; }
        public bool IsAI { get; set; }
        public int Side { get; set; }
        public int Team { get; set; }
        public string SideName { get; set; }
        public int ColorR { get; set; }
        public int ColorG { get; set; }
        public int ColorB { get; set; }
        public bool SawCompletion { get; set; }
    }

    /// <summary>
    /// Represents total statistics across all games.
    /// </summary>
    public class TotalStatistics
    {
        public int GamesStarted { get; set; }
        public int GamesFinished { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double WinLossRatio { get; set; }
        public TimeSpan AverageGameLength { get; set; }
        public TimeSpan TotalTimePlayed { get; set; }
        public double AverageEnemyCount { get; set; }
        public double AverageAllyCount { get; set; }
        public int TotalKills { get; set; }
        public int KillsPerGame { get; set; }
        public int TotalLosses { get; set; }
        public int LossesPerGame { get; set; }
        public double KillLossRatio { get; set; }
        public int TotalScore { get; set; }
        public int AverageEconomy { get; set; }
        public string FavouriteSide { get; set; }
        public string AverageAILevel { get; set; }
    }
}


