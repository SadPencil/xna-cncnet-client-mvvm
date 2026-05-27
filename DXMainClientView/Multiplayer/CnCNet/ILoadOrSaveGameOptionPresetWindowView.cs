#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface ILoadOrSaveGameOptionPresetWindowView
{
    event Action? ConfirmRequested;
    event Action? DeleteRequested;
    event Action? CancelRequested;

    void ShowLoadMode();
    void ShowSaveMode();
    void Hide();
    void SetPresetNames(IEnumerable<string> presetNames);
    void SetSelectedPresetName(string presetName);
    void SetEnteredPresetName(string presetName);
    void SetConfirmEnabled(bool enabled);
    void SetDeleteEnabled(bool enabled);
    void ShowValidationError(string errorMessage);
}
