namespace TQG.Automation.SDK.Utils;

internal sealed class AsyncLock(int initialCount = 1)
{
    private readonly SemaphoreSlim _semaphore = new(initialCount, initialCount);

    public async Task<IDisposable> LockAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        return new Releaser(_semaphore);
    }

    private sealed class Releaser(SemaphoreSlim toRelease) : IDisposable
    {
        private SemaphoreSlim? _toRelease = toRelease;

        public void Dispose() => Interlocked.Exchange(ref _toRelease, null)?.Release();
    }
}

