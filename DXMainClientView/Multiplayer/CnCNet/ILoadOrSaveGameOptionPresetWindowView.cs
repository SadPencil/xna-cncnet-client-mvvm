#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ILoadOrSaveGameOptionPresetWindowView
{
    ILoadOrSaveGameOptionPresetWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
