using AvClientMvvmContract.Multiplayer.GameLobby;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Multiplayer.GameLobby;

public class PlayerStartOption : ObservableObject, IPlayerStart
{
    private int _index;
    public int Index { get => _index; set => SetProperty(ref _index, value); }

    private string _name = string.Empty;
    public string Name { get => _name; set => SetProperty(ref _name, value); }

    public override string ToString() => Name;
}
