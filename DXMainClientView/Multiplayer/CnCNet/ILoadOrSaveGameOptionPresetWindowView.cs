using DXMainClientMVVMContract.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface ILoadOrSaveGameOptionPresetWindowView
{
    ILoadOrSaveGameOptionPresetWindowViewModel? ViewModel { get; set; }
}
