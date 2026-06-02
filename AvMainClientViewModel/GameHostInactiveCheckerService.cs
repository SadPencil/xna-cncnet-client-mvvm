using System;
using System.Timers;

using ClientCore;

namespace AvMainClientViewModel;

/// <summary>
/// Service that monitors game host inactivity and raises events when
/// warning or disconnect thresholds are reached.
/// </summary>
public class GameHostInactiveCheckerService : IGameHostInactiveCheckerService
{
    private readonly Timer timer;
    private bool isWarningShown;
    private DateTime startTime;

    private static int WarningSeconds => ClientConfiguration.Instance.InactiveHostWarningMessageSeconds;
    private static int CloseSeconds => ClientConfiguration.Instance.InactiveHostKickSeconds;

    public event EventHandler? CloseEvent;
    public event EventHandler? WarningRequested;

    public GameHostInactiveCheckerService()
    {
        timer = new Timer();
        timer.AutoReset = true;
        timer.Interval = 1000;
        timer.Elapsed += TimerOnElapsed;
    }

    private void TimerOnElapsed(object sender, ElapsedEventArgs e)
    {
        double secondsElapsed = (DateTime.UtcNow - startTime).TotalSeconds;

        if (secondsElapsed > WarningSeconds && !isWarningShown)
        {
            isWarningShown = true;
            WarningRequested?.Invoke(this, EventArgs.Empty);
        }

        if (secondsElapsed > CloseSeconds)
        {
            Stop();
            CloseEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Start()
    {
        Reset();
        timer.Start();
    }

    public void Reset()
    {
        startTime = DateTime.UtcNow;
        isWarningShown = false;
    }

    public void Stop() => timer.Stop();
}
