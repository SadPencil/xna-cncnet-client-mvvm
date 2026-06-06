using System;

using AvClientMvvmContract.Generic.OptionPanels;
using AvClientMvvmContract.ViewServices;

using ClientCore;
using ClientCore.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvClientViewModel.Generic.OptionPanels;

/// <summary>
/// ViewModel for the audio options panel.
/// Contains all business logic from AudioOptionsPanel.cs except XNA UI rendering.
/// </summary>
public partial class AudioOptionsPanelViewModel : ObservableObject, IAudioOptionsPanelViewModel
{
    private const int VOLUME_SCALE = 10;

    private readonly UserINISettings iniSettings;
    private readonly IClientSoundService clientSoundService;

    // --- Observable state ---

    [ObservableProperty]
    public partial int ScoreVolume { get; set; }

    [ObservableProperty]
    public partial int SoundVolume { get; set; }

    [ObservableProperty]
    public partial int VoiceVolume { get; set; }

    [ObservableProperty]
    public partial int ClientVolume { get; set; }

    [ObservableProperty]
    public partial bool IsScoreShuffleEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsMainMenuMusicEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsStopMusicOnMenuDisabled { get; set; }

    /// <summary>
    /// Derived from IsMainMenuMusicEnabled - defined once, not repeated.
    /// </summary>
    public bool IsStopMusicOnMenuAllowed => IsMainMenuMusicEnabled;

    [ObservableProperty]
    public partial bool IsGameLobbyMessageSoundEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsHostedGameSoundEnabled { get; set; }

    /// <summary>
    /// Indicates whether a restart is required after saving settings.
    /// In the original, base.Save() checks IFileSetting entries for restart needs.
    /// </summary>
    public bool IsRestartRequired { get; private set; }

    // --- Constructor ---

    public AudioOptionsPanelViewModel(UserINISettings iniSettings, IClientSoundService clientSoundService)
    {
        this.iniSettings = iniSettings;
        this.clientSoundService = clientSoundService;
    }

    // --- Commands ---

    [RelayCommand]
    private void LoadSettings()
    {
        ScoreVolume = (int)(iniSettings.ScoreVolume * VOLUME_SCALE);
        SoundVolume = (int)(iniSettings.SoundVolume * VOLUME_SCALE);
        VoiceVolume = (int)(iniSettings.VoiceVolume * VOLUME_SCALE);
        ClientVolume = (int)(iniSettings.ClientVolume * VOLUME_SCALE);
        clientSoundService.SetVolume(ClientVolume / (float)VOLUME_SCALE);

        IsScoreShuffleEnabled = iniSettings.IsScoreShuffle;
        IsMainMenuMusicEnabled = iniSettings.PlayMainMenuMusic;
        IsStopMusicOnMenuDisabled = iniSettings.StopMusicOnMenu;
        IsGameLobbyMessageSoundEnabled = !iniSettings.StopGameLobbyMessageAudio;
        IsHostedGameSoundEnabled = iniSettings.PlaySoundOnGameHosted;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        iniSettings.ScoreVolume.Value = ScoreVolume / (double)VOLUME_SCALE;
        iniSettings.SoundVolume.Value = SoundVolume / (double)VOLUME_SCALE;
        iniSettings.VoiceVolume.Value = VoiceVolume / (double)VOLUME_SCALE;
        iniSettings.ClientVolume.Value = ClientVolume / (double)VOLUME_SCALE;

        iniSettings.IsScoreShuffle.Value = IsScoreShuffleEnabled;
        iniSettings.PlayMainMenuMusic.Value = IsMainMenuMusicEnabled;
        iniSettings.StopMusicOnMenu.Value = IsStopMusicOnMenuDisabled;
        iniSettings.StopGameLobbyMessageAudio.Value = !IsGameLobbyMessageSoundEnabled;
        iniSettings.PlaySoundOnGameHosted.Value = IsHostedGameSoundEnabled;
    }

    // --- Property change handlers ---

    partial void OnClientVolumeChanged(int value)
    {
        clientSoundService.SetVolume(value / (float)VOLUME_SCALE);
    }

    partial void OnIsMainMenuMusicEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(IsStopMusicOnMenuAllowed));
        if (!value)
            IsStopMusicOnMenuDisabled = false;
    }
}


