namespace TQG.Automation.SDK.Utils;

internal static class TimeoutHelper
{
    public static async Task RunWithTimeout(Func<CancellationToken, Task> action, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await action(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds} seconds");
        }
    }

    public static async Task<T> RunWithTimeout<T>(Func<CancellationToken, Task<T>> action, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await action(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds} seconds");
        }
    }
}
