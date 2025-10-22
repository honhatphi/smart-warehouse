namespace TQG.Automation.SDK.Utils.Retry;

internal interface IDelayStrategy
{
    Task DelayAsync(int attempt, CancellationToken cancellationToken);
}

