
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ClientCore;
using ClientCore.Extensions;

using ClientUpdater;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic.OptionPanels;

/// <summary>
/// ViewModel for the updater options panel.
/// Contains all business logic from UpdaterOptionsPanel.cs except XNA UI rendering.
/// Self-sufficient: handles confirmation via observable properties.
/// </summary>
public partial class UpdaterOptionsPanelViewModel : ObservableObject, IUpdaterOptionsPanelViewModel
{
    private readonly UserINISettings iniSettings;

    // --- Observable state ---

    [ObservableProperty]
    private int _selectedUpdateServerIndex = -1;

    [ObservableProperty]
    private bool _checkForUpdatesAutomatically;

    [ObservableProperty]
    private bool _isForceUpdateEnabled = true;

    // --- Confirmation dialog state ---

    [ObservableProperty]
    private bool _isConfirmationVisible;

    [ObservableProperty]
    private string _confirmationMessage = string.Empty;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _updateServerNames = new();
    public IReadOnlyList<string> UpdateServerNames => _updateServerNames;

    // --- Domain events (on concrete class only, not on interface) ---

    public event EventHandler? ForceUpdateRequested;

    // --- Constructor ---

    public UpdaterOptionsPanelViewModel(UserINISettings iniSettings)
    {
        this.iniSettings = iniSettings;
    }

    // --- Commands ---

    [RelayCommand]
    private void MoveServerUp()
    {
        if (SelectedUpdateServerIndex < 1 || Updater.UpdateMirrors == null)
            return;

        int index = SelectedUpdateServerIndex;

        // Swap in the display list
        string tmp = _updateServerNames[index - 1];
        _updateServerNames[index - 1] = _updateServerNames[index];
        _updateServerNames[index] = tmp;

        SelectedUpdateServerIndex = index - 1;

        Updater.MoveMirrorUp(index);
    }

    [RelayCommand]
    private void MoveServerDown()
    {
        if (Updater.UpdateMirrors == null)
            return;

        int index = SelectedUpdateServerIndex;
        if (index > _updateServerNames.Count - 2 || index < 0)
            return;

        // Swap in the display list
        string tmp = _updateServerNames[index + 1];
        _updateServerNames[index + 1] = _updateServerNames[index];
        _updateServerNames[index] = tmp;

        SelectedUpdateServerIndex = index + 1;

        Updater.MoveMirrorDown(index);
    }

    [RelayCommand]
    private void ForceUpdate()
    {
        string message = ("WARNING: Force update will result in files being re-verified\n" +
            "and re-downloaded. While this may fix problems with game\n" +
            "files, this also may delete some custom modifications\n" +
            "made to this installation. Use at your own risk!\n\n" +
            "If you proceed, the options window will close and the\n" +
            "client will proceed to checking for updates.\n\n" +
            "Do you really want to force update?").L10N("Client:DTAConfig:ForceUpdateConfirmText") + "\n";

        ConfirmationMessage = message;
        IsConfirmationVisible = true;
    }

    [RelayCommand]
    private void ConfirmYes()
    {
        IsConfirmationVisible = false;
        Updater.ClearVersionInfo();
        ForceUpdateRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ConfirmNo()
    {
        IsConfirmationVisible = false;
    }

    [RelayCommand]
    private void LoadSettings()
    {
        _updateServerNames.Clear();

        if (Updater.UpdateMirrors != null)
        {
            foreach (var mirror in Updater.UpdateMirrors)
            {
                string name = mirror.Name.L10N($"INI:UpdateMirrors:{mirror.Name}:Name");
                string location = mirror.Location.L10N($"INI:UpdateMirrors:{mirror.Name}:Location");

                _updateServerNames.Add(name +
                    (!string.IsNullOrEmpty(location)
                        ? $" ({location})"
                        : string.Empty));
            }
        }

        CheckForUpdatesAutomatically = iniSettings.CheckForUpdates;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        iniSettings.CheckForUpdates.Value = CheckForUpdatesAutomatically;

        iniSettings.SettingsIni.EraseSectionKeys("DownloadMirrors");

        if (Updater.UpdateMirrors != null)
        {
            int id = 0;
            foreach (UpdateMirror um in Updater.UpdateMirrors)
            {
                iniSettings.SettingsIni.SetStringValue("DownloadMirrors", id.ToString(), um.Name);
                id++;
            }
        }
    }
}
