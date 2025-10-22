namespace TQG.Automation.SDK.Configurations;

/// <summary>
/// Cấu hình cho các thao tác PLC bao gồm timeout và hành vi retry kết nối.
/// Cung cấp cấu hình thống nhất cho toàn bộ giao tiếp PLC giữa các thiết bị.
/// </summary>
public class PlcConfiguration
{
    /// <summary>
    /// Timeout mặc định cho các thao tác đọc PLC tính bằng giây. Mặc định là 10 giây.
    /// Áp dụng cho tất cả thao tác đọc khi không định nghĩa timeout cụ thể.
    /// </summary>
    public int ReadTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Timeout mặc định cho các thao tác ghi PLC tính bằng giây. Mặc định là 10 giây.
    /// Thao tác ghi thường mất thời gian lâu hơn đọc do xử lý PLC.
    /// </summary>
    public int WriteTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Số lần thử lại tối đa cho kết nối PLC. Mặc định là 5 lần.
    /// </summary>
    public int MaxConnectionRetries { get; set; } = 5;

    /// <summary>
    /// Độ trễ giữa các lần thử kết nối lại tính bằng giây. Mặc định là 2 giây.
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Lấy timeout đọc dưới dạng TimeSpan.
    /// </summary>
    public TimeSpan ReadTimeout => TimeSpan.FromSeconds(ReadTimeoutSeconds);

    /// <summary>
    /// Lấy timeout ghi dưới dạng TimeSpan.
    /// </summary>
    public TimeSpan WriteTimeout => TimeSpan.FromSeconds(WriteTimeoutSeconds);

    /// <summary>
    /// Lấy độ trễ retry dưới dạng TimeSpan.
    /// </summary>
    public TimeSpan RetryDelay => TimeSpan.FromSeconds(RetryDelaySeconds);

    /// <summary>
    /// Xác thực các giá trị cấu hình để đảm bảo tính hợp lệ.
    /// </summary>
    /// <exception cref="ArgumentException">Ném ra khi các giá trị cấu hình không hợp lệ.</exception>
    public void Validate()
    {
        if (ReadTimeoutSeconds <= 0)
            throw new ArgumentException("ReadTimeoutSeconds must be greater than 0");

        if (WriteTimeoutSeconds <= 0)
            throw new ArgumentException("WriteTimeoutSeconds must be greater than 0");

        if (MaxConnectionRetries <= 0)
            throw new ArgumentException("MaxConnectionRetries must be greater than 0");

        if (RetryDelaySeconds <= 0)
            throw new ArgumentException("RetryDelaySeconds must be greater than 0");
    }
}