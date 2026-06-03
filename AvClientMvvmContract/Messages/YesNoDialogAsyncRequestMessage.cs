using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AvClientMvvmContract.Messages;

public class YesNoDialogAsyncRequestMessage : AsyncRequestMessage<YesNoDialogResult>
{
    public string Title { get; }
    public string Message { get; }

    public YesNoDialogAsyncRequestMessage(string title, string message)
    {
        Title = title;
        Message = message;
    }
}
