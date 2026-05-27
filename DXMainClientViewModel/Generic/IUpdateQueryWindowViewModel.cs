#nullable enable
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IUpdateQueryWindowViewModel : INotifyPropertyChanged
{
    string VersionText { get; }
    string UpdateSizeText { get; }
    string ChangelogUrl { get; }

    IRelayCommand AcceptUpdateCommand { get; }
    IRelayCommand DeclineUpdateCommand { get; }
    IRelayCommand OpenChangelogCommand { get; }
}
