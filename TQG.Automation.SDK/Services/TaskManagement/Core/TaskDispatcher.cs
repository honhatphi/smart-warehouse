using System.Collections.Concurrent;

namespace TQG.Automation.SDK.Services.TaskManagement.Core;

/// <summary>
/// Dịch vụ đã được tái cấu trúc để điều phối và quản lý các nhiệm vụ vận chuyển.
/// Sử dụng các nguyên tắc clean architecture với các mối quan tâm được tách biệt và hiệu suất được cải thiện.
/// </summary>
///
/// Tổng quan luồng dữ liệu (đường dẫn nhiệm vụ):
/// - Các caller bên ngoài đưa TransportTask(s) vào hàng đợi qua CommandSender, được ủy quyền cho EnqueueTasks.
/// - Nhiệm vụ được lưu trong ITaskQueue và xử lý bởi vòng lặp background (ProcessQueueAsync).
/// - Khi một thiết bị có sẵn (DeviceMonitor báo cáo Idle), dispatcher gán nhiệm vụ và
///   phát ra TaskAssigned mà CommandSender đăng ký để thực thi với PLC.
/// - Khi hoàn thành (TaskSucceeded/TaskFailed) dispatcher cập nhật trạng thái gán và có thể
///   kích hoạt xử lý hàng đợi bổ sung. Timeout được xử lý trực tiếp bởi chiến lược thực thi lệnh,
///   và validation được xử lý bởi các component phụ trợ (ValidationResultHandler) được điều phối qua events.
///
internal sealed class TaskDispatcher : ITaskDispatcher, IDisposable
{
    #region Fields

    private readonly ITaskQueue _taskQueue;
    private readonly ITaskAssignmentStrategy _assignmentStrategy;
    private readonly DeviceMonitor _deviceMonitor;
    private readonly TaskDispatcherConfiguration _configuration;
    private readonly IReadOnlyDictionary<string, DeviceProfile> _deviceProfiles;
    private readonly IReadOnlyDictionary<CommandType, Location> _referenceLocations;
    private readonly ILogger _logger;

    private readonly ConcurrentDictionary<string, string> _assigningDevices = new();
    // Lock-free synchronization strategy:
    // - _queueLock protects all interactions with the task queue and related checks
    // - _assignmentLock protects the assigning devices map
    // - _isProcessing acts as an atomic flag to ensure a single concurrent processor
    // - _isRunning uses Interlocked operations for lock-free state management
    private readonly object _queueLock = new();
    private readonly object _assignmentLock = new();
    private int _isProcessing = 0;
    private int _isRunning = 0; // 1 = running, 0 = paused (atomic operations)
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private int _isDisposed = 0;

    #endregion

    #region Events & Properties

    /// <summary>
    /// Occurs when a task is assigned to a device.
    /// </summary>
    public event EventHandler<TaskAssignedEventArgs>? TaskAssigned;

    /// <summary>
    /// Gets a value indicating whether the task queue is currently paused.
    /// </summary>
    public bool IsPaused => _isRunning == 0;

    #endregion

    #region Constructor

    public TaskDispatcher(
        ITaskQueue taskQueue,
        ITaskAssignmentStrategy assignmentStrategy,
        DeviceMonitor deviceMonitor,
        TaskDispatcherConfiguration configuration,
        IReadOnlyDictionary<string, DeviceProfile> deviceProfiles,
        ILogger? logger = null)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        _assignmentStrategy = assignmentStrategy ?? throw new ArgumentNullException(nameof(assignmentStrategy));
        _deviceMonitor = deviceMonitor ?? throw new ArgumentNullException(nameof(deviceMonitor));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _deviceProfiles = deviceProfiles ?? throw new ArgumentNullException(nameof(deviceProfiles));
        _referenceLocations = CreateDefaultReferenceLocations;
        _logger = logger ?? NullLogger.Instance;

