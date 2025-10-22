namespace TQG.Automation.SDK.Events;

/// <summary>
/// Tham số sự kiện khi trạng thái thiết bị thay đổi.
/// Chứa thông tin về thiết bị, trạng thái mới và trạng thái trước đó.
/// </summary>
/// <param name="deviceId">Mã định danh thiết bị có trạng thái thay đổi.</param>
/// <param name="newStatus">Trạng thái mới của thiết bị.</param>
/// <param name="previousStatus">Trạng thái trước đó của thiết bị. Null nếu là lần đầu thiết lập trạng thái.</param>
public class DeviceStatusChangedEventArgs(string deviceId, DeviceStatus newStatus, DeviceStatus? previousStatus = null) : EventArgs
{
    /// <summary>
    /// Mã định danh thiết bị có trạng thái thay đổi.
    /// </summary>
    public string DeviceId { get; } = deviceId;
    
    /// <summary>
    /// Trạng thái mới của thiết bị.
    /// </summary>
    public DeviceStatus NewStatus { get; } = newStatus;
    
    /// <summary>
    /// Trạng thái trước đó của thiết bị. Null nếu là lần đầu thiết lập trạng thái.
    /// </summary>
    public DeviceStatus? PreviousStatus { get; } = previousStatus;
}
