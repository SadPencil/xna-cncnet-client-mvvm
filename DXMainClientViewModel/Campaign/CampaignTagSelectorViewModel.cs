
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
    private CampaignSelectorViewModel? campaignSelector;

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

    private readonly List<bool> _campaignTagEnabled = new();

    // --- Properties delegated to child ---

    public IReadOnlyDictionary<int, IMission> UniqueIDToMissions =>
        campaignSelector?.UniqueIDToMissions ?? new Dictionary<int, IMission>();
    public IReadOnlyCollection<IMission> AllMissions =>
        campaignSelector?.AllMissions ?? Array.Empty<IMission>();

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

        if (!_campaignTagEnabled[SelectedTagIndex])
            return;

        SelectedTagName = _campaignTags[SelectedTagIndex];
        campaignSelector?.LoadMissionsWithFilter(new HashSet<string>() { SelectedTagName }, disableCustomMissions: false, disableOfficialMissions: false);
        IsVisible = false;
    }

    [RelayCommand]
    private void ShowAllCampaigns()
    {
        SelectedTagName = null;
        SelectedTagIndex = -1;
        campaignSelector?.LoadMissionsWithFilter(null, disableCustomMissions: false, disableOfficialMissions: false);
        IsVisible = false;
    }

    [RelayCommand]
    private void Cancel()
    {
        IsVisible = false;
    }

    // --- Lifecycle (called by parent ViewModel, not View) ---

    public void Initialize()
    {
        if (!ClientConfiguration.Instance.CampaignTagSelectorEnabled)
            return;

        campaignSelector = new CampaignSelectorViewModel(discordHandler, gameProcessService, fileIntegrityService, OnReturnRequested);
    }

    /// <summary>
    /// Sets the available campaign tags. Called by the parent ViewModel
    /// after discovering tag names from INI configuration.
    /// </summary>
    public void SetTags(IEnumerable<(string Name, bool IsEnabled)> tags)
    {
        _campaignTags.Clear();
        _campaignTagEnabled.Clear();
        foreach (var (name, isEnabled) in tags)
        {
            _campaignTags.Add(name);
            _campaignTagEnabled.Add(isEnabled);
        }
    }

    /// <summary>
    /// Opens the tag selector panel. If tag selector is disabled,
    /// loads all missions directly into the CampaignSelector.
    /// </summary>
    public void Open()
    {
        if (ClientConfiguration.Instance.CampaignTagSelectorEnabled)
            IsVisible = true;
        else
            campaignSelector?.LoadMissionsWithFilter(null, disableCustomMissions: false, disableOfficialMissions: false);
    }

    private void OnReturnRequested()
    {
        // Equivalent of NoFadeSwitch: hide tag selector, show campaign selector
        IsVisible = false;
    }
}

