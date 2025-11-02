using System.Collections.Concurrent;

namespace TQG.Automation.SDK.Services.CommandExecution.Core;

/// <summary>
/// Xử lý thực thi lệnh sử dụng strategy pattern.
/// Tách biệt logic thực thi lệnh khỏi CommandSender để dễ bảo trì hơn.
/// </summary>
internal sealed class CommandExecutor(
    CommandExecutionStrategyFactory strategyFactory, 
    TaskTimeoutConfiguration timeoutConfiguration,
    ILogger? logger = null) : IDisposable
{
    private readonly CommandExecutionStrategyFactory _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
    private readonly TaskTimeoutConfiguration _timeoutConfiguration = timeoutConfiguration ?? throw new ArgumentNullException(nameof(timeoutConfiguration));
    private readonly ConcurrentDictionary<string, (Task PollTask, CancellationTokenSource Cts)> _activePollTasks = new();
    private readonly SemaphoreSlim _cleanupSemaphore = new(1, 1);
    private readonly ILogger _logger = logger ?? NullLogger.Instance;
    private bool _disposed = false;

    public event EventHandler<TaskSucceededEventArgs>? TaskSucceeded;
    public event EventHandler<TaskFailedEventArgs>? TaskFailed;
    public event EventHandler<TaskCancelledEventArgs>? TaskCancelled;

    /// <summary>
    /// Thực thi lệnh bất đồng bộ sử dụng strategy pattern.
    /// </summary>
    /// <param name="task">Transport task cần thực thi.</param>
    /// <param name="deviceId">Mã định danh thiết bị.</param>
    /// <param name="connector">PLC connector.</param>
    /// <param name="profile">Profile của thiết bị.</param>
    /// <returns>Task đại diện cho thao tác bất đồng bộ.</returns>
    /// <exception cref="ArgumentNullException">Ném ra khi tham số là null.</exception>
    /// <exception cref="ArgumentException">Ném ra khi tham số không hợp lệ.</exception>
    public async Task ExecuteCommandAsync(
        TransportTask task,
        string deviceId,
        IPlcConnector connector,
        DeviceProfile profile)
    {
        try
        {
            // Common validation (fail-fast) before selecting strategy.
            ArgumentNullException.ThrowIfNull(task);
            ArgumentException.ThrowIfNullOrEmpty(task.TaskId);
            ArgumentException.ThrowIfNullOrEmpty(deviceId);
            ArgumentNullException.ThrowIfNull(connector);
            ArgumentNullException.ThrowIfNull(profile);

            if (profile.Signals == null)
                throw new ArgumentException("Device profile does not contain signal mapping.", nameof(profile));

            _logger.LogInformation($"Executing command {task.CommandType} for task {task.TaskId} on device {deviceId}");

            // Create strategy from single centralized factory (no scattered switches).
            var strategy = _strategyFactory.CreateStrategy(task.CommandType);

            // Wire-up simplified event forwarding with self-cleanup
            EventHandler<TaskSucceededEventArgs>? onTaskSucceeded = null;
            EventHandler<TaskFailedEventArgs>? onTaskFailed = null;
            EventHandler<TaskCancelledEventArgs>? onTaskCancelled = null;
            
            onTaskSucceeded = (s, e) => 
            {
                try
                {
                    TaskSucceeded?.Invoke(this, e);
                }
                finally
                {
                    // Clean up immediately on completion
                    if (onTaskSucceeded != null) strategy.TaskSucceeded -= onTaskSucceeded;
                    if (onTaskFailed != null) strategy.TaskFailed -= onTaskFailed;
                    if (onTaskCancelled != null) strategy.TaskCancelled -= onTaskCancelled;
                }
            };
            
            onTaskFailed = (s, e) => 
            {
                try
                {
                    TaskFailed?.Invoke(this, e);
                }
                finally
                {
                    // Clean up immediately on completion
                    if (onTaskSucceeded != null) strategy.TaskSucceeded -= onTaskSucceeded;
                    if (onTaskFailed != null) strategy.TaskFailed -= onTaskFailed;
                    if (onTaskCancelled != null) strategy.TaskCancelled -= onTaskCancelled;
                }
            };

            onTaskCancelled = (s, e) => 
            {
                try
                {
                    TaskCancelled?.Invoke(this, e);
                }
                finally
                {
                    // Clean up immediately on completion
                    if (onTaskSucceeded != null) strategy.TaskSucceeded -= onTaskSucceeded;
                    if (onTaskFailed != null) strategy.TaskFailed -= onTaskFailed;
                    if (onTaskCancelled != null) strategy.TaskCancelled -= onTaskCancelled;
                }
            };

            try
            {
                strategy.TaskSucceeded += onTaskSucceeded;
                strategy.TaskFailed += onTaskFailed;
                strategy.TaskCancelled += onTaskCancelled;

                await strategy.TriggerCommandAsync(connector, profile.Signals, task);

                var timeoutMinutes = _timeoutConfiguration.GetTimeoutMinutes(task.CommandType);
                var pollingCts = new CancellationTokenSource();
                var pollTask = strategy.StartPollingAsync(
                    deviceId,
                    task.TaskId,
                    connector,
                    profile.Signals,
                    timeoutMinutes,
                    pollingCts.Token);

                _activePollTasks.TryAdd(task.TaskId, (pollTask, pollingCts));

                _ = pollTask.ContinueWith(async t =>
                {
                    try
                    {
                        await CompleteTaskAsync(task.TaskId);
                    }
                    finally
                    {
                        // Clean up event subscriptions when polling completes (fallback cleanup)
                        if (onTaskSucceeded != null) strategy.TaskSucceeded -= onTaskSucceeded;
                        if (onTaskFailed != null) strategy.TaskFailed -= onTaskFailed;
                        if (onTaskCancelled != null) strategy.TaskCancelled -= onTaskCancelled;
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);

                _logger.LogInformation($"Command execution initiated for task {task.TaskId} on device {deviceId}");
            }
            catch (Exception)
            {
                // Clean up event subscriptions on error (fallback cleanup)
                if (onTaskSucceeded != null) strategy.TaskSucceeded -= onTaskSucceeded;
                if (onTaskFailed != null) strategy.TaskFailed -= onTaskFailed;
                if (onTaskCancelled != null) strategy.TaskCancelled -= onTaskCancelled;
                throw;
            }
        }
        catch (DeviceNotRegisteredException ex)
        {
            _logger.LogError($"Device not registered for task {task.TaskId}", ex);
            TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, task.TaskId, ErrorDetail.DeviceNotRegistered(deviceId)));
            throw;
        }
        catch (TimeoutException tex)
        {
            var timeoutMinutes = _timeoutConfiguration.GetTimeoutMinutes(task.CommandType);
            _logger.LogWarning($"Command timed out for task {task.TaskId} on device {deviceId}: {tex.Message}");
            TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, task.TaskId, ErrorDetail.Timeout(deviceId, task.TaskId, TimeSpan.FromMinutes(timeoutMinutes))));
            throw; // propagate TimeoutException upstream
        }
        catch (PlcConnectionFailedException pex)
        {
            _logger.LogError($"PLC connection failed during command {task.TaskId} on device {deviceId}: {pex.Message}");
            TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, task.TaskId, ErrorDetail.PlcConnectionFailed(deviceId, pex.Message)));
            throw; // propagate PLC connection failure
        }
        catch (Exception ex)
        {
            _logger.LogError($"Command execution failed for task {task.TaskId} on device {deviceId}", ex);
            TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, task.TaskId, ErrorDetail.ExecutionException(task.TaskId, deviceId, ex)));
            throw;
        }
    }

    private async Task CompleteTaskAsync(string taskId)
    {
        await _cleanupSemaphore.WaitAsync();
        try
        {
            if (_activePollTasks.TryRemove(taskId, out var taskInfo))
            {
                taskInfo.Cts.Cancel();
                taskInfo.Cts.Dispose();
            }
            else
            {
                _logger.LogWarning($"Task {taskId} not found in active tasks during completion");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error completing task {taskId}", ex);
        }
        finally
        {
            _cleanupSemaphore.Release();
        }
    }

    public async Task CancelTaskAsync(string taskId)
    {
        await _cleanupSemaphore.WaitAsync();
        try
        {
            if (_activePollTasks.TryGetValue(taskId, out var taskInfo))
            {
                _logger.LogInformation($"Cancelling task {taskId}");
                taskInfo.Cts.Cancel();
            }
            else
            {
                _logger.LogWarning($"Task {taskId} not found in active tasks during cancellation");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error cancelling task {taskId}", ex);
        }
        finally
        {
            _cleanupSemaphore.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _logger.LogInformation($"Disposing CommandExecutor with {_activePollTasks.Count} active tasks");

        foreach (var (_, cts) in _activePollTasks.Values)
        {
            try
            {
                cts.Cancel();
                cts.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error disposing task cancellation token: {ex.Message}");
            }
        }

        _activePollTasks.Clear();
        _cleanupSemaphore.Dispose();
    }
}
