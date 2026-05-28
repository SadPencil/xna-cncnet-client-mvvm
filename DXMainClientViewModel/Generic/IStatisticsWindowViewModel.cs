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
    string SummaryText { get; }

    IRelayCommand RefreshCommand { get; }
    IRelayCommand ClearStatisticsCommand { get; }
    IRelayCommand ReturnCommand { get; }

    void Initialize();
}
