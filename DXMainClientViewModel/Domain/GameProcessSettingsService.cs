namespace DXMainClientViewModel.Domain
{
    /// <summary>
    /// Simple implementation of IGameProcessSettingsService.
    /// Stores UseQres and SingleCoreAffinity settings.
    /// </summary>
    public class GameProcessSettingsService : IGameProcessSettingsService
    {
        public bool UseQres { get; set; }
        public bool SingleCoreAffinity { get; set; }
    }
}
