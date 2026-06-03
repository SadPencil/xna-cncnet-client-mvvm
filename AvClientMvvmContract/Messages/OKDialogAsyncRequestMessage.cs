using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AvClientMvvmContract.Messages;

public class OKDialogAsyncRequestMessage : AsyncRequestMessage<OKDialogResult>
{
    public string Title { get; }
    public string Message { get; }

    public OKDialogAsyncRequestMessage(string title, string message)
    {
        Title = title;
        Message = message;
    }
}
