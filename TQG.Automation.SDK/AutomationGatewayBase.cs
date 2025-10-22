using System.Threading.Channels;

namespace TQG.Automation.SDK;

/// <summary>
/// Lớp cơ sở để tương tác với các thiết bị PLC.
/// Cung cấp chức năng core cho quản lý thiết bị, thực thi lệnh và xử lý barcode.
/// </summary>
public abstract class AutomationGatewayBase : IDisposable
{
    private readonly DeviceMonitor _deviceMonitor;
    private readonly BarcodeHandler _barcodeHandler;
    private readonly TaskDispatcher _taskDispatcher;
    private readonly CommandSender _commandSender;
    private readonly ILogger _logger;

    protected readonly Channel<BarcodeRequest> ValidationChannel;

    /// <summary>
    /// Lấy cấu hình các thiết bị.
    /// </summary>
    public IReadOnlyDictionary<string, DeviceProfile> DeviceConfigs { get; }

    /// <summary>
    /// Sự kiện phát sinh khi nhận được barcode từ thiết bị.
    /// </summary>
    public event EventHandler<BarcodeReceivedEventArgs>? BarcodeReceived;

    /// <summary>
    /// Sự kiện phát sinh khi nhiệm vụ hoàn thành thành công.
    /// </summary>
    public event EventHandler<TaskSucceededEventArgs>? TaskSucceeded;

    /// <summary>
    /// Sự kiện phát sinh khi nhiệm vụ thất bại.
    /// </summary>
    public event EventHandler<TaskFailedEventArgs>? TaskFailed;

    /// <summary>
    /// Khởi tạo instance mới của lớp AutomationGatewayBase.
    /// </summary>
    /// <param name="devices">Danh sách cấu hình thiết bị (không được null).</param>
    /// <param name="config">Cấu hình ứng dụng (không được null).</param>
    /// <exception cref="ArgumentNullException">Ném ra khi devices hoặc config là null.</exception>
    /// <exception cref="ArgumentException">Ném ra khi có ID thiết bị trùng lặp trong danh sách devices.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi tạo logger thất bại hoặc khởi tạo service thất bại.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi bất kỳ service nào đã bị dispose trong quá trình khởi tạo.</exception>
    protected AutomationGatewayBase(IEnumerable<DeviceProfile> devices, ApplicationConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(devices);
        ArgumentNullException.ThrowIfNull(config);
        ValidateUniqueDeviceIds(devices);

        DeviceConfigs = devices.ToDictionary(d => d.Id);

        if (DeviceConfigs.Count == 0)
        {
            throw new ArgumentException("The devices collection must contain at least one device configuration.", nameof(devices));
        }

        _logger = LoggerFactory.CreateLogger("AutomationGateway", config.Logger);

        int deviceCount = DeviceConfigs.Count;
        ValidationChannel = Channel.CreateBounded<BarcodeRequest>(deviceCount > 0 ? deviceCount : 1);

        _logger.LogInformation($"Initializing AutomationGateway with {deviceCount} devices");

        _deviceMonitor = new DeviceMonitor(DeviceConfigs, config.IsTestMode, config.DeviceMonitor, null, _logger);
        _barcodeHandler = new BarcodeHandler(_deviceMonitor, ValidationChannel, config.BarcodeHandler, _logger);

        _taskDispatcher = TaskDispatcherFactory.Create(_deviceMonitor, DeviceConfigs, config.TaskDispatcher, _logger);

        var strategyFactory = new CommandExecutionStrategyFactory(_deviceMonitor, _barcodeHandler, _taskDispatcher, _logger);
        var commandExecutor = new CommandExecutor(strategyFactory, config.TaskTimeout, _logger);
        var validationHandler = new ValidationResultHandler(_deviceMonitor, _barcodeHandler, _taskDispatcher);
        _commandSender = new CommandSender(_taskDispatcher, commandExecutor, validationHandler, _logger);

        _barcodeHandler.BarcodeReceived += (sender, args) => BarcodeReceived?.Invoke(this, args);
        _barcodeHandler.TaskFailed += (sender, args) => TaskFailed?.Invoke(this, args);
        _commandSender.TaskSucceeded += (sender, args) => TaskSucceeded?.Invoke(this, args);
        _commandSender.TaskFailed += (sender, args) => TaskFailed?.Invoke(this, args);

        _logger.LogInformation($"AutomationGateway initialized successfully with {deviceCount} devices configured");
    }

