namespace TQG.Automation.SDK.Utils.Retry;

internal sealed class FixedDelayStrategy(TimeSpan delay) : IDelayStrategy
{
    public Task DelayAsync(int attempt, CancellationToken cancellationToken) => Task.Delay(delay, cancellationToken);
}
