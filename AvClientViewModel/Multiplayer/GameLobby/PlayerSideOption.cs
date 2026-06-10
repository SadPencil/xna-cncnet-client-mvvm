using AvClientMvvmContract.Multiplayer.GameLobby;

using CommunityToolkit.Mvvm.ComponentModel;

using SixLabors.ImageSharp;

namespace AvClientViewModel.Multiplayer.GameLobby;

public partial class PlayerSideOption : ObservableObject, IPlayerSide
{
    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Image? Icon { get; set; }

    public override string ToString() => Name;
}
