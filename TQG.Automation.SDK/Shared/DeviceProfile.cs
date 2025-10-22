using S7.Net;
using System.Text.Json.Serialization;

namespace TQG.Automation.SDK.Shared;

/// <summary>
/// Hồ sơ cấu hình thiết bị chứa tất cả các cài đặt cần thiết cho việc giao tiếp PLC.
/// </summary>
public sealed class DeviceProfile
{
    /// <summary>
    /// Mã định danh duy nhất, không trùng lặp cho thiết bị.
    /// Ví dụ: "SHUTTLE_01"
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Địa chỉ IP sản xuất của PLC điều khiển thiết bị này.
    /// </summary>
    public required string ProductionEndpoint { get; init; }

    /// <summary>
    /// Địa chỉ IP thử nghiệm của PLC điều khiển thiết bị này.
    /// </summary>
    public required string TestEndpoint { get; init; }

    /// <summary>
    /// Loại CPU PLC. Mặc định là S7-1200.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CpuType Cpu { get; init; } = CpuType.S71200;

    /// <summary>
    /// Số rack của PLC.
    /// </summary>
    public short Rack { get; init; } = 0;

    /// <summary>
    /// Số slot của PLC.
    /// </summary>
    public short Slot { get; init; } = 1;

    /// <summary>
    /// Ánh xạ địa chỉ bộ nhớ cho các thanh ghi PLC.
    /// </summary>
    public required SignalMap Signals { get; init; }

    /// <summary>
    /// Lấy endpoint phù hợp dựa trên chế độ hoạt động.
    /// </summary>
    /// <param name="isTestMode">True nếu ở chế độ test, false nếu ở chế độ production</param>
    /// <returns>Endpoint tương ứng</returns>
    public string GetEndpoint(bool isTestMode)
    {
        return isTestMode ? TestEndpoint : ProductionEndpoint;
    }
}
