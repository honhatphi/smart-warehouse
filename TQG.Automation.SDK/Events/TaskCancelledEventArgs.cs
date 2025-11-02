namespace TQG.Automation.SDK.Events;

/// <summary>
/// Tham số sự kiện khi task trên thiết bị bị hủy (cancelled).
/// Chứa thông tin về thiết bị và task bị hủy.
/// </summary>
/// <param name="deviceId">Mã định danh thiết bị có task bị hủy.</param>
/// <param name="taskId">Mã định danh task đã bị hủy.</param>
public sealed class TaskCancelledEventArgs(string deviceId, string taskId) : EventArgs
{
    /// <summary>
    /// Mã định danh thiết bị có task bị hủy.
    /// </summary>
    public string DeviceId { get; } = deviceId;
    
    /// <summary>
    /// Mã định danh task đã bị hủy.
    /// </summary>
    public string TaskId { get; } = taskId;
}
