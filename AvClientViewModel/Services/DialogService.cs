using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AvClientMvvmContract.Messages;

using CommunityToolkit.Mvvm.Messaging;

namespace AvClientViewModel.Services
{
    public class DialogService
    {
        public async Task ShowOKDialog(string title, string message)
        {
            _ = await WeakReferenceMessenger.Default.Send(new OKDialogAsyncRequestMessage(title, message));
        }

        public async Task<bool> ShowYesNoDialog(string title, string message)
        {
            var msg = new YesNoDialogAsyncRequestMessage(title, message);
            YesNoDialogResult result = await WeakReferenceMessenger.Default.Send(msg);
            return result.Result;
        }
    }
}
