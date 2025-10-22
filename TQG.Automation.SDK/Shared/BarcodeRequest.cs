namespace TQG.Automation.SDK.Shared;

/// <summary>
/// Đại diện cho yêu cầu xác thực barcode từ thiết bị.
/// </summary>
public class BarcodeRequest
{
    /// <summary>
    /// Mã định danh thiết bị đã gửi yêu cầu barcode.
    /// </summary>
    public required string DeviceId { get; set; }

    /// <summary>
    /// Mã định danh nhiệm vụ được liên kết với yêu cầu barcode.
    /// </summary>
    public required string TaskId { get; set; }

    /// <summary>
    /// Giá trị barcode cần được xác thực.
    /// </summary>
    public required string Barcode { get; set; }

    /// <summary>
    /// Vị trí thực tế nơi barcode được quét.
    /// </summary>
    public Location? ActualLocation { get; set; }
}
