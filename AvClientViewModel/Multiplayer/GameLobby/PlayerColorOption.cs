using AvClientMvvmContract.Multiplayer.GameLobby;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Multiplayer.GameLobby;

public partial class PlayerColorOption : ObservableObject, IPlayerColor
{
    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial uint Color { get; set; }

    public override string ToString() => Name;
}
