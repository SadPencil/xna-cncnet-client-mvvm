using System;

using Avalonia.Threading;

using AvClientMvvmContract.ViewServices;

namespace AvClientView.Services;

/// <summary>
/// Avalonia implementation of IUIThreadMarshaller.
/// Dispatches delegates to the Avalonia UI thread.
/// </summary>
public class AvaloniaUIThreadMarshaller : IUIThreadMarshaller
{
    public void AddCallback(Delegate d, params object[] args)
    {
        Dispatcher.UIThread.Post(() => d.DynamicInvoke(args));
    }
}
