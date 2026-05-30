namespace DXMainClientViewModel.Services;

public interface IPreStartupSystemService
{
    void StartSystemSpecificationsCheck();

    void StartOnlineIdGeneration();

    void WriteInstallPathToRegistryIfNeeded();
}
