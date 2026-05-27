#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface ICnCNetLoginWindowView
{
    ICnCNetLoginWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
