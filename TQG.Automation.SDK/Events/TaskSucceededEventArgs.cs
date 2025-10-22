namespace TQG.Automation.SDK.Events;

/// <summary>
/// Tham số sự kiện khi task trên thiết bị thành công.
/// Chứa thông tin về thiết bị và task đã hoàn thành thành công.
/// </summary>
/// <param name="deviceId">Mã định danh thiết bị có task thành công.</param>
/// <param name="taskId">Mã định danh task đã hoàn thành thành công.</param>
public class TaskSucceededEventArgs(string deviceId, string taskId) : EventArgs
{
    /// <summary>
    /// Mã định danh thiết bị có task thành công.
    /// </summary>
    public string DeviceId { get; } = deviceId;
    
    /// <summary>
    /// Mã định danh task đã hoàn thành thành công.
    /// </summary>
    public string TaskId { get; } = taskId;
}