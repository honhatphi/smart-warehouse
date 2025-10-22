namespace TQG.Automation.SDK.Utils.Retry;

internal sealed class ExponentialBackoffStrategy(
    TimeSpan initialDelay,
    double factor = 2.0,
    TimeSpan? maxDelay = null) : IDelayStrategy
{
    public Task DelayAsync(int attempt, CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromMilliseconds(initialDelay.TotalMilliseconds * Math.Pow(factor, Math.Max(0, attempt - 1)));
        if (maxDelay is not null && delay > maxDelay) delay = maxDelay.Value;
        return Task.Delay(delay, cancellationToken);
    }
}
