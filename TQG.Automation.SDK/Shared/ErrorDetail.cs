namespace TQG.Automation.SDK.Shared;

/// <summary>
/// Đại diện cho thông tin lỗi chi tiết bao gồm mã lỗi, thông điệp, timestamp và chi tiết exception tùy chọn.
/// </summary>
/// <param name="ErrorCode">Mã lỗi số</param>
/// <param name="ErrorMessage">Mô tả thông điệp lỗi</param>
/// <param name="Timestamp">Timestamp khi lỗi xảy ra</param>
/// <param name="Exception">Chi tiết exception tùy chọn</param>
public record ErrorDetail(int ErrorCode, string ErrorMessage, DateTime Timestamp = default, Exception? Exception = null)
{
    /// <summary>
    /// Lấy timestamp khi lỗi xảy ra. Mặc định là thời gian UTC hiện tại nếu không được chỉ định.
    /// </summary>
    public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.UtcNow : Timestamp;

    /// <summary>
    /// Lấy chi tiết exception tùy chọn được liên kết với lỗi này.
    /// </summary>
    public Exception? Exception { get; init; } = Exception;

    /// <summary>
    /// Lấy thông điệp lỗi đầy đủ bao gồm mã lỗi và chi tiết exception tùy chọn.
    /// </summary>
    /// <returns>Chuỗi thông điệp lỗi đã định dạng</returns>
    public string GetFullMessage()
    {
        var message = $"[{ErrorCode}] {ErrorMessage}";
        if (Exception != null)
        {
            message += $"\nException: {Exception}";
        }
        return message;
    }

    /// <summary>
    /// Tạo chi tiết lỗi khi không tìm thấy nhiệm vụ.
    /// </summary>
    /// <param name="deviceId">Mã định danh thiết bị</param>
    /// <param name="taskId">Mã định danh nhiệm vụ</param>
    /// <returns>Instance ErrorDetail với mã lỗi 1001</returns>
    public static ErrorDetail NotFoundTask(string deviceId, string taskId) =>
        new(1001, $"No pending response found for task {taskId} on device {deviceId}.");

    /// <summary>
    /// Tạo chi tiết lỗi khi ID thiết bị không khớp.
    /// </summary>
    /// <param name="taskId">Mã định danh nhiệm vụ</param>
    /// <param name="expectedDeviceId">Mã định danh thiết bị mong đợi</param>
    /// <param name="providedDeviceId">Mã định danh thiết bị được cung cấp</param>
    /// <returns>Instance ErrorDetail với mã lỗi 1002</returns>
    public static ErrorDetail MismatchedDevice(string taskId, string expectedDeviceId, string providedDeviceId) =>
        new(1002, $"Mismatched device ID for task {taskId}. Expected: {expectedDeviceId}, Provided: {providedDeviceId}.");

    /// <summary>
    /// Tạo chi tiết lỗi khi thiết bị chưa được đăng ký.
    /// </summary>
    /// <param name="deviceId">Mã định danh thiết bị</param>
    /// <returns>Instance ErrorDetail với mã lỗi 1003</returns>
    public static ErrorDetail DeviceNotRegistered(string deviceId) =>
        new(1003, $"Device {deviceId} is not registered in the system.");

    /// <summary>
    /// Tạo chi tiết lỗi cho các ngoại lệ polling.
    /// </summary>
    /// <param name="pollType">Loại thao tác polling</param>
    /// <param name="deviceId">Mã định danh thiết bị</param>
    /// <param name="taskId">Mã định danh nhiệm vụ</param>
    /// <param name="exception">Ngoại lệ đã xảy ra</param>
    /// <returns>Instance ErrorDetail với mã lỗi 1004</returns>
    public static ErrorDetail PollingException(string pollType, string deviceId, string taskId, Exception exception)
        => new(1004, $"Polling {pollType} exception for task {taskId} on device {deviceId}: {exception.Message}.", default, exception);

