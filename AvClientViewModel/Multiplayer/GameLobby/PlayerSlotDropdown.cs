using System.Collections.ObjectModel;

using AvClientMvvmContract.Multiplayer.GameLobby;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Multiplayer.GameLobby;

public class PlayerSlotDropdown : ObservableObject, IPlayerSlotDropdown
{
    private ObservableCollection<string> _options = new();
    public ObservableCollection<string> Options
    {
        get => _options;
        set => SetProperty(ref _options, value);
    }

    private ObservableCollection<bool> _selectable = new();
    public ObservableCollection<bool> Selectable
    {
        get => _selectable;
        set => SetProperty(ref _selectable, value);
    }

    private string _selectedOption = string.Empty;
    public string SelectedOption
    {
        get => _selectedOption;
        set => SetProperty(ref _selectedOption, value);
    }

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }
}
