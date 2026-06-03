using AvClientMvvmContract.Generic;

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AvClientViewModel.Generic;

internal sealed class YesNoDialogMessage : AsyncRequestMessage<bool>, IYesNoDialogMessage
{
    public string Title { get; }
    public string Message { get; }

    public YesNoDialogMessage(string title, string message)
    {
        Title = title;
        Message = message;
    }
}
