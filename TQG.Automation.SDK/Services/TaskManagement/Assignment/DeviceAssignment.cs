namespace TQG.Automation.SDK.Services.TaskManagement.Assignment;

/// <summary>
/// Đại diện cho kết quả của một thao tác gán nhiệm vụ.
/// </summary>
public class DeviceAssignment(string deviceId, DeviceProfile deviceProfile, TransportTask task)
{
    public string DeviceId { get; } = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
    public DeviceProfile DeviceProfile { get; } = deviceProfile ?? throw new ArgumentNullException(nameof(deviceProfile));
    public TransportTask Task { get; } = task ?? throw new ArgumentNullException(nameof(task));
}
