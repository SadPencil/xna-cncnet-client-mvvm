namespace AvClientMvvmContract.Generic;

/// <summary>
/// Settings populated by PreStartup before the View initializes.
/// The View reads these instead of UserINISettings directly.
/// </summary>
public static class StartupSettings
{
    /// <summary>
    /// Set by PreStartup from UserINISettings.Instance.BorderlessWindowedClient.
    /// </summary>
    public static bool IsBorderlessClient { get; set; }
}
