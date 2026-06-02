using AvClientMvvmContract.Multiplayer;

namespace AvClientView.Multiplayer;

public interface IPlayerExtraOptionsPanelView
{
    IPlayerExtraOptionsPanelViewModel? ViewModel { get; set; }
}
