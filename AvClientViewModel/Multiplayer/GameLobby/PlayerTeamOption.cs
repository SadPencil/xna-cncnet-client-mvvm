using AvClientMvvmContract.Multiplayer.GameLobby;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Multiplayer.GameLobby;

public partial class PlayerTeamOption : ObservableObject, IPlayerTeam
{
    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}
