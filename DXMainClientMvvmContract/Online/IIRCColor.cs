namespace DXMainClientMvvmContract.Online;

/// <summary>
/// Read-only view of an IRC color definition.
/// </summary>
public interface IIRCColor
{
    string Name { get; }
    bool Selectable { get; }
    int R { get; }
    int G { get; }
    int B { get; }
    int IrcColorId { get; }
}
