namespace TQG.Automation.SDK.Events;

/// <summary>
/// Tham số sự kiện khi task trên thiết bị thất bại.
/// Chứa thông tin về thiết bị, task và chi tiết lỗi xảy ra.
/// </summary>
/// <param name="deviceId">Mã định danh thiết bị có task thất bại.</param>
/// <param name="taskId">Mã định danh task đã thất bại.</param>
/// <param name="errorDetail">Chi tiết lỗi bao gồm mã lỗi và thông báo lỗi.</param>
public sealed class TaskFailedEventArgs(string deviceId, string taskId, ErrorDetail errorDetail) : EventArgs
{
    /// <summary>
    /// Mã định danh thiết bị có task thất bại.
    /// </summary>
    public string DeviceId { get; } = deviceId;
    
    /// <summary>
    /// Mã định danh task đã thất bại.
    /// </summary>
    public string TaskId { get; } = taskId;
    
    /// <summary>
    /// Chi tiết lỗi bao gồm mã lỗi và thông báo lỗi.
    /// </summary>
    public ErrorDetail ErrorDetail { get; } = errorDetail;
}


