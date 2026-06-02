namespace AvMainClientViewModel.Domain;

/// <summary>
/// Service for checking if game files are in their original state.
/// Abstracts ClientUpdater.Updater for ViewModel use.
/// </summary>
public interface IFileIntegrityService
{
    /// <summary>
    /// Returns true if the file does not exist or is in its original (unmodified) state.
    /// </summary>
    bool IsFileNonexistantOrOriginal(string filePath);
}
