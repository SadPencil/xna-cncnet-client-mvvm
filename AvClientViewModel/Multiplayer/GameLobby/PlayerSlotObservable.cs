using System.Collections.Generic;
using System.Collections.ObjectModel;

using AvClientMvvmContract.Multiplayer.GameLobby;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Multiplayer.GameLobby;

/// <summary>
/// Observable state for a single player slot (dropdowns for name, side, color, start, team).
/// </summary>
public class PlayerSlotObservable : ObservableObject, IPlayerSlotObservable
{
    private string _playerName = string.Empty;
    public string PlayerName
    {
        get => _playerName;
        set => SetProperty(ref _playerName, value);
    }

    private ObservableCollection<string> _nameOptions = new ObservableCollection<string>();
    public ObservableCollection<string> NameOptions
    {
        get => _nameOptions;
        set => SetProperty(ref _nameOptions, value);
    }

    private int _selectedNameIndex;
    public int SelectedNameIndex
    {
        get => _selectedNameIndex;
        set => SetProperty(ref _selectedNameIndex, value);
    }

    private bool _isNameDropdownEnabled;
    public bool IsNameDropdownEnabled
    {
        get => _isNameDropdownEnabled;
        set => SetProperty(ref _isNameDropdownEnabled, value);
    }

    private ObservableCollection<string> _sideOptions = new ObservableCollection<string>();
    public ObservableCollection<string> SideOptions
    {
        get => _sideOptions;
        set => SetProperty(ref _sideOptions, value);
    }

    private int _selectedSideIndex;
    public int SelectedSideIndex
    {
        get => _selectedSideIndex;
        set => SetProperty(ref _selectedSideIndex, value);
    }

    private bool _isSideDropdownEnabled;
    public bool IsSideDropdownEnabled
    {
        get => _isSideDropdownEnabled;
        set => SetProperty(ref _isSideDropdownEnabled, value);
    }

    private ObservableCollection<bool> _sideSelectable = new ObservableCollection<bool>();
    /// <summary>
    /// Per-item selectability for side options.
    /// </summary>
    public ObservableCollection<bool> SideSelectable
    {
        get => _sideSelectable;
        set => SetProperty(ref _sideSelectable, value);
    }

    private ObservableCollection<string> _colorOptions = new ObservableCollection<string>();
    public ObservableCollection<string> ColorOptions
    {
        get => _colorOptions;
        set => SetProperty(ref _colorOptions, value);
    }

    private int _selectedColorIndex;
    public int SelectedColorIndex
    {
        get => _selectedColorIndex;
        set => SetProperty(ref _selectedColorIndex, value);
    }

    private bool _isColorDropdownEnabled;
    public bool IsColorDropdownEnabled
    {
        get => _isColorDropdownEnabled;
        set => SetProperty(ref _isColorDropdownEnabled, value);
    }

    private ObservableCollection<bool> _colorSelectable = new ObservableCollection<bool>();
    /// <summary>
    /// Per-item selectability for color options.
    /// </summary>
    public ObservableCollection<bool> ColorSelectable
    {
        get => _colorSelectable;
        set => SetProperty(ref _colorSelectable, value);
    }

    private ObservableCollection<string> _startOptions = new ObservableCollection<string>();
    public ObservableCollection<string> StartOptions
    {
        get => _startOptions;
        set => SetProperty(ref _startOptions, value);
    }

    private int _selectedStartIndex;
    public int SelectedStartIndex
    {
        get => _selectedStartIndex;
        set => SetProperty(ref _selectedStartIndex, value);
    }

    private bool _isStartDropdownEnabled;
    public bool IsStartDropdownEnabled
    {
        get => _isStartDropdownEnabled;
        set => SetProperty(ref _isStartDropdownEnabled, value);
    }

    private ObservableCollection<bool> _startSelectable = new ObservableCollection<bool>();
    /// <summary>
    /// Per-item selectability for start location options.
    /// </summary>
    public ObservableCollection<bool> StartSelectable
    {
        get => _startSelectable;
        set => SetProperty(ref _startSelectable, value);
    }

    private ObservableCollection<string> _teamOptions = new ObservableCollection<string>();
    public ObservableCollection<string> TeamOptions
    {
        get => _teamOptions;
        set => SetProperty(ref _teamOptions, value);
    }

    private int _selectedTeamIndex;
    public int SelectedTeamIndex
    {
        get => _selectedTeamIndex;
        set => SetProperty(ref _selectedTeamIndex, value);
    }

    private bool _isTeamDropdownEnabled;
    public bool IsTeamDropdownEnabled
    {
        get => _isTeamDropdownEnabled;
        set => SetProperty(ref _isTeamDropdownEnabled, value);
    }
}
