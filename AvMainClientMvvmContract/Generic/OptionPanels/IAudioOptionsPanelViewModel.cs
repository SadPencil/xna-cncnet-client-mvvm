using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvMainClientMvvmContract.Generic.OptionPanels;

public interface IAudioOptionsPanelViewModel : INotifyPropertyChanged
{
    int ScoreVolume { get; set; }
    int SoundVolume { get; set; }
    int VoiceVolume { get; set; }
    int ClientVolume { get; set; }
    bool IsScoreShuffleEnabled { get; set; }
    bool IsMainMenuMusicEnabled { get; set; }
    bool IsStopMusicOnMenuDisabled { get; set; }
    bool IsStopMusicOnMenuAllowed { get; }
    bool IsGameLobbyMessageSoundEnabled { get; set; }
    bool IsHostedGameSoundEnabled { get; set; }

    IRelayCommand LoadSettingsCommand { get; }
    IRelayCommand SaveSettingsCommand { get; }
}
