namespace TQG.Automation.SDK.Utils.Retry;

internal static class RetryPolicy
{
    /// <summary>
    /// Thực thi một thao tác async với chiến lược retry + delay. Ném exception cuối cùng nếu tất cả các lần thử đều thất bại.
    /// </summary>
    public static async Task ExecuteAsync(Func<Task> action, int maxAttempts, IDelayStrategy delay, ILogger logger, string operationName, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        Exception? last = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await action().ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                last = ex;
                logger.LogWarning($"{operationName} attempt {attempt}/{maxAttempts} failed: {ex.Message}");
                if (attempt == maxAttempts) break;
                await delay.DelayAsync(attempt, ct).ConfigureAwait(false);
            }
        }
        throw last!;
    }
}