using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic;

public interface IStatisticsWindowViewModel : INotifyPropertyChanged
{
    List<string> GameModeNames { get; }
    int SelectedGameModeIndex { get; set; }
    List<string> GameClassNames { get; }
    int SelectedGameClassIndex { get; set; }
    bool IncludeSpectatedGames { get; set; }
    List<string> StatisticEntrySummaries { get; }
    bool IsVisible { get; set; }
    List<GamePlayerStatistics> SelectedGamePlayers { get; }
    TotalStatistics TotalStatistics { get; }
    bool ShowClearConfirmation { get; set; }

    IRelayCommand RefreshCommand { get; }
    IRelayCommand ClearStatisticsCommand { get; }
    IRelayCommand ReturnCommand { get; }
    IRelayCommand<int> SelectGameCommand { get; }
    IRelayCommand ConfirmClearCommand { get; }

    void Initialize();
}
