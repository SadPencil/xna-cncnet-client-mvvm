using System.ComponentModel;

namespace AvClientMvvmContract.Campaign;

/// <summary>
/// Read-only view of a campaign list item.
/// </summary>
public interface ICampaignListItem : INotifyPropertyChanged
{
    string Text { get; }
    bool IsEnabled { get; }
    bool IsHeader { get; }
    bool IsSelectable { get; }
    string? IconPath { get; }
    CampaignListItemColor TextColorKind { get; }
}

public enum CampaignListItemColor
{
    Default,
    Disabled,
    Header
}
