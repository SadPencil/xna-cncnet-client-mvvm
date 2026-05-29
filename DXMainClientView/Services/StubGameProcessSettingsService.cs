using DXMainClientViewModel.Domain;

namespace DXMainClientView.Services;

/// <summary>
/// Stub implementation of IGameProcessSettingsService for the View layer.
/// </summary>
public class StubGameProcessSettingsService : IGameProcessSettingsService
{
    public bool UseQres { get; set; }
    public bool SingleCoreAffinity { get; set; }
}
