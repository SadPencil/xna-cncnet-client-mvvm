#nullable enable
using System.ComponentModel;
using System.Threading.Tasks;

namespace DXMainClientViewModel;

public interface ILoadingScreenViewModel : INotifyPropertyChanged
{
    string StatusText { get; }
    string CurrentTaskText { get; }
    int ProgressPercentage { get; }
    bool IsIndeterminate { get; }

    Task InitializeAsync();
}
