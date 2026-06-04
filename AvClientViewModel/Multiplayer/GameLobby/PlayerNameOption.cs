using AvClientMvvmContract.Multiplayer.GameLobby;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Multiplayer.GameLobby;

public class PlayerNameOption : ObservableObject, IPlayerName
{
    private int _index;
    public int Index { get => _index; set => SetProperty(ref _index, value); }

    private string _name = string.Empty;
    public string Name { get => _name; set => SetProperty(ref _name, value); }

    private byte[]? _icon;
    public byte[]? Icon { get => _icon; set => SetProperty(ref _icon, value); }

    private int _latency;
    public int Latency { get => _latency; set => SetProperty(ref _latency, value); }

    public override string ToString() => Name;
}
