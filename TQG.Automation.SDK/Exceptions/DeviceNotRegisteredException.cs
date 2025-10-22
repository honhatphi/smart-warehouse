namespace TQG.Automation.SDK.Exceptions;

/// <summary>
/// Exception được ném ra khi thao tác trên thiết bị chưa được đăng ký trong hệ thống.
/// Thường xảy ra khi cố gắng truy cập hoặc điều khiển thiết bị không tồn tại trong danh sách cấu hình.
/// </summary>
/// <param name="deviceId">Mã định danh thiết bị không được tìm thấy trong hệ thống.</param>
public sealed class DeviceNotRegisteredException(string deviceId) : Exception($"Device with ID '{deviceId}' not registered.")
{
    /// <summary>
    /// Mã định danh thiết bị không được tìm thấy trong hệ thống.
    /// </summary>
    public string DeviceId { get; } = deviceId;
}

