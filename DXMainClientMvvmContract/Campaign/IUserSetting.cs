namespace DXMainClientMvvmContract.Campaign;

/// <summary>
/// Abstracts a user-configurable setting that can be saved/loaded and reset to default.
/// The View creates these from INI and registers them with the ViewModel.
/// </summary>
public interface IUserSetting
{
    bool ResetToDefaultOnGameExit { get; }
    void Save();
    void Load();
    void ResetToDefault();
}
