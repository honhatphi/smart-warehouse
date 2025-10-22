namespace TQG.Automation.SDK.Configurations;

/// <summary>
/// Cấu hình cho dịch vụ BarcodeHandler - xử lý và validation barcode.
/// Định nghĩa các giới hạn thời gian và độ dài cho việc xử lý barcode.
/// </summary>
public class BarcodeHandlerConfiguration
{
    /// <summary>
    /// Thời gian timeout cho việc validation barcode tính bằng phút. Mặc định là 2 phút.
    /// Sau thời gian này, quá trình validation sẽ bị hủy bỏ.
    /// </summary>
    public int ValidationTimeoutMinutes { get; set; } = 2;

    /// <summary>
    /// Số ký tự tối đa được phép trong một barcode. Mặc định là 10 ký tự.
    /// Barcode vượt quá giới hạn này sẽ bị từ chối.
    /// </summary>
    public int MaxBarcodeLength { get; set; } = 10;
}
