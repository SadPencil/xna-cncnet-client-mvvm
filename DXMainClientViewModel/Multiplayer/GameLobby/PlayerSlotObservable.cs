using DXMainClientMvvmContract.Multiplayer.GameLobby;

using System.Collections.Generic;

using CommunityToolkit.Mvvm.ComponentModel;

namespace DXMainClientViewModel.Multiplayer.GameLobby;

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

    private IReadOnlyList<string> _nameOptions = System.Array.Empty<string>();
    public IReadOnlyList<string> NameOptions
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

    private IReadOnlyList<string> _sideOptions = System.Array.Empty<string>();
    public IReadOnlyList<string> SideOptions
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

    private IReadOnlyList<bool> _sideSelectable = System.Array.Empty<bool>();
    /// <summary>
    /// Per-item selectability for side options.
    /// </summary>
    public IReadOnlyList<bool> SideSelectable
    {
        get => _sideSelectable;
        set => SetProperty(ref _sideSelectable, value);
    }

    private IReadOnlyList<string> _colorOptions = System.Array.Empty<string>();
    public IReadOnlyList<string> ColorOptions
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

    private IReadOnlyList<bool> _colorSelectable = System.Array.Empty<bool>();
    /// <summary>
    /// Per-item selectability for color options.
    /// </summary>
    public IReadOnlyList<bool> ColorSelectable
    {
        get => _colorSelectable;
        set => SetProperty(ref _colorSelectable, value);
    }

    private IReadOnlyList<string> _startOptions = System.Array.Empty<string>();
    public IReadOnlyList<string> StartOptions
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

    private IReadOnlyList<bool> _startSelectable = System.Array.Empty<bool>();
    /// <summary>
    /// Per-item selectability for start location options.
    /// </summary>
    public IReadOnlyList<bool> StartSelectable
    {
        get => _startSelectable;
        set => SetProperty(ref _startSelectable, value);
    }

    private IReadOnlyList<string> _teamOptions = System.Array.Empty<string>();
    public IReadOnlyList<string> TeamOptions
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
