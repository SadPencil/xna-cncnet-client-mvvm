using AvMainClientMvvmContract.Multiplayer.CnCNet;

namespace AvMainClientView.Multiplayer.CnCNet;

public interface ICnCNetLoginWindowView
{
    ICnCNetLoginWindowViewModel? ViewModel { get; set; }
}
