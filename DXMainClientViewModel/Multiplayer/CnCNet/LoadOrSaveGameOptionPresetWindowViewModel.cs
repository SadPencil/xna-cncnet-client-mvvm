using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain.Multiplayer;
using DXMainClientViewModel.Online.EventArguments;

namespace DXMainClientViewModel.Multiplayer.CnCNet;


/// <summary>
/// ViewModel for the load or save game option preset window.
/// Contains all business logic from LoadOrSaveGameOptionPresetWindow.cs except XNA UI rendering.
/// </summary>
public partial class LoadOrSaveGameOptionPresetWindowViewModel : ObservableObject, ILoadOrSaveGameOptionPresetWindowViewModel
{
    private static readonly string CreateNewPlaceholder = "[Create New]".L10N("Client:Main:CreateNewPreset");
    private static readonly string SelectPresetPlaceholder = "[Select Preset]".L10N("Client:Main:SelectPreset");

    // --- Observable state ---

    [ObservableProperty]
    private bool _isLoadMode;

    [ObservableProperty]
    private int _selectedPresetIndex;

    [ObservableProperty]
    private string _presetName = string.Empty;

    [ObservableProperty]
    private bool _isWindowVisible;

    [ObservableProperty]
    private bool _isNewPresetNameEnabled;

    [ObservableProperty]
    private bool _isConfirmEnabled;

    [ObservableProperty]
    private bool _isDeleteEnabled;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _presetNames = new();
    public IReadOnlyList<string> PresetNames => _presetNames;

    // --- Events ---

    public event EventHandler<GameOptionPresetEventArgs>? PresetLoaded;
    public event EventHandler<GameOptionPresetEventArgs>? PresetSaved;
    public event EventHandler? Cancelled;

    // --- Commands ---

    [RelayCommand]
    private void Confirm()
    {
        if (SelectedPresetIndex < 0 || SelectedPresetIndex >= _presetNames.Count)
            return;

        string selectedItem = _presetNames[SelectedPresetIndex];

        if (IsLoadMode)
        {
            PresetLoaded?.Invoke(this, new GameOptionPresetEventArgs(selectedItem));
        }
        else
        {
            string name = IsCreateNewSelected ? PresetName : selectedItem;
            PresetSaved?.Invoke(this, new GameOptionPresetEventArgs(name));
        }

        IsWindowVisible = false;
    }

    [RelayCommand]
    private void DeleteSelectedPreset()
    {
        if (SelectedPresetIndex < 0 || SelectedPresetIndex >= _presetNames.Count)
            return;

        string selectedItem = _presetNames[SelectedPresetIndex];
        if (selectedItem == CreateNewPlaceholder || selectedItem == SelectPresetPlaceholder)
            return;

        GameOptionPresets.Instance.DeletePreset(selectedItem);
        _presetNames.Remove(selectedItem);
        SelectedPresetIndex = 0;
        RefreshButtons();
    }

    [RelayCommand]
    private void Cancel()
    {
        IsWindowVisible = false;
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    // --- Public methods ---

    /// <summary>
    /// Shows the window in load or save mode.
    /// </summary>
    public void Show(bool isLoad)
    {
        IsLoadMode = isLoad;
        LoadPresets();

        if (isLoad)
        {
            IsNewPresetNameEnabled = false;
        }
        else
        {
            IsNewPresetNameEnabled = true;
            PresetName = string.Empty;
        }

        SelectedPresetIndex = 0;
        RefreshButtons();
        IsWindowVisible = true;
    }

    // --- Property change handlers ---

    partial void OnSelectedPresetIndexChanged(int value)
    {
        if (!IsLoadMode)
            UpdateNewPresetNameVisibility();
        RefreshButtons();
    }

    partial void OnPresetNameChanged(string value)
    {
        RefreshButtons();
    }

    // --- Helpers ---

    private bool IsCreateNewSelected =>
        SelectedPresetIndex >= 0 && SelectedPresetIndex < _presetNames.Count
        && _presetNames[SelectedPresetIndex] == CreateNewPlaceholder;

    private bool IsSelectPresetSelected =>
        SelectedPresetIndex >= 0 && SelectedPresetIndex < _presetNames.Count
        && _presetNames[SelectedPresetIndex] == SelectPresetPlaceholder;

    private bool IsNewPresetNameFieldEmpty => string.IsNullOrWhiteSpace(PresetName);

    private void LoadPresets()
    {
        _presetNames.Clear();

        if (IsLoadMode)
            _presetNames.Add(SelectPresetPlaceholder);
        else
            _presetNames.Add(CreateNewPlaceholder);

        foreach (string name in GameOptionPresets.Instance.GetPresetNames().OrderBy(n => n))
            _presetNames.Add(name);
    }

    private void UpdateNewPresetNameVisibility()
    {
        if (IsCreateNewSelected)
            IsNewPresetNameEnabled = true;
        else
            IsNewPresetNameEnabled = false;
    }

    private void RefreshButtons()
    {
        if (IsLoadMode)
            IsConfirmEnabled = !IsSelectPresetSelected;
        else
            IsConfirmEnabled = !IsCreateNewSelected || !IsNewPresetNameFieldEmpty;

        IsDeleteEnabled = !IsCreateNewSelected && !IsSelectPresetSelected;
    }
}