        try
        {
            _configuration.Validate();
            _logger.LogInformation($"TaskDispatcher initialized with {_deviceProfiles.Count} devices");

            _deviceMonitor.DeviceStatusChanged += OnDeviceStatusChanged;

            _logger.LogInformation("TaskDispatcher background processing started");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to initialize TaskDispatcher", ex);
            throw;
        }
    }

    #endregion

    #region Public Methods

    public void Pause()
    {
        Interlocked.Exchange(ref _isRunning, 0);
        _logger.LogInformation("TaskDispatcher paused");
    }

    public void Resume()
    {
        Interlocked.Exchange(ref _isRunning, 1);
        _logger.LogInformation("TaskDispatcher resumed");

        // Trigger queue processing if we have tasks
        if (!_taskQueue.IsEmpty)
        {
            _ = Task.Run(ProcessQueueIfNeeded);
        }
    }

    /// Xử lý các sự kiện thay đổi trạng thái thiết bị.
    public void OnDeviceStatusChanged(object? sender, DeviceStatusChangedEventArgs args)
    {
        try
        {
            if (args.NewStatus == DeviceStatus.Idle && !_taskQueue.IsEmpty)
            {
                if (_isRunning == 1)
                {
                    _ = Task.Run(ProcessQueueIfNeeded);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error handling device status change for {args.DeviceId}", ex);
        }
    }

    public TransportTask[] GetQueuedTasks() => _taskQueue.GetAll();

    public bool RemoveTasks(IEnumerable<string> taskIds)
    {
        if (taskIds == null)
            return false;

        // Filter out null/whitespace IDs and deduplicate to make the operation
        // deterministic and avoid repeated work.
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in taskIds)
        {
            if (string.IsNullOrWhiteSpace(id))
                continue;
            set.Add(id);
        }

        if (set.Count == 0)
            return false;

        var removedCount = 0;
        lock (_queueLock)
        {
            foreach (var taskId in set)
            {
                if (_taskQueue.TryRemove(taskId))
                {
                    removedCount++;
                }
            }

            if (removedCount > 0 && _taskQueue.IsEmpty && _configuration.AutoPauseWhenEmpty)
            {
                Interlocked.Exchange(ref _isRunning, 0);
            }
        }

        return removedCount > 0;
    }

    /// <summary>
    /// Xóa một task đơn lẻ khỏi hàng đợi.
    /// </summary>
    /// <param name="taskId">ID của task cần xóa.</param>
    /// <returns>True nếu task được xóa thành công; ngược lại false.</returns>
    public bool RemoveTask(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            return false;

        lock (_queueLock)
        {
            var removed = _taskQueue.TryRemove(taskId);

            if (removed && _taskQueue.IsEmpty && _configuration.AutoPauseWhenEmpty)
            {
                Interlocked.Exchange(ref _isRunning, 0);
            }

            return removed;
        }
    }

    public string? GetCurrentTask(string deviceId)
    {
        return _assigningDevices.TryGetValue(deviceId, out string? taskId) ? taskId : null;
    }

    /// Hoàn thành việc gán nhiệm vụ cho thiết bị được chỉ định.
    public bool CompleteTaskAssignment(string deviceId, string taskId)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(taskId))
            return false;

        var result = _assigningDevices.TryRemove(deviceId, out var currentTaskId) &&
               currentTaskId == taskId;

        if (result)
        {
            // Nếu còn task trong queue, trigger xử lý task tiếp theo
            if (!_taskQueue.IsEmpty)
            {
                // Nếu dispatcher đang pause, resume nó
                if (_isRunning == 0)
                {
                    _logger.LogInformation("Resuming queue processing after task completion");
                    Interlocked.Exchange(ref _isRunning, 1);
                }
                
                _ = Task.Run(ProcessQueueIfNeeded);
            }
            else if (_configuration.AutoPauseWhenEmpty)
            {
                // Queue rỗng và cấu hình auto pause
                Interlocked.Exchange(ref _isRunning, 0);
            }
        }

        return result;
    }

    public void EnqueueTasks(IEnumerable<TransportTask> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        var taskList = tasks.ToList();
        if (taskList.Count == 0)
            return;

        lock (_queueLock)
        {
            if (_taskQueue.Count + taskList.Count > _configuration.MaxQueueSize)
            {
                var error = ErrorDetail.TaskQueueFull(taskList.First().TaskId, _taskQueue.Count, _configuration.MaxQueueSize);
                _logger.LogError($"Queue size limit exceeded: {error.GetFullMessage()}", null);
                throw new InvalidOperationException(error.GetFullMessage());
            }

            foreach (var task in taskList)
            {
                var priority = DetermineTaskPriority(task);
                _taskQueue.Enqueue(task, priority);
            }

            _logger.LogInformation($"Enqueued {taskList.Count} tasks. Queue size: {_taskQueue.Count}");

            if (_isRunning == 1)
            {
                _ = Task.Run(ProcessQueueIfNeeded);
            }
        }
        // Note: No auto-resume when paused - requires manual ResumeQueue() call for robot systems
    }

    /// <summary>
    /// Processes the task queue if needed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ProcessQueueIfNeeded()
    {
        // Quick snapshot check under queue lock to avoid races between checks and
        // scheduling processing. If a processor is already running, bail out.
        lock (_queueLock)
        {
            if (_taskQueue.IsEmpty || _isRunning == 0)
                return;
            // try to acquire processing right
            if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 1)
                return;
        }

        try
        {
            await ProcessNextTasksAsync();
        }
        finally
        {
            Interlocked.Exchange(ref _isProcessing, 0);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates default reference locations for distance calculations.
    /// </summary>
    private static IReadOnlyDictionary<CommandType, Location> CreateDefaultReferenceLocations =>
        new Dictionary<CommandType, Location>
        {
            { CommandType.Inbound, new Location(1, 14, 5) }
        };

    /// <summary>
    /// Processes the next batch of tasks from the queue.
    /// </summary>
    private async Task ProcessNextTasksAsync()
    {
        try
        {
            var processedCount = 0;
            var maxTasks = _configuration.MaxTasksPerCycle;

            while (processedCount < maxTasks)
            {
                // Kiểm tra trạng thái dispatcher trước khi xử lý task
                if (_isRunning == 0)
                    break;

                // Xem task tiếp theo mà không lấy ra khỏi hàng đợi
                if (!_taskQueue.TryPeek(out var task) || task == null)
                    break; // Hàng đợi rỗng

                // Thử gán task cho thiết bị phù hợp
                var assignment = await TryAssignTaskToDevice(task);

                if (assignment == null)
                {
                    // Không tìm thấy thiết bị phù hợp, giữ task trong queue và dừng
                    _logger.LogWarning($"No suitable device available for task {task.TaskId}");
                    break;
                }

                // Có thiết bị phù hợp rồi, lấy task ra khỏi hàng đợi
                if (!_taskQueue.TryDequeue(out var dequeuedTask) || dequeuedTask?.TaskId != task.TaskId)
                {
                    // Task đã bị thay đổi (có thể bị xóa), thử lại vòng lặp
                    _logger.LogWarning($"Task {task.TaskId} was modified during assignment, retrying");
                    continue;
                }

                // Xử lý việc gán task cho thiết bị
                var assignmentSuccess = ProcessDeviceAssignment(assignment, dequeuedTask);
                
                if (assignmentSuccess)
                {
                    processedCount++;
                }

                await Task.Delay(1000);
            }

            LogProcessingResults(processedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error processing task queue", ex);
        }
    }

    /// <summary>
    /// Thử gán task cho thiết bị phù hợp.
    /// </summary>
    private async Task<DeviceAssignment?> TryAssignTaskToDevice(TransportTask task)
    {
        return await _assignmentStrategy.AssignTaskAsync(
            task,
            await _deviceMonitor.GetIdleDevices(),
            _deviceProfiles,
            _assigningDevices,
            _referenceLocations);
    }

    /// <summary>
    /// Xử lý việc gán task cho thiết bị.
    /// </summary>
    private bool ProcessDeviceAssignment(DeviceAssignment assignment, TransportTask task)
    {
        lock (_assignmentLock)
        {
            // Kiểm tra thiết bị có đang được gán cho task khác không
            if (_assigningDevices.ContainsKey(assignment.DeviceId))
            {
                RequeueTask(task, $"Device {assignment.DeviceId} was assigned to another task");
                return false;
            }

            // Kiểm tra trạng thái sẵn sàng của thiết bị
            if (!IsDeviceReady(assignment, task))
            {
                return false;
            }

            // Gán task cho thiết bị
            return AssignTaskToDevice(assignment, task);
        }
    }

    /// <summary>
    /// Kiểm tra xem thiết bị có sẵn sàng để nhận task hay không.
    /// </summary>
    private bool IsDeviceReady(DeviceAssignment assignment, TransportTask task)
    {
        var deviceProfile = _deviceProfiles[assignment.DeviceId];
        var connector = _deviceMonitor.GetConnector(assignment.DeviceId);

        try
        {
            var isReadyTask = connector.ReadAsync<bool>(deviceProfile.Signals.DeviceReady);
            if (isReadyTask.IsCompleted)
            {
                var isReady = isReadyTask.Result;
                if (!isReady)
                {
                    RequeueTask(task, $"Device {assignment.DeviceId} became busy during assignment");
                    return false;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to check device {assignment.DeviceId} status during assignment, proceeding with assignment: {ex.Message}");
            return true; // Tiếp tục gán nếu không kiểm tra được trạng thái
        }
    }

    /// <summary>
    /// Gán task cho thiết bị và thông báo.
    /// </summary>
    private bool AssignTaskToDevice(DeviceAssignment assignment, TransportTask task)
    {
        _assigningDevices[assignment.DeviceId] = task.TaskId;
        _logger.LogInformation($"Assigned task {task.TaskId} to device {assignment.DeviceId}");
        NotifyTaskAssigned(assignment);
        return true;
    }

    /// <summary>
    /// Đưa task quay lại hàng đợi với lý do cụ thể.
    /// </summary>
    private void RequeueTask(TransportTask task, string reason)
    {
        _logger.LogWarning($"{reason}, re-queuing task {task.TaskId}");
        lock (_queueLock)
        {
            _taskQueue.Enqueue(task, DetermineTaskPriority(task));
        }
    }

    /// <summary>
    /// Ghi log kết quả xử lý các tasks.
    /// </summary>
    private void LogProcessingResults(int processedCount)
    {
        if (processedCount > 0)
        {
            _logger.LogInformation($"Processed {processedCount} tasks. Remaining queue size: {_taskQueue.Count}");
        }
    }

    /// <summary>
    /// Determines the priority of a task based on its properties.
    /// </summary>
    private static TaskPriority DetermineTaskPriority(TransportTask task)
    {
        if (!string.IsNullOrEmpty(task.DeviceId))
            return TaskPriority.High;

        return TaskPriority.Normal;
    }

    /// <summary>
    /// Notifies that a task has been assigned to a device.
    /// </summary>
    private void NotifyTaskAssigned(DeviceAssignment assignment)
    {
        var connector = _deviceMonitor.GetConnector(assignment.DeviceId);
        TaskAssigned?.Invoke(this, new TaskAssignedEventArgs(assignment.DeviceId, assignment.Task, assignment.DeviceProfile, connector));
    }

    #endregion

    #region Disposal

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
            return;

        Interlocked.Exchange(ref _isRunning, 0);
        _cancellationTokenSource.Cancel();
        Shutdown();
    }

    private void Shutdown()
    {
        try
        {
            _deviceMonitor.DeviceStatusChanged -= OnDeviceStatusChanged;
        }
        finally
        {
            _assigningDevices.Clear();
            _taskQueue.Clear();
            _cancellationTokenSource.Dispose();
        }
    }

    #endregion
}