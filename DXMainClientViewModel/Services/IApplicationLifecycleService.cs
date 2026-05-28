using System;

namespace DXMainClientViewModel.Services;

/// <summary>
/// Service that notifies ViewModels about application lifecycle events.
/// </summary>
public interface IApplicationLifecycleService
{
    event EventHandler ApplicationClosing;
}
