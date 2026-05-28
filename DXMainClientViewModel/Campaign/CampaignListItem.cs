namespace DXMainClientViewModel.Campaign;

/// <summary>
/// Data model for a single campaign list item.
/// The View uses this to render the item with appropriate styling.
/// </summary>
public sealed class CampaignListItem
{
    public required string Text { get; init; }
    public bool IsEnabled { get; init; } = true;
    public bool IsHeader { get; init; }
    public bool IsSelectable { get; init; } = true;
    public string? IconPath { get; init; }
    public CampaignListItemColor TextColorKind { get; init; }
}

public enum CampaignListItemColor
{
    Default,
    Disabled,
    Header
}
