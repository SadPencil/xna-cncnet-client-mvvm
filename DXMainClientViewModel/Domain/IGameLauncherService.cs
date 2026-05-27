#nullable enable
using System.Threading.Tasks;

namespace DXMainClientViewModel.Domain;

public interface IGameLauncherService
{
    bool IsGameRunning { get; }
    string GameExecutablePath { get; }

    Task LaunchGameAsync(string arguments);
    Task LaunchMapEditorAsync();
    void TerminateGame();
    void ReturnToGame();
}
