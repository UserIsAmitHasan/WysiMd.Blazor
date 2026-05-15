namespace WysiMd.Blazor.Utilities;

/// <summary>
/// Delays execution of an async action until a period of inactivity elapses.
/// Thread-safe. Supports runtime-adjustable delay, immediate flush, and cancellation.
/// </summary>
internal sealed class Debouncer : IAsyncDisposable
{
    private readonly Func<int> _getDelay;
    private readonly Func<Task> _action;
    private readonly Action<Exception>? _onException;
    private readonly System.Timers.Timer _timer;
    private readonly object _lock = new();
    private bool _disposed;
    private bool _scheduled;

    /// <summary>True when a debounced call is pending and has not yet fired.</summary>
    public bool IsScheduled
    {
        get { lock (_lock) return _scheduled; }
    }

    /// <param name="getDelay">Returns the current debounce delay in ms. Called on every Schedule(). Return 0 to fire immediately.</param>
    /// <param name="action">The async action to debounce.</param>
    /// <param name="onException">Optional handler for exceptions thrown by the action.</param>
    public Debouncer(Func<int> getDelay, Func<Task> action, Action<Exception>? onException = null)
    {
        _getDelay = getDelay;
        _action = action;
        _onException = onException;

        _timer = new System.Timers.Timer { AutoReset = false };
        _timer.Elapsed += OnElapsed;
    }

    /// <summary>
    /// Schedules the action. Resets the countdown if already scheduled.
    /// If delay is 0, fires immediately without using the timer.
    /// </summary>
    public void Schedule()
    {
        if (_disposed) return;

        int delay = _getDelay();
        if (delay <= 0)
        {
            _ = FireAsync();
            return;
        }

        lock (_lock)
        {
            _timer.Stop();
            _timer.Interval = delay;
            _scheduled = true;
            _timer.Start();
        }
    }

    /// <summary>
    /// Cancels a pending scheduled call without firing the action.
    /// </summary>
    public void Cancel()
    {
        lock (_lock)
        {
            _timer.Stop();
            _scheduled = false;
        }
    }

    /// <summary>
    /// Cancels the pending scheduled call and fires the action immediately.
    /// No-op if nothing is scheduled.
    /// </summary>
    public async Task FlushAsync()
    {
        bool wasPending;
        lock (_lock)
        {
            _timer.Stop();
            wasPending = _scheduled;
            _scheduled = false;
        }

        if (wasPending)
            await FireAsync();
    }

    private void OnElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_disposed) return;
        lock (_lock) _scheduled = false;
        _ = FireAsync();
    }

    private async Task FireAsync()
    {
        try
        {
            await _action();
        }
        catch (Exception ex)
        {
            _onException?.Invoke(ex);
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (_lock)
        {
            if (_disposed) return ValueTask.CompletedTask;
            _disposed = true;
            _timer.Stop();
            _timer.Elapsed -= OnElapsed;
            _timer.Dispose();
        }
        return ValueTask.CompletedTask;
    }
}
