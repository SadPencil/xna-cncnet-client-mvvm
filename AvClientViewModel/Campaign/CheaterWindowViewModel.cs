using AvClientMvvmContract.Campaign;

#nullable enable
using System;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvClientViewModel.Campaign;

public partial class CheaterWindowViewModel : ObservableObject, ICheaterWindowViewModel
{
    public CheaterWindowViewModel(Action onConfirm, Action onCancel)
    {
        ConfirmCommand = new RelayCommand(onConfirm);
        CancelCommand = new RelayCommand(onCancel);
    }

    [ObservableProperty]
    public partial string TitleText { get; set; }  = string.Empty;

    [ObservableProperty]
    public partial string MessageText { get; set; }  = string.Empty;

    public IRelayCommand ConfirmCommand { get; }
    public IRelayCommand CancelCommand { get; }
}


