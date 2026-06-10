using System.ComponentModel;

using SixLabors.ImageSharp;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// An option in the side dropdown (Random, country-selectors, individual sides).
/// </summary>
public interface IPlayerSide : INotifyPropertyChanged
{
    int Index { get; set; }
    string Name { get; set; }
    Image? Icon { get; set; }
}
