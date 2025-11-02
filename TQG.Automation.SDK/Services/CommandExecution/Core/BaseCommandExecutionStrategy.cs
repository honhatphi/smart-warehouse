namespace TQG.Automation.SDK.Services.CommandExecution.Core;

/// <summary>
/// Triển khai cơ sở cho các strategy thực thi lệnh.
/// Cung cấp chức năng chung cho tất cả các loại lệnh.
/// </summary>
internal abstract class BaseCommandExecutionStrategy(
    DeviceMonitor deviceMonitor,
    IBarcodeHandler barcodeHandler,
    TaskDispatcher taskDispatcher,
    ILogger logger) : ICommandExecutionStrategy
{
    protected readonly DeviceMonitor DeviceMonitor = deviceMonitor ?? throw new ArgumentNullException(nameof(deviceMonitor));
    protected readonly IBarcodeHandler BarcodeHandler = barcodeHandler ?? throw new ArgumentNullException(nameof(barcodeHandler));
    protected readonly TaskDispatcher TaskDispatcher = taskDispatcher ?? throw new ArgumentNullException(nameof(taskDispatcher));
    protected readonly ILogger Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public event EventHandler<TaskSucceededEventArgs>? TaskSucceeded;
    public event EventHandler<TaskFailedEventArgs>? TaskFailed;
    public event EventHandler<TaskCancelledEventArgs>? TaskCancelled;

    public abstract CommandType CommandType { get; }

    public abstract Task TriggerCommandAsync(IPlcConnector connector, SignalMap signals, TransportTask task);

    public abstract Task StartPollingAsync(
        string deviceId,
        string taskId,
        IPlcConnector connector,
        SignalMap signals,
        int timeoutMinutes,
        CancellationToken cancellationToken);

    protected async Task<bool> CheckDeviceErrorsAsync(
        IPlcConnector connector,
        SignalMap signals,
        string deviceId,
        string taskId)
    {
        try
        {
            bool rejected = await connector.ReadAsync<bool>(signals.CommandRejected);
            bool alarm = await connector.ReadAsync<bool>(signals.Alarm);
            bool cancelCommand = await connector.ReadAsync<bool>(signals.CancelCommand);

            if (cancelCommand)
            {
                Logger.LogWarning($"[{CommandType}] Cancel command detected for device {deviceId}, task {taskId}. Removing task from queue.");
                TaskDispatcher.RemoveTask(taskId);
                OnTaskCancelled(deviceId, taskId);
                DeviceMonitor.UpdateDeviceStatus(deviceId, DeviceStatus.Error);
                return true;
            }

            if (rejected || alarm)
            {
                short errorCode = await connector.ReadAsync<short>(signals.ErrorCode);

                Logger.LogError($"[{CommandType}] Alarm detected for device {deviceId}, task {taskId}. Error code: {errorCode}. Waiting for completion or cancel signal.");
                OnTaskFailed(deviceId, taskId, ErrorDetail.RunningFailure(taskId, deviceId, errorCode));
                DeviceMonitor.UpdateDeviceStatus(deviceId, DeviceStatus.Error);
                TaskDispatcher.Pause();
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError($"[{CommandType}] Error handling command status for device {deviceId}, task {taskId}", ex);
            HandlePollingException(deviceId, taskId, ex, "HandleCommandStatus");
            return false;
        }
    }

    protected async Task<bool> CheckCompletionAsync(
        IPlcConnector connector,
        SignalMap signals,
        string deviceId,
        string taskId,
        string completeSignal)
    {
        try
        {
            // Đọc tất cả các tín hiệu cần thiết
            bool alarm = await connector.ReadAsync<bool>(signals.Alarm);
            bool complete = await connector.ReadAsync<bool>(completeSignal);
            bool cancelCommand = await connector.ReadAsync<bool>(signals.CancelCommand);

            // Kiểm tra nếu có Complete hoặc Alarm
            if (complete || alarm)
            {
                short errorCode = await connector.ReadAsync<short>(signals.ErrorCode);

                // Trường hợp 1: Complete được bật (không có alarm)
                if (complete && !alarm)
                {
                    await Task.Delay(6000);

                    Logger.LogInformation($"[{CommandType}] Task completed successfully for device {deviceId}, task {taskId}. Removing task from queue.");
                    TaskDispatcher.RemoveTask(taskId);
                    OnTaskSucceeded(deviceId, taskId);
                    DeviceMonitor.UpdateDeviceStatus(deviceId, DeviceStatus.Idle);
                    return true;
                }

                // Trường hợp 2: Có Alarm - cần đợi Complete hoặc CancelCommand
                if (alarm)
                {
                    Logger.LogError($"[{CommandType}] Task completed with alarm for device {deviceId}, task {taskId}. Error code: {errorCode}. Waiting for completion or cancel signal.");
                    TaskDispatcher.RemoveTask(taskId);
                    OnTaskFailed(deviceId, taskId, ErrorDetail.RunningFailure(taskId, deviceId, errorCode));
                    DeviceMonitor.UpdateDeviceStatus(deviceId, DeviceStatus.Error);

                    // Polling liên tục để chờ Complete hoặc CancelCommand
                    Logger.LogInformation($"[{CommandType}] Starting alarm resolution polling for device {deviceId}, task {taskId}");
                    
                    using var alarmTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
                    var alarmStartTime = DateTime.UtcNow;
                    var alarmTimeout = TimeSpan.FromMinutes(30); // Timeout cho việc xử lý alarm

                    while (DateTime.UtcNow - alarmStartTime < alarmTimeout)
                    {
                        await alarmTimer.WaitForNextTickAsync();

                        bool alarmComplete = await connector.ReadAsync<bool>(completeSignal);
                        bool alarmCancel = await connector.ReadAsync<bool>(signals.CancelCommand);

                        // Kiểm tra CancelCommand
                        if (alarmCancel)
                        {
                            Logger.LogWarning($"[{CommandType}] Cancel command detected during alarm for device {deviceId}, task {taskId}.");
                            OnTaskCancelled(deviceId, taskId);
                            DeviceMonitor.UpdateDeviceStatus(deviceId, DeviceStatus.Error);
                            return true;
                        }

                        // Kiểm tra Complete
                        if (alarmComplete)
                        {
                            await Task.Delay(6000);

                            Logger.LogInformation($"[{CommandType}] Task completed after alarm resolution for device {deviceId}, task {taskId}.");
                            OnTaskSucceeded(deviceId, taskId);
                            DeviceMonitor.UpdateDeviceStatus(deviceId, DeviceStatus.Idle);
                            return true;
                        }
                    }

                    // Timeout khi đợi xử lý alarm
                    Logger.LogError($"[{CommandType}] Alarm resolution timeout for device {deviceId}, task {taskId} after {alarmTimeout.TotalMinutes} minutes.");
                    return true;
                }
            }

            // Không có Complete hay Alarm, tiếp tục polling
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError($"[{CommandType}] Error checking completion for device {deviceId}, task {taskId}", ex);
            HandlePollingException(deviceId, taskId, ex, "CheckCompletion");
            return false;
        }
    }

    protected async Task ExecutePollingLoopAsync(
        string deviceId,
        string taskId,
        int timeoutMinutes,
        Func<Task<bool>> checkCompletion,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation($"[{CommandType}] Starting polling loop for device {deviceId}, task {taskId} with timeout {timeoutMinutes} minutes");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromMinutes(timeoutMinutes);

            while (DateTime.UtcNow - startTime < timeout && !cancellationToken.IsCancellationRequested)
            {
                if (!await timer.WaitForNextTickAsync(cancellationToken)) break;

                if (await checkCompletion())
                {
                    break;
                }
            }

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed >= timeout)
            {
                Logger.LogWarning($"[{CommandType}] Polling timeout reached for device {deviceId}, task {taskId} after {elapsed.TotalMinutes:F1} minutes. Removing task from queue.");
                TaskDispatcher.RemoveTask(taskId);
                OnTaskFailed(deviceId, taskId, ErrorDetail.Timeout(deviceId, taskId, elapsed));
                return;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Logger.LogError($"[{CommandType}] Polling exception for device {deviceId}, task {taskId}", ex);
            HandlePollingException(deviceId, taskId, ex, CommandType.ToString());
            throw;
        }
    }

    protected void OnTaskSucceeded(string deviceId, string taskId)
    {
        Logger.LogInformation($"[{CommandType}] Task succeeded event raised for device {deviceId}, task {taskId}");
        TaskSucceeded?.Invoke(this, new TaskSucceededEventArgs(deviceId, taskId));
    }

    protected void OnTaskFailed(string deviceId, string taskId, ErrorDetail error)
    {
        Logger.LogError($"[{CommandType}] Task failed event raised for device {deviceId}, task {taskId}. Error: {error.ErrorMessage}");
        TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, taskId, error));
    }

    protected void OnTaskCancelled(string deviceId, string taskId)
    {
        Logger.LogWarning($"[{CommandType}] Task cancelled event raised for device {deviceId}, task {taskId}");
        TaskCancelled?.Invoke(this, new TaskCancelledEventArgs(deviceId, taskId));
    }

    private void HandlePollingException(string deviceId, string taskId, Exception ex, string pollType)
    {
        Logger.LogError($"[{CommandType}] Polling exception in {pollType} for device {deviceId}, task {taskId}", ex);
        OnTaskFailed(deviceId, taskId, ErrorDetail.PollingException(pollType, deviceId, taskId, ex));
    }
}
