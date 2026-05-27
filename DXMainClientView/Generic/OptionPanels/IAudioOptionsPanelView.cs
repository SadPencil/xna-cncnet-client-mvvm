#nullable enable

namespace DXMainClientView;

public interface IAudioOptionsPanelView
{
    void SetScoreVolume(int value);
    void SetSoundVolume(int value);
    void SetVoiceVolume(int value);
    void SetClientVolume(int value);
    void SetShuffleEnabled(bool enabled);
    void SetMainMenuMusicEnabled(bool enabled);
    void SetStopMusicOnMenuEnabled(bool enabled);
    void SetLobbySoundEnabled(bool enabled);
    void RefreshPanel();
}
