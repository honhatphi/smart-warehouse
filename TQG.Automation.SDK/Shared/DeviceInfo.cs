namespace TQG.Automation.SDK.Shared;

/// <summary>
/// Đại diện cho thông tin cơ bản về thiết bị bao gồm mã định danh và vị trí hiện tại.
/// </summary>
/// <param name="DeviceId">Mã định danh duy nhất của thiết bị</param>
/// <param name="Location">Vị trí hiện tại của thiết bị</param>
public record DeviceInfo(string DeviceId, Location Location);
