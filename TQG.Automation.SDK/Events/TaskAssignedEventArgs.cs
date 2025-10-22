namespace TQG.Automation.SDK.Events;

/// <summary>
/// Tham số sự kiện khi gán task cho thiết bị.
/// Chứa thông tin về thiết bị, task, profile và connector được sử dụng.
/// </summary>
/// <param name="deviceId">Mã định danh thiết bị được gán task.</param>
/// <param name="task">Task được gán cho thiết bị.</param>
/// <param name="profile">Profile cấu hình của thiết bị.</param>
/// <param name="connector">Connector PLC để giao tiếp với thiết bị.</param>
internal class TaskAssignedEventArgs(string deviceId, TransportTask task, DeviceProfile profile, IPlcConnector connector) : EventArgs
{
    /// <summary>
    /// Mã định danh thiết bị được gán task.
    /// </summary>
    public string DeviceId { get; } = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
    
    /// <summary>
    /// Task được gán cho thiết bị.
    /// </summary>
    public TransportTask Task { get; } = task ?? throw new ArgumentNullException(nameof(task));
    
    /// <summary>
    /// Profile cấu hình của thiết bị.
    /// </summary>
    public DeviceProfile Profile { get; } = profile ?? throw new ArgumentNullException(nameof(profile));
    
    /// <summary>
    /// Connector PLC để giao tiếp với thiết bị.
    /// </summary>
    public IPlcConnector Connector { get; } = connector ?? throw new ArgumentNullException(nameof(connector));
}