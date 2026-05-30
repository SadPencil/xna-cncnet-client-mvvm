using DXMainClientMvvmContract.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface ICnCNetLoginWindowView
{
    ICnCNetLoginWindowViewModel? ViewModel { get; set; }
}
