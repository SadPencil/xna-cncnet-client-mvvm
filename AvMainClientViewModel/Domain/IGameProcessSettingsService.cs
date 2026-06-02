namespace AvMainClientViewModel.Domain
{
    /// <summary>
    /// Interface for game process settings that were previously set on GameProcessLogic.
    /// </summary>
    public interface IGameProcessSettingsService
    {
        bool UseQres { get; set; }
        bool SingleCoreAffinity { get; set; }
    }
}
