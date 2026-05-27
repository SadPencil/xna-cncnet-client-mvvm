#nullable enable

using System.Collections.Generic;

namespace DXMainClientView;

public interface IComponentsPanelView
{
    void SetComponentNames(IEnumerable<string> componentNames);
    void SetComponentButtonText(int componentIndex, string buttonText);
    void SetComponentButtonEnabled(int componentIndex, bool enabled);
    void SetComponentProgress(int componentIndex, int percentage);
    void ShowComponentDownloadPrompt(string componentName, string message);
    void ShowComponentStatus(int componentIndex, string statusText);
    void ClearComponentProgress();
    void CancelAllDownloads();
}
