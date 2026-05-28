// checked
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Campaign;

/// <summary>
/// ViewModel for the campaign tag selector.
/// Contains all business logic from CampaignTagSelector.cs except XNA UI rendering.
/// </summary>
public partial class CampaignTagSelectorViewModel : ObservableObject, ICampaignTagSelectorViewModel
{
    // --- Observable state ---

    [ObservableProperty]
    private int _selectedTagIndex = -1;

    [ObservableProperty]
    private string? _selectedTagName;

    [ObservableProperty]
    private bool _isVisible;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _campaignTags = new();
    public IReadOnlyList<string> CampaignTags => _campaignTags;

    // --- Events ---

    /// <summary>
    /// Raised when the user selects a specific tag. The string is the tag name.
    /// </summary>
    public event EventHandler<string?>? TagSelected;

    /// <summary>
    /// Raised when the user requests to show all campaigns (no tag filter).
    /// </summary>
    public event EventHandler? ShowAllCampaignsRequested;

    /// <summary>
    /// Raised when the user cancels the tag selection.
    /// </summary>
    public event EventHandler? Cancelled;

    // --- Commands ---

    [RelayCommand]
    private void SelectTag()
    {
        if (SelectedTagIndex < 0 || SelectedTagIndex >= _campaignTags.Count)
            return;

        SelectedTagName = _campaignTags[SelectedTagIndex];
        IsVisible = false;
        TagSelected?.Invoke(this, SelectedTagName);
    }

    [RelayCommand]
    private void ShowAllCampaigns()
    {
        SelectedTagName = null;
        SelectedTagIndex = -1;
        IsVisible = false;
        ShowAllCampaignsRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        IsVisible = false;
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    // --- Public methods ---

    public void Initialize()
    {
        // Tags are populated by the View from INI configuration via SetTags
    }

    /// <summary>
    /// Sets the available campaign tags. Called by the parent ViewModel
    /// after discovering tag names from INI configuration.
    /// </summary>
    public void SetTags(IEnumerable<string> tagNames)
    {
        _campaignTags.Clear();
        foreach (string tag in tagNames)
            _campaignTags.Add(tag);
    }

    /// <summary>
    /// Opens the tag selector panel.
    /// </summary>
    public void Open()
    {
        IsVisible = true;
    }
}
