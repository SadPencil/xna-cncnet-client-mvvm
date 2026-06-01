namespace DXMainClientMvvmContract.Online;

/// <summary>
/// Read-only view of an IRC color definition.
/// </summary>
public interface IIRCColor : IRgb24Color
{
    string Name { get; }
    int IrcColorId { get; }
}
