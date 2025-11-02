namespace TQG.Automation.SDK.Services.CommandExecution.Communication;


/// <summary>
/// CommandSender được tối ưu hóa sử dụng Strategy Pattern và nguyên tắc Clean Architecture.
/// Tách biệt các concerns và giảm độ phức tạp để dễ bảo trì hơn.
///
/// Tổng quan luồng dữ liệu (high level):
/// - Các lời gọi SendCommand/SendMultipleCommands đưa TransportTask(s) vào TaskDispatcher.
/// - TaskDispatcher gán tasks cho thiết bị và phát sinh TaskAssigned events.
/// - OnTaskAssigned nhận assignment và gọi CommandExecutor để thực thi
///   lệnh đối với PLC connector của thiết bị. Kết quả được báo cáo lại thông qua
///   TaskSucceeded/TaskFailed events để hoàn thành lifecycle (timeout/validation
///   handlers tham gia ở đây).
///
/// Class này là điểm orchestration cho task -> dispatcher -> PLC execution -> result.
/// </summary>
internal sealed class CommandSender : ICommandSender, IDisposable
{
    private readonly TaskDispatcher _taskDispatcher;
    private readonly CommandExecutor _commandExecutor;
    private readonly ValidationResultHandler _validationHandler;
    private readonly ILogger _logger;
    private bool _disposed = false;

    public event EventHandler<TaskSucceededEventArgs>? TaskSucceeded;
    
    public event EventHandler<TaskFailedEventArgs>? TaskFailed;

    public event EventHandler<TaskCancelledEventArgs>? TaskCancelled;

    /// <summary>
    /// Khởi tạo instance mới của CommandSender với các dependencies được inject.
    /// </summary>
    /// <param name="taskDispatcher">Task dispatcher để quản lý việc phân phối tasks.</param>
    /// <param name="commandExecutor">Command executor để thực thi lệnh trên thiết bị.</param>
    /// <param name="validationHandler">Validation result handler để xử lý kết quả validation.</param>
    /// <param name="logger">Logger để ghi log các thao tác (tùy chọn).</param>
    /// <exception cref="ArgumentNullException">Ném ra khi bất kỳ dependency nào là null.</exception>
    public CommandSender(
        TaskDispatcher taskDispatcher,
        CommandExecutor commandExecutor,
        ValidationResultHandler validationHandler,
        ILogger? logger = null)
    {
        _taskDispatcher = taskDispatcher ?? throw new ArgumentNullException(nameof(taskDispatcher));
        _commandExecutor = commandExecutor ?? throw new ArgumentNullException(nameof(commandExecutor));
        _validationHandler = validationHandler ?? throw new ArgumentNullException(nameof(validationHandler));
        _logger = logger ?? NullLogger.Instance;

        try
        {
            _taskDispatcher.TaskAssigned += OnTaskAssigned;
            _commandExecutor.TaskSucceeded += OnTaskSucceeded;
            _commandExecutor.TaskFailed += OnTaskFailed;
            _commandExecutor.TaskCancelled += OnTaskCancelled;
            _validationHandler.TaskFailed += (s, e) => TaskFailed?.Invoke(this, e);

            _logger.LogInformation("CommandSender initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to initialize CommandSender", ex);
            throw;
        }
    }

    public async Task SendCommand(TransportTask task)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogInformation($"Sending command {task.CommandType} for task {task.TaskId}");

            ValidateTask(task);

