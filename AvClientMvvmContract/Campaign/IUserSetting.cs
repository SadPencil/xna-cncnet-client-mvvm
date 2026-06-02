namespace AvClientMvvmContract.Campaign;

/// <summary>
/// TODO: remove this interface and re-think the MVVM pattern here
/// </summary>
public interface IUserSetting
{
    bool ResetToDefaultOnGameExit { get; }
    void Save();
    void Load();
    void ResetToDefault();
}
