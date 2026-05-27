#nullable enable
using System.Threading.Tasks;

namespace DXMainClientViewModel;

public interface ILoadingScreenViewModel
{
    string StatusText { get; }
    string CurrentTaskText { get; }
    int ProgressPercentage { get; }
    bool IsIndeterminate { get; }

    Task InitializeAsync();
}
