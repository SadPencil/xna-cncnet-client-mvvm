namespace AvClientMvvmContract.Messages;

/// <summary>
/// Published when the user toggles the borderless client checkbox in DisplayOptions.
/// MainWindowViewModel receives this to re-evaluate the effective WindowState.
/// </summary>
public sealed class BorderlessClientToggledMessage
{
}
