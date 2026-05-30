using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMvvmContract.Generic;

public interface IUpdateQueryWindowViewModel : INotifyPropertyChanged
{
    string DescriptionText { get; }
    string UpdateSizeText { get; }
    bool IsVisible { get; set; }

    IRelayCommand AcceptCommand { get; }
    IRelayCommand DeclineCommand { get; }
    IRelayCommand ViewChangelogCommand { get; }
}