    /// <summary>
    /// Kích hoạt thiết bị và bắt đầu giám sát trạng thái.
    /// </summary>
    /// <param name="deviceId">Mã định danh thiết bị cần kích hoạt.</param>
    /// <exception cref="ArgumentException">Ném ra khi deviceId là null hoặc rỗng.</exception>
    /// <exception cref="DeviceNotRegisteredException">Ném ra khi thiết bị không tồn tại.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi DeviceMonitor đã bị dispose.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi thiết bị ở trạng thái không hợp lệ để kích hoạt.</exception>
    /// <exception cref="TimeoutException">Ném ra khi thao tác đọc PLC bị timeout.</exception>
    /// <exception cref="PlcConnectionFailedException">Ném ra khi không thể thiết lập kết nối PLC.</exception>
    public Task ActivateDevice(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        return Task.Run(async () =>
        {
            await ValidateDeviceConnectionAsync(deviceId);
            await _deviceMonitor.StartMonitoring(deviceId);
        });
    }

    /// <summary>
    /// Vô hiệu hóa thiết bị và ngắt kết nối.
    /// </summary>
    /// <param name="deviceId">Mã định danh thiết bị cần vô hiệu hóa.</param>
    /// <exception cref="ObjectDisposedException">Ném ra khi DeviceMonitor đã bị dispose.</exception>
    public void DeactivateDevice(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        _deviceMonitor.StopMonitoring(deviceId);
    }

    /// <summary>
    /// Kiểm tra trạng thái kết nối của thiết bị.
    /// </summary>
    /// <param name="deviceId">Mã định danh thiết bị cần kiểm tra.</param>
    /// <returns>True nếu thiết bị đang kết nối (Idle hoặc Busy), ngược lại là False.</returns>
    /// <exception cref="ObjectDisposedException">Ném ra khi DeviceMonitor đã bị dispose.</exception>
    public bool IsConnected(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        return _deviceMonitor.IsConnected(deviceId);
    }

    /// <summary>
    /// Lấy trạng thái hiện tại của thiết bị.
    /// </summary>
    /// <param name="deviceId">Mã định danh thiết bị cần lấy trạng thái.</param>
    /// <returns>Trạng thái thiết bị (DeviceStatus). Trả về DeviceStatus.Offline nếu không tìm thấy.</returns>
    /// <exception cref="ObjectDisposedException">Ném ra khi DeviceMonitor đã bị dispose.</exception>
    public DeviceStatus GetDeviceStatus(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        return _deviceMonitor.GetDeviceStatus(deviceId);
    }

    /// <summary>
    /// Đặt lại trạng thái thiết bị về Idle nếu không đang Busy và trạng thái PLC ổn định.
    /// </summary>
    /// <param name="deviceId">Mã định danh thiết bị cần reset.</param>
    /// <returns>True nếu trạng thái được reset, False nếu thiết bị đang Busy, có alarm hoặc mã lỗi != 0.</returns>
    /// <exception cref="ObjectDisposedException">Ném ra khi DeviceMonitor đã bị dispose.</exception>
    /// <exception cref="TimeoutException">Ném ra khi thao tác đọc PLC bị timeout.</exception>
    /// <exception cref="PlcConnectionFailedException">Ném ra khi kết nối PLC thất bại.</exception>
    public async Task<bool> ResetDeviceStatusAsync(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        return await _deviceMonitor.ResetDeviceStatusAsync(deviceId);
    }

    /// <summary>
    /// Gửi một lệnh vận chuyển đơn lẻ.
    /// </summary>
    /// <param name="task">Nhiệm vụ vận chuyển cần gửi.</param>
    /// <exception cref="ArgumentNullException">Ném ra khi task là null hoặc có targetLocation null.</exception>
    /// <exception cref="ArgumentException">Ném ra khi task có taskId null hoặc rỗng.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi CommandSender đã bị dispose.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi hàng đợi nhiệm vụ đã đầy hoặc phân bổ thiết bị thất bại.</exception>
    /// <exception cref="DeviceNotRegisteredException">Ném ra khi thiết bị được phân bổ chưa được đăng ký.</exception>
    /// <exception cref="TimeoutException">Ném ra khi thực thi lệnh bị timeout.</exception>
    /// <exception cref="PlcConnectionFailedException">Ném ra khi kết nối PLC thất bại trong quá trình thực thi.</exception>
    public Task SendCommand(TransportTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        return Task.Run(async () =>
        {
            if (!string.IsNullOrWhiteSpace(task.DeviceId))
            {
                await ValidateDeviceConnectionAsync(task.DeviceId);
            }

            await _commandSender.SendCommand(task);
        });
    }

