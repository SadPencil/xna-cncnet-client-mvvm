using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

public interface ILoadOrSaveGameOptionPresetWindowView
{
    ILoadOrSaveGameOptionPresetWindowViewModel? ViewModel { get; set; }
}
