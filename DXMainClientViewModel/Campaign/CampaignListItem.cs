namespace DXMainClientViewModel.Campaign;

/// <summary>
/// Represents a single item in the campaign list.
/// Provides all data the View needs to render the item.
/// </summary>
public sealed class CampaignListItem
{
    public required string Text { get; init; }
    public bool IsEnabled { get; init; } = true;
    public bool IsHeader { get; init; }
    public bool IsSelectable { get; init; } = true;
    public string? IconPath { get; init; }
}
