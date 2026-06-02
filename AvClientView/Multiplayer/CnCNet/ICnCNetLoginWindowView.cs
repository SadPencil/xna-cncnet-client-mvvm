using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

public interface ICnCNetLoginWindowView
{
    ICnCNetLoginWindowViewModel? ViewModel { get; set; }
}
