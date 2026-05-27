#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface ILoadOrSaveGameOptionPresetWindowView
{
    ILoadOrSaveGameOptionPresetWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
