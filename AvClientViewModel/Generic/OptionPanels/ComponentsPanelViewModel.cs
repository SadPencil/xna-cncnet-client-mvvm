using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using AvClientMvvmContract.Generic.OptionPanels;
using AvClientMvvmContract.ViewServices;

using ClientCore;
using ClientCore.Extensions;

using ClientUpdater;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Rampastring.Tools;

namespace AvClientViewModel.Generic.OptionPanels;

/// <summary>
/// ViewModel for the components panel.
/// Contains all business logic from ComponentsPanel.cs except XNA UI rendering.
/// Self-sufficient: handles confirmation and notifications via observable properties.
/// </summary>
public partial class ComponentsPanelViewModel : ObservableObject, IComponentsPanelViewModel
{
    private readonly IUIThreadMarshaller uiThreadMarshaller;
    private bool downloadCancelled;
    private CustomComponent? pendingInstallComponent;

    // --- Observable state ---

    [ObservableProperty]
    private int _selectedComponentIndex = -1;

    [ObservableProperty]
    private bool _isBusy;

    // --- Confirmation dialog state ---

    [ObservableProperty]
    private bool _isConfirmationVisible;

    [ObservableProperty]
    private string _confirmationMessage = string.Empty;

    // --- Message box state ---

    [ObservableProperty]
    private bool _isMessageBoxVisible;

    [ObservableProperty]
    private string _messageBoxTitle = string.Empty;

    [ObservableProperty]
    private string _messageBoxMessage = string.Empty;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _componentNames = new();
    public IReadOnlyList<string> ComponentNames => _componentNames;

    private readonly ObservableCollection<string> _componentActionTexts = new();
    public IReadOnlyList<string> ComponentActionTexts => _componentActionTexts;

    private readonly ObservableCollection<string> _componentStatusTexts = new();
    public IReadOnlyList<string> ComponentStatusTexts => _componentStatusTexts;

    // --- Constructor ---

    public ComponentsPanelViewModel(IUIThreadMarshaller uiThreadMarshaller)
    {
        this.uiThreadMarshaller = uiThreadMarshaller;

        Updater.FileIdentifiersUpdated += () => UpdateInstallationButtons();
    }

    // --- Commands ---