    /// <summary>
    /// Gửi nhiều lệnh vận chuyển cùng lúc.
    /// </summary>
    /// <param name="tasks">Danh sách các nhiệm vụ vận chuyển cần gửi.</param>
    /// <exception cref="ArgumentNullException">Ném ra khi tasks là null hoặc chứa task có targetLocation null.</exception>
    /// <exception cref="ArgumentException">Ném ra khi tasks rỗng hoặc chứa task có taskId null/rỗng.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi CommandSender đã bị dispose.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi hàng đợi nhiệm vụ đã đầy hoặc phân bổ thiết bị thất bại.</exception>
    /// <exception cref="DeviceNotRegisteredException">Ném ra khi thiết bị được phân bổ chưa được đăng ký.</exception>
    /// <exception cref="TimeoutException">Ném ra khi thực thi lệnh bị timeout.</exception>
    /// <exception cref="PlcConnectionFailedException">Ném ra khi kết nối PLC thất bại trong quá trình thực thi.</exception>
    public Task SendMultipleCommands(List<TransportTask> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        // Validate non-empty and duplicate taskIds
        if (tasks.Count == 0)
            throw new ArgumentException("Tasks list must contain at least one task.", nameof(tasks));

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in tasks)
        {
            if (t is null)
                throw new ArgumentException("Tasks list contains a null task.", nameof(tasks));
            if (string.IsNullOrWhiteSpace(t.TaskId))
                throw new ArgumentException("All tasks must have a non-empty TaskId.", nameof(tasks));
            if (!seen.Add(t.TaskId))
                throw new ArgumentException($"Duplicate TaskId detected: {t.TaskId}", nameof(tasks));
        }

        var devicesToValidate = tasks
            .Where(t => !string.IsNullOrWhiteSpace(t.DeviceId))
            .Select(t => t.DeviceId!)
            .Distinct()
            .ToList();

