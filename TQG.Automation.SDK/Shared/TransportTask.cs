namespace TQG.Automation.SDK.Shared;

/// <summary>
/// Đại diện cho một nhiệm vụ vận chuyển pallet giữa các vị trí trong hệ thống kho tự động.
/// </summary>
public class TransportTask
{
    /// <summary>
    /// Mã định danh duy nhất cho nhiệm vụ vận chuyển.
    /// </summary>
    public required string TaskId { get; set; }

    /// <summary>
    /// Mã định danh thiết bị (DeviceId) dự kiến thực thi nhiệm vụ.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Loại lệnh cần được thực thi (Inbound, Outbound, hoặc Transfer).
    /// </summary>
    public CommandType CommandType { get; set; }

    /// <summary>
    /// Vị trí nguồn cho thao tác vận chuyển.
    /// Sử dụng cho các thao tác Outbound và Transfer.
    /// </summary>
    public Location? SourceLocation { get; set; }

    /// <summary>
    /// Vị trí đích cho thao tác vận chuyển.
    /// Sử dụng cho các thao tác Transfer.
    /// </summary>
    public Location? TargetLocation { get; set; }

    /// <summary>
    /// Số cổng cho thao tác vận chuyển.
    /// </summary>
    public short GateNumber { get; set; }

    /// <summary>
    /// Hướng vào của shuttle.
    /// Sử dụng cho các thao tác Transfer.
    /// </summary>
    public Direction InDirBlock { get; set; }

    /// <summary>
    /// Hướng ra của shuttle.
    /// Sử dụng cho cả các thao tác Outbound và Transfer.
    /// </summary>
    public Direction OutDirBlock { get; set; }
}