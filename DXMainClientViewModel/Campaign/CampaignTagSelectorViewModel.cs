
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ClientCore;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain;

namespace DXMainClientViewModel.Campaign;

/// <summary>
/// ViewModel for the campaign tag selector.
/// Contains all business logic from CampaignTagSelector.cs except XNA UI rendering.
/// </summary>
public partial class CampaignTagSelectorViewModel : ObservableObject, ICampaignTagSelectorViewModel
{
    private readonly IDiscordHandlerService discordHandler;
    private readonly ICampaignGameProcessService gameProcessService;
    private readonly IFileIntegrityService fileIntegrityService;

    // Child ViewModel
    private CampaignSelectorViewModel campaignSelector;

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

    // --- Properties delegated to child ---

    public IReadOnlyDictionary<int, Mission> UniqueIDToMissions => campaignSelector.UniqueIDToMissions;
    public IReadOnlyCollection<Mission> AllMissions => campaignSelector.AllMissions;

    // --- Constructor ---

    public CampaignTagSelectorViewModel(
        IDiscordHandlerService discordHandler,
        ICampaignGameProcessService gameProcessService,
        IFileIntegrityService fileIntegrityService)
    {
        this.discordHandler = discordHandler;
        this.gameProcessService = gameProcessService;
        this.fileIntegrityService = fileIntegrityService;
    }

    // --- Commands ---

    [RelayCommand]
    private void SelectTag()
    {
        if (SelectedTagIndex < 0 || SelectedTagIndex >= _campaignTags.Count)
            return;

        SelectedTagName = _campaignTags[SelectedTagIndex];
        campaignSelector.LoadMissionsWithFilter(new HashSet<string>() { SelectedTagName }, disableCustomMissions: false, disableOfficialMissions: false);
        IsVisible = false;
    }

    [RelayCommand]
    private void ShowAllCampaigns()
    {
        SelectedTagName = null;
        SelectedTagIndex = -1;
        campaignSelector.LoadMissionsWithFilter(null, disableCustomMissions: false, disableOfficialMissions: false);
        IsVisible = false;
    }

    [RelayCommand]
    private void Cancel()
    {
        IsVisible = false;
    }

    // --- Lifecycle ---

    public void Initialize()
    {
        campaignSelector = new CampaignSelectorViewModel(discordHandler, gameProcessService, fileIntegrityService);
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
        if (ClientConfiguration.Instance.CampaignTagSelectorEnabled)
            IsVisible = true;
        else
            campaignSelector.LoadMissionsWithFilter(null, disableCustomMissions: false, disableOfficialMissions: false);
    }
}
// checked
