using DXMainClientMVVMContract.Generic.OptionPanels;

using System;

using ClientCore;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DXMainClientMVVMContract.ViewServices;

namespace DXMainClientViewModel.Generic.OptionPanels;

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
    private int _scoreVolume;

    [ObservableProperty]
    private int _soundVolume;

    [ObservableProperty]
    private int _voiceVolume;

    [ObservableProperty]
    private int _clientVolume;

    [ObservableProperty]
    private bool _isScoreShuffleEnabled;

    [ObservableProperty]
    private bool _isMainMenuMusicEnabled;

    [ObservableProperty]
    private bool _isStopMusicOnMenuDisabled;

    [ObservableProperty]
    private bool _isGameLobbyMessageSoundEnabled;

    [ObservableProperty]
    private bool _isHostedGameSoundEnabled;

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
        IsStopMusicOnMenuDisabled = value;
    }
}