    [RelayCommand]
    private async Task InstallSelectedComponent()
    {
        if (SelectedComponentIndex < 0 || Updater.CustomComponents == null)
            return;

        var cc = Updater.CustomComponents[SelectedComponentIndex];
        if (cc.IsBeingDownloaded)
            return;

        FileInfo localFileInfo = SafePath.GetFile(ProgramConstants.GamePath, cc.LocalPath);
        if (localFileInfo.Exists)
            return;

        // Show confirmation dialog
        string message = string.Format(
            ("To enable {0} the Client will need to download the necessary files to your game directory.\n\n" +
            "This will take an additional {1} of disk space, and the download may take some time\n" +
            "depending on your Internet connection speed. The size of the download is {2}.\n\n" +
            "You will not be able to play during the download. Do you wish to continue?").L10N("Client:DTAConfig:UpdateConfirmRequiredText"),
            cc.GUIName, GetSizeString(cc.RemoteSize), GetSizeString(cc.Archived ? cc.RemoteArchiveSize : cc.RemoteSize));

        pendingInstallComponent = cc;
        ConfirmationMessage = message;
        IsConfirmationVisible = true;

        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task UpdateSelectedComponent()
    {
        if (SelectedComponentIndex < 0 || Updater.CustomComponents == null)
            return;

        var cc = Updater.CustomComponents[SelectedComponentIndex];
        if (cc.IsBeingDownloaded)
            return;

        FileInfo localFileInfo = SafePath.GetFile(ProgramConstants.GamePath, cc.LocalPath);
        if (!localFileInfo.Exists || cc.LocalIdentifier == cc.RemoteIdentifier)
            return;

        StartDownload(cc);
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task UninstallSelectedComponent()
    {
        if (SelectedComponentIndex < 0 || Updater.CustomComponents == null)
            return;

        var cc = Updater.CustomComponents[SelectedComponentIndex];
        if (cc.IsBeingDownloaded)
            return;

        FileInfo localFileInfo = SafePath.GetFile(ProgramConstants.GamePath, cc.LocalPath);
        if (localFileInfo.Exists && cc.LocalIdentifier == cc.RemoteIdentifier)
        {
            localFileInfo.IsReadOnly = false;
            localFileInfo.Delete();
            UpdateInstallationButtons();
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task CancelDownloads()
    {
        Logger.Log("Cancelling all custom component downloads.");
        downloadCancelled = true;

        if (Updater.CustomComponents == null)
            return;

        foreach (CustomComponent cc in Updater.CustomComponents)
        {
            if (cc.IsBeingDownloaded)
                cc.StopDownload();
        }

        IsBusy = false;
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void RefreshComponents()
    {
        downloadCancelled = false;
        UpdateInstallationButtons();
    }

    [RelayCommand]
    private void ConfirmYes()
    {
        IsConfirmationVisible = false;

        if (pendingInstallComponent != null)
        {
            StartDownload(pendingInstallComponent);
            pendingInstallComponent = null;
        }
    }

    [RelayCommand]
    private void ConfirmNo()
    {
        IsConfirmationVisible = false;
        pendingInstallComponent = null;
    }

    [RelayCommand]
    private void DismissMessageBox()
    {
        IsMessageBoxVisible = false;
    }

    // --- Public methods ---

    public void Initialize()
    {
        _componentNames.Clear();
        _componentActionTexts.Clear();
        _componentStatusTexts.Clear();

        if (Updater.CustomComponents == null)
            return;

        foreach (CustomComponent c in Updater.CustomComponents)
        {
            _componentNames.Add(c.GUIName);
            _componentActionTexts.Add(GetActionText(c));
            _componentStatusTexts.Add(string.Empty);
        }
    }


    // --- Helpers ---

    private void StartDownload(CustomComponent cc)
    {
        IsBusy = true;
        cc.DownloadFinished += HandleDownloadFinishedCallback;
        cc.DownloadProgressChanged += HandleDownloadProgressChangedCallback;
        cc.DownloadComponent();
    }

    private void HandleDownloadFinishedCallback(CustomComponent c, bool success)
    {
        uiThreadMarshaller.AddCallback(new Action<CustomComponent, bool>(HandleDownloadFinished), c, success);
    }

    private void HandleDownloadProgressChangedCallback(CustomComponent c, int percentage)
    {
        uiThreadMarshaller.AddCallback(new Action<CustomComponent, int>(HandleDownloadProgressChanged), c, percentage);
    }

    private void HandleDownloadProgressChanged(CustomComponent cc, int percentage)
    {
        if (Updater.CustomComponents == null)
            return;

        int index = Updater.CustomComponents.IndexOf(cc);
        if (index < 0 || index >= _componentActionTexts.Count)
            return;

        percentage = Math.Min(percentage, 100);

        if (cc.Archived && percentage == 100)
            _componentActionTexts[index] = "Unpacking...".L10N("Client:DTAConfig:Unpacking");
        else
            _componentActionTexts[index] = "Downloading...".L10N("Client:DTAConfig:Downloading") + " " + percentage + "%";
    }

    private void HandleDownloadFinished(CustomComponent cc, bool success)
    {
        cc.DownloadFinished -= HandleDownloadFinishedCallback;
        cc.DownloadProgressChanged -= HandleDownloadProgressChangedCallback;

        IsBusy = false;

        if (Updater.CustomComponents == null)
            return;

        int index = Updater.CustomComponents.IndexOf(cc);
        if (index < 0 || index >= _componentActionTexts.Count)
            return;

        if (!success)
        {
            if (!downloadCancelled)
            {
                ShowMessageBox(
                    "Optional Component Download Failed".L10N("Client:DTAConfig:OptionalComponentDownloadFailedTitle"),
                    string.Format(("Download of optional component {0} failed.\n" +
                    "See client.log for details.\n\n" +
                    "If this problem continues, please contact your mod's authors for support.").L10N("Client:DTAConfig:OptionalComponentDownloadFailedText"),
                    cc.GUIName));
            }

            _componentActionTexts[index] = GetActionText(cc);
        }
        else
        {
            ShowMessageBox(
                "Download Completed".L10N("Client:DTAConfig:DownloadCompleteTitle"),
                string.Format("Download of optional component {0} completed succesfully.".L10N("Client:DTAConfig:DownloadCompleteText"), cc.GUIName));
            _componentActionTexts[index] = "Uninstall".L10N("Client:DTAConfig:Uninstall");
        }
    }

    private void UpdateInstallationButtons()
    {
        if (Updater.CustomComponents == null)
            return;

        for (int i = 0; i < Updater.CustomComponents.Count; i++)
        {
            if (i >= _componentActionTexts.Count)
                break;

            CustomComponent c = Updater.CustomComponents[i];

            if (!c.Initialized || c.IsBeingDownloaded)
                continue;

            _componentActionTexts[i] = GetActionText(c);
        }
    }

    private static string GetActionText(CustomComponent c)
    {
        if (SafePath.GetFile(ProgramConstants.GamePath, c.LocalPath).Exists)
        {
            if (c.LocalIdentifier != c.RemoteIdentifier)
                return "Update".L10N("Client:DTAConfig:Update") + $" ({GetSizeString(c.RemoteSize)})";
            return "Uninstall".L10N("Client:DTAConfig:Uninstall");
        }

        if (!string.IsNullOrEmpty(c.RemoteIdentifier))
            return "Install".L10N("Client:DTAConfig:Install") + $" ({GetSizeString(c.RemoteSize)})";

        return "Not Available".L10N("Client:DTAConfig:NotAvailable");
    }

    private static string GetSizeString(long size)
    {
        if (size < 1048576)
            return (size / 1024) + " KB";
        return (size / 1048576) + " MB";
    }

    private void ShowMessageBox(string title, string message)
    {
        MessageBoxTitle = title;
        MessageBoxMessage = message;
        IsMessageBoxVisible = true;
    }
}



