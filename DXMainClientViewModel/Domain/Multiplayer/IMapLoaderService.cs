using System.Collections.Generic;
using System.Threading.Tasks;

namespace DXMainClientViewModel.Domain.Multiplayer;

public interface IMapLoaderService
{
    bool IsLoading { get; }
    IReadOnlyList<string> GameModeNames { get; }
    IReadOnlyList<string> MapNames { get; }

    Task LoadAsync();
    Task RefreshAsync();
    IReadOnlyList<string> GetMapsForGameMode(string gameModeName);
    string GetMapPreviewPath(string mapName);
}
