using ClientUpdater;

namespace AvClientViewModel.Domain;

/// <summary>
/// Implementation of IFileIntegrityService.
/// Delegates to ClientUpdater.Updater for file integrity checking.
/// </summary>
public class FileIntegrityService : IFileIntegrityService
{
    public bool IsFileNonexistantOrOriginal(string filePath)
    {
        return Updater.IsFileNonexistantOrOriginal(filePath);
    }
}
