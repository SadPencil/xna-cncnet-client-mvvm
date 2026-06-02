namespace AvClientMvvmContract.Generic.OptionPanels;

/// <summary>
/// Read-only view of data for a game list item in options.
/// </summary>
public interface IGameListItemData
{
    string InternalName { get; }
    string UIName { get; }
    bool IsLocalGame { get; }
    bool IsFollowed { get; }
}
