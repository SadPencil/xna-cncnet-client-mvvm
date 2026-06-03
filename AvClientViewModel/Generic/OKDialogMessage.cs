using AvClientMvvmContract.Generic;

namespace AvClientViewModel.Generic;

internal sealed class OKDialogMessage : IOKDialogMessage
{
    public string Title { get; }
    public string Message { get; }

    public OKDialogMessage(string title, string message)
    {
        Title = title;
        Message = message;
    }
}
