namespace TQG.Automation.SDK.Configurations;

/// <summary>
/// Cấu hình cho các thao tác giám sát thiết bị.
/// Kiểm soát hiệu suất và giới hạn tài nguyên trong việc tương tác với PLC.
/// </summary>
public sealed class DeviceMonitorConfiguration
{
    /// <summary>
    /// Số lượng tối đa các thao tác thiết bị đồng thời. Mặc định: 10.
    /// Giới hạn tương tác PLC/thiết bị song song để tránh làm quá tải host hoặc PLC.
    /// Giảm giá trị này trên các host có tài nguyên hạn chế.
    /// </summary>
    public int MaxConcurrentOperations { get; set; } = 10;
}
