#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IMapPreviewBoxView
{
    IMapPreviewBoxViewModel? ViewModel { get; set; }
}