            _taskDispatcher.EnqueueTasks([task]);
            await _taskDispatcher.ProcessQueueIfNeeded();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send command {task.CommandType} for task {task.TaskId}", ex);
            throw;
        }
    }

    public async Task SendMultipleCommands(List<TransportTask> tasks)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogInformation($"Sending {tasks.Count} commands");

            ValidateTasks(tasks);

            foreach (var task in tasks)
            {
                ValidateTaskArguments(task);
            }

            _taskDispatcher.EnqueueTasks(tasks);
            await _taskDispatcher.ProcessQueueIfNeeded();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send multiple commands", ex);
            throw;
        }
    }

    public async Task SendValidationResult(
        string deviceId,
        string taskId,
        bool isValid,
        Location? targetLocation,
        Direction direction,
        short gateNumber)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogInformation($"Sending validation result for task {taskId} on device {deviceId}. Valid: {isValid}, Direction: {direction}, Gate: {gateNumber}");

            if (!isValid)
            {
                _logger.LogWarning($"Validation failed for task {taskId} on device {deviceId}");
            }

            await _validationHandler.SendValidationResultAsync(deviceId, taskId, isValid, targetLocation, direction, gateNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send validation result for task {taskId}", ex);
            throw;
        }
    }

    private void OnTaskAssigned(object? sender, TaskAssignedEventArgs args)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation($"Task {args.Task.TaskId} assigned to device {args.DeviceId}, executing command");

                await _commandExecutor.ExecuteCommandAsync(args.Task, args.DeviceId, args.Connector, args.Profile);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to execute command for task {args.Task.TaskId} on device {args.DeviceId}", ex);
            }
        });
    }

    private void OnTaskSucceeded(object? sender, TaskSucceededEventArgs args)
    {
        try
        {
            _logger.LogInformation($"Task {args.TaskId} succeeded on device {args.DeviceId}");

            _taskDispatcher.CompleteTaskAssignment(args.DeviceId, args.TaskId);
            TaskSucceeded?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error handling task success for {args.TaskId}", ex);
        }
    }

    private void OnTaskFailed(object? sender, TaskFailedEventArgs args)
    {
        try
        {
            _logger.LogWarning($"Task {args.TaskId} failed on device {args.DeviceId}: {args.ErrorDetail.ErrorMessage}");

            _taskDispatcher.CompleteTaskAssignment(args.DeviceId, args.TaskId);
            TaskFailed?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error handling task failure for {args.TaskId}", ex);
        }
    }

    private void OnTaskCancelled(object? sender, TaskCancelledEventArgs args)
    {
        try
        {
            _logger.LogWarning($"Task {args.TaskId} cancelled on device {args.DeviceId}");

            _taskDispatcher.CompleteTaskAssignment(args.DeviceId, args.TaskId);
            TaskCancelled?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error handling task cancellation for {args.TaskId}", ex);
        }
    }

    private static void ValidateTask(TransportTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentException.ThrowIfNullOrEmpty(task.TaskId);
    }

    /// <summary>
    /// Validate danh sách transport tasks.
    /// </summary>
    /// <param name="tasks">Danh sách transport tasks cần validate.</param>
    /// <exception cref="ArgumentException">Ném ra khi danh sách là null hoặc rỗng.</exception>
    private static void ValidateTasks(List<TransportTask> tasks)
    {
        if (tasks == null || tasks.Count == 0)
        {
            throw new ArgumentException("Tasks must not be null or empty.", nameof(tasks));
        }
    }

    private static void ValidateTaskArguments(TransportTask task)
    {
        switch (task.CommandType)
        {
            case CommandType.Outbound:
                ArgumentNullException.ThrowIfNull(task.SourceLocation);
                break;
            case CommandType.Transfer:
                ArgumentNullException.ThrowIfNull(task.SourceLocation);
                ArgumentNullException.ThrowIfNull(task.TargetLocation);
                break;
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, nameof(CommandSender));

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _logger.LogInformation("Disposing CommandSender");

        try
        {
            _taskDispatcher.TaskAssigned -= OnTaskAssigned;
            _commandExecutor.TaskSucceeded -= OnTaskSucceeded;
            _commandExecutor.TaskFailed -= OnTaskFailed;
            _commandExecutor.TaskCancelled -= OnTaskCancelled;
            _commandExecutor.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during CommandSender disposal", ex);
        }
    }
}
