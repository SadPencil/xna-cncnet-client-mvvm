using AvClientMvvmContract.Multiplayer.GameLobby;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Multiplayer.GameLobby;

public class PlayerColorOption : ObservableObject, IPlayerColor
{
    private int _index;
    public int Index { get => _index; set => SetProperty(ref _index, value); }

    private string _name = string.Empty;
    public string Name { get => _name; set => SetProperty(ref _name, value); }

    private uint _color;
    public uint Color { get => _color; set => SetProperty(ref _color, value); }

    public override string ToString() => Name;
}
