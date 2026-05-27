#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface IGameCreationWindowView
{
    IGameCreationWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
