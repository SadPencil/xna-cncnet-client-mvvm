using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AvClientMvvmContract.Messages;

using CommunityToolkit.Mvvm.Messaging;

using Serilog;

namespace AvClientViewModel.Services
{
    public class DialogService
    {
        public async Task ShowOKDialog(string title, string message)
        {
            Log.Information($"Showing OK dialog. Title: {title}, Message: {message}");
            _ = await WeakReferenceMessenger.Default.Send(new OKDialogAsyncRequestMessage(title, message));
        }

        public async Task<bool> ShowYesNoDialog(string title, string message)
        {
            Log.Information($"Showing Yes/No dialog. Title: {title}, Message: {message}");
            var msg = new YesNoDialogAsyncRequestMessage(title, message);
            YesNoDialogResult result = await WeakReferenceMessenger.Default.Send(msg);
            Log.Information($"Yes/No dialog closed. Title: {title}, Message: {message}, Result: {result.Result}");
            return result.Result;
        }
    }
}