        return Task.Run(async () =>
        {
            foreach (var deviceId in devicesToValidate)
            {
                await ValidateDeviceConnectionAsync(deviceId);
            }

            await _commandSender.SendMultipleCommands(tasks);
        });
    }

    /// <summary>
    /// Gửi kết quả validation barcode đến thiết bị.
    /// </summary>
    /// <param name="deviceId">Mã định danh thiết bị cần gửi kết quả.</param>
    /// <param name="taskId">Mã định danh nhiệm vụ liên quan.</param>
    /// <param name="isValid">Kết quả validation barcode (true nếu hợp lệ).</param>
    /// <param name="targetLocation">Vị trí đích (tùy chọn, bắt buộc nếu valid).</param>
    /// <param name="direction">Hướng vào block (block có 2 hướng vào, ví dụ: block 3).</param>
    /// <param name="gateNumber">Số cổng để vào kho.</param>
    /// <exception cref="ArgumentException">Ném ra khi deviceId hoặc taskId là null hoặc rỗng.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi CommandSender đã bị dispose.</exception>
    /// <exception cref="DeviceNotRegisteredException">Ném ra khi thiết bị chưa được đăng ký.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi thao tác ghi PLC thất bại.</exception>
    /// <exception cref="TimeoutException">Ném ra khi thao tác PLC bị timeout.</exception>
    /// <exception cref="PlcConnectionFailedException">Ném ra khi kết nối PLC thất bại.</exception>
    public async Task SendValidationResult(
        string deviceId,
        string taskId,
        bool isValid,
        Location? targetLocation,
        Direction direction,
        short gateNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        if (isValid)
        {
            if (targetLocation is null)
            {
                _logger.LogWarning($"Validation result marked valid but targetLocation is null for device {deviceId}, task {taskId}");
                throw new ArgumentException("targetLocation must be provided when isValid is true.", nameof(targetLocation));
            }

            if (!Enum.IsDefined(typeof(Direction), direction))
            {
                _logger.LogWarning($"Invalid direction value supplied for device {deviceId}, task {taskId}: {direction}");
                throw new ArgumentException("Invalid direction value.", nameof(direction));
            }

            if (gateNumber < 0)
            {
                _logger.LogWarning($"Invalid gateNumber supplied for device {deviceId}, task {taskId}: {gateNumber}");
                throw new ArgumentException("gateNumber must be non-negative.", nameof(gateNumber));
            }
        }

        _logger.LogInformation($"Sending validation result for device: {deviceId}, task: {taskId}, valid: {isValid}");
        await _commandSender.SendValidationResult(deviceId, taskId, isValid, targetLocation, direction, gateNumber);
    }

    /// <summary>
    /// Lấy danh sách các shuttle đang rảnh rỗi cùng với vị trí hiện tại của chúng.
    /// </summary>
    /// <returns>Danh sách các shuttle rảnh rỗi và vị trí hiện tại.</returns>
    /// <exception cref="ObjectDisposedException">Ném ra khi DeviceMonitor đã bị dispose.</exception>
    /// <exception cref="TimeoutException">Ném ra khi thao tác đọc PLC bị timeout.</exception>
    /// <exception cref="PlcConnectionFailedException">Ném ra khi kết nối PLC thất bại.</exception>
    public async Task<List<DeviceInfo>> GetIdleDevicesAsync()
    {
        return await _deviceMonitor.GetIdleDevices();
    }

    /// <summary>
    /// Lấy vị trí hiện tại của shuttle.
    /// </summary>
    /// <param name="deviceId">Mã định danh shuttle cần lấy vị trí.</param>
    /// <returns>Vị trí hiện tại (Location) hoặc null nếu shuttle không hoạt động.</returns>
    /// <exception cref="ArgumentException">Ném ra khi deviceId là null hoặc rỗng.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi DeviceMonitor đã bị dispose.</exception>
    /// <exception cref="TimeoutException">Ném ra khi thao tác đọc PLC bị timeout.</exception>
    /// <exception cref="PlcConnectionFailedException">Ném ra khi kết nối PLC thất bại.</exception>
    public async Task<Location?> GetActualLocationAsync(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        return await _deviceMonitor.GetCurrentLocationAsync(deviceId);
    }

    /// <summary>
    /// Lấy danh sách các nhiệm vụ đang chờ xử lý trong hàng đợi.
    /// </summary>
    /// <returns>Danh sách các nhiệm vụ.</returns>
    /// <exception cref="ObjectDisposedException">Ném ra khi TaskDispatcher đã bị dispose.</exception>
    public TransportTask[] GetPendingTask() => _taskDispatcher.GetQueuedTasks();

    /// <summary>
    /// Xóa một hoặc nhiều nhiệm vụ khỏi hàng đợi.
    /// </summary>
    /// <param name="taskIds">Danh sách ID của các nhiệm vụ.</param>
    /// <returns>False nếu danh sách rỗng hoặc queue không pause (IsPauseQueue = False), True nếu thành công.</returns>
    /// <exception cref="ArgumentNullException">Ném ra khi taskIds là null.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi TaskDispatcher đã bị dispose.</exception>
    public bool RemoveTransportTasks(IEnumerable<string> taskIds)
    {
        ArgumentNullException.ThrowIfNull(taskIds);
        // Avoid materializing unless necessary; reject empty sequences quickly
        int count = taskIds switch
        {
            ICollection<string> collection => collection.Count,
            IReadOnlyCollection<string> readOnlyCollection => readOnlyCollection.Count,
            _ => !taskIds.Take(1).Any() ? 0 : -1 // -1 means unknown (non-empty)
        };

        if (count == 0)
        {
            return false;
        }

        // If count is unknown (-1), we still need to validate duplicates; do a single-pass
        // and collect IDs into a HashSet to detect duplicates while avoiding full List allocation when possible.
        if (count == -1)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in taskIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("taskIds must not contain null or empty ids", nameof(taskIds));
                }
                if (!set.Add(id))
                {
                    throw new ArgumentException($"Duplicate taskId detected: {id}", nameof(taskIds));
                }
            }
            return _taskDispatcher.RemoveTasks(set);
        }

        // count > 0 path: ensure no empty or duplicate ids
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in taskIds)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("taskIds must not contain null or empty ids", nameof(taskIds));
            if (!seen.Add(id))
                throw new ArgumentException($"Duplicate taskId detected: {id}", nameof(taskIds));
        }

        return _taskDispatcher.RemoveTasks(seen);
    }

    /// <summary>
    /// Lấy TaskId đang được thực thi hiện tại.
    /// </summary>
    /// <param name="deviceId">Mã định danh shuttle.</param>
    /// <returns>TaskId hoặc null nếu không có nhiệm vụ đang chạy.</returns>
    /// <exception cref="ObjectDisposedException">Ném ra khi TaskDispatcher đã bị dispose.</exception>
    public string? GetCurrentTask(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        return _taskDispatcher.GetCurrentTask(deviceId);
    }


    /// <summary>
    /// Tạm dừng việc thực thi lệnh (các lệnh đang chạy vẫn tiếp tục cho đến khi hoàn thành).
    /// </summary>
    /// <exception cref="ObjectDisposedException">Ném ra khi TaskDispatcher đã bị dispose.</exception>
    public void PauseQueue()
    {
        _logger.LogInformation("Pausing command queue");
        _taskDispatcher.Pause();
    }

    /// <summary>
    /// Tiếp tục việc thực thi lệnh cho các nhiệm vụ trong hàng đợi.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Ném ra khi TaskDispatcher đã bị dispose.</exception>
    public void ResumeQueue()
    {
        _logger.LogInformation("Resuming command queue");
        _taskDispatcher.Resume();
    }

    /// <summary>
    /// Kiểm tra xem hàng đợi hiện có đang bị tạm dừng hay không.
    /// </summary>
    public bool IsPauseQueue
        => _taskDispatcher.IsPaused;

    /// <summary>
    /// Giải phóng tài nguyên của lớp, bao gồm dispose các component con.
    /// </summary>
    public void Dispose()
    {
        _logger.LogInformation("Disposing AutomationGateway resources");
        _commandSender.Dispose();
        _barcodeHandler.Dispose();
        _deviceMonitor.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Đặt lại hệ thống về trạng thái ban đầu cho mục đích testing.
    /// Phương thức này chỉ nên được gọi ở test mode và yêu cầu các điều kiện bảo mật cụ thể.
    /// </summary>
    /// <param name="deviceId">Mã định danh thiết bị cần reset.</param>
    /// <returns>Task đại diện cho thao tác reset bất đồng bộ.</returns>
    /// <exception cref="ArgumentException">Ném ra khi deviceId là null hoặc rỗng.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi không ở test mode hoặc không thỏa mãn điều kiện bảo mật.</exception>
    /// <exception cref="DeviceNotRegisteredException">Ném ra khi thiết bị chưa được đăng ký.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi DeviceMonitor đã bị dispose.</exception>
    /// <exception cref="TimeoutException">Ném ra khi các thao tác PLC bị timeout.</exception>
    /// <exception cref="PlcConnectionFailedException">Ném ra khi kết nối PLC thất bại.</exception>
    public async Task ResetSystemAsync(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        await _deviceMonitor.ResetSystemAsync(deviceId);
    }

    /// <summary>
    /// Kiểm tra tính duy nhất của các ID thiết bị trong danh sách cấu hình.
    /// </summary>
    /// <param name="devices">Danh sách cấu hình thiết bị.</param>
    /// <exception cref="ArgumentException">Ném ra khi có ID trùng lặp.</exception>
    private static void ValidateUniqueDeviceIds(IEnumerable<DeviceProfile> devices)
    {
        var seenIds = new HashSet<string>();
        var duplicates = new List<string>();

        foreach (var device in devices)
        {
            if (!seenIds.Add(device.Id))
            {
                duplicates.Add(device.Id);
            }
        }

        if (duplicates.Count > 0)
        {
            throw new ArgumentException($"Duplicate device IDs found: {string.Join(", ", duplicates)}");
        }
    }

    private async Task ValidateDeviceConnectionAsync(string deviceId)
    {
        _logger.LogInformation($"Validating device {deviceId} connection");

        try
        {
            bool isConnected = await _deviceMonitor.ReadConnectionStatusAsync(deviceId);

            if (!isConnected)
            {
                var errorMessage = $"Device {deviceId} is not connected to software. Please ensure device connection before operation.";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            _logger.LogInformation($"Device {deviceId} connection validated successfully");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to validate device {deviceId} connection: {ex.Message}");
            throw new InvalidOperationException($"Failed to validate device {deviceId} connection status.", ex);
        }
    }
}