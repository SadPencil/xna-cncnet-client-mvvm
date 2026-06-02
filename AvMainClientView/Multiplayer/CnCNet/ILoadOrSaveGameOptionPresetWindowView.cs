using AvMainClientMvvmContract.Multiplayer.CnCNet;

namespace AvMainClientView.Multiplayer.CnCNet;

public interface ILoadOrSaveGameOptionPresetWindowView
{
    ILoadOrSaveGameOptionPresetWindowViewModel? ViewModel { get; set; }
}