    /// <summary>
    /// Tạo chi tiết lỗi cho thất bại khi chạy nhiệm vụ.
    /// </summary>
    /// <param name="taskId">Mã định danh nhiệm vụ</param>
    /// <param name="deviceId">Mã định danh thiết bị</param>
    /// <param name="errorCode">Mã lỗi từ thiết bị</param>
    /// <returns>Instance ErrorDetail với mã lỗi được cung cấp</returns>
    public static ErrorDetail RunningFailure(string taskId, string deviceId, short errorCode)
        => new(errorCode, $"Task {taskId} on device {deviceId} running failed with error code {errorCode}.");

    /// <summary>
    /// Tạo chi tiết lỗi cho timeout của nhiệm vụ.
    /// </summary>
    /// <param name="deviceId">Mã định danh thiết bị</param>
    /// <param name="taskId">Mã định danh nhiệm vụ</param>
    /// <param name="timeout">Thời gian timeout</param>
    /// <returns>Instance ErrorDetail với mã lỗi 1006</returns>
    public static ErrorDetail Timeout(string deviceId, string taskId, TimeSpan timeout)
        => new(1006, $"Timeout for task {taskId} on device {deviceId} after {timeout.TotalMinutes} minutes.");

    /// <summary>
    /// Tạo chi tiết lỗi cho kết nối PLC thất bại.
    /// </summary>
    public static ErrorDetail PlcConnectionFailed(string deviceId, string message)
        => new(1011, $"PLC connection failed for device {deviceId}: {message}");

    /// <summary>
    /// Tạo chi tiết lỗi cho các lỗi không xác định.
    /// </summary>
    /// <param name="deviceId">Mã định danh thiết bị</param>
    /// <param name="taskId">Mã định danh nhiệm vụ</param>
    /// <param name="message">Thông báo lỗi</param>
    /// <returns>Instance ErrorDetail với mã lỗi 1007</returns>
    public static ErrorDetail Unknown(string deviceId, string taskId, string message)
        => new(1007, $"Unknown error for task {taskId} on device {deviceId}: {message}.");

    /// <summary>
    /// Tạo chi tiết lỗi cho ngoại lệ thực thi lệnh.
    /// </summary>
    /// <param name="taskId">Mã định danh nhiệm vụ</param>
    /// <param name="deviceId">Mã định danh thiết bị</param>
    /// <param name="exception">Ngoại lệ đã xảy ra</param>
    /// <returns>Instance ErrorDetail với mã lỗi 1008</returns>
    public static ErrorDetail ExecutionException(string taskId, string deviceId, Exception exception)
        => new(1008, $"Command execution failed for task {taskId} on device {deviceId}: {exception.Message}.", default, exception);

    /// <summary>
    /// Tạo chi tiết lỗi cho ngoại lệ validation.
    /// </summary>
    /// <param name="taskId">Mã định danh nhiệm vụ</param>
    /// <param name="deviceId">Mã định danh thiết bị</param>
    /// <param name="exception">Ngoại lệ đã xảy ra</param>
    /// <returns>Instance ErrorDetail với mã lỗi 1009</returns>
    public static ErrorDetail ValidationException(string taskId, string deviceId, Exception exception)
        => new(1009, $"Validation result handling failed for task {taskId} on device {deviceId}: {exception.Message}.", default, exception);

    /// <summary>
    /// Tạo chi tiết lỗi khi hàng đợi nhiệm vụ đã đầy.
    /// </summary>
    /// <param name="taskId">Mã định danh nhiệm vụ</param>
    /// <param name="currentCount">Số lượng nhiệm vụ hiện tại trong hàng đợi</param>
    /// <param name="maxCount">Số lượng nhiệm vụ tối đa cho phép trong hàng đợi</param>
    /// <returns>Instance ErrorDetail với mã lỗi 1010</returns>
    public static ErrorDetail TaskQueueFull(string taskId, int currentCount, int maxCount)
        => new(1010, $"Task queue is full. Cannot enqueue task {taskId}. Current: {currentCount}, Max: {maxCount}.");
}