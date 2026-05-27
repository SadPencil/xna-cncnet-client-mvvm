#nullable enable
using System.ComponentModel;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IStatisticsWindowViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> GameModeNames { get; }
    int SelectedGameModeIndex { get; set; }
    IReadOnlyList<string> GameClassNames { get; }
    int SelectedGameClassIndex { get; set; }
    bool IncludeSpectatedGames { get; set; }
    IReadOnlyList<string> StatisticEntrySummaries { get; }
    string SummaryText { get; }

    IRelayCommand RefreshCommand { get; }
    IRelayCommand ClearStatisticsCommand { get; }
    IRelayCommand ReturnCommand { get; }

    void Initialize();
}
