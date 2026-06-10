using AvClientMvvmContract.Multiplayer.GameLobby;

using CommunityToolkit.Mvvm.ComponentModel;

using SixLabors.ImageSharp;

namespace AvClientViewModel.Multiplayer.GameLobby;

public partial class PlayerNameOption : ObservableObject, IPlayerName
{
    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Image? Icon { get; set; }

    [ObservableProperty]
    public partial int Latency { get; set; }

    public override string ToString() => Name;
}
