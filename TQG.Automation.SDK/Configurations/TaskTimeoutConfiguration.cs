namespace TQG.Automation.SDK.Configurations;

/// <summary>
/// Cấu hình xử lý timeout cho các task theo loại command.
/// Định nghĩa thời gian chờ tối đa cho từng loại task khác nhau.
/// </summary>
public class TaskTimeoutConfiguration
{
    /// <summary>
    /// Timeout cho task inbound tính bằng phút. Mặc định: 15 phút.
    /// Áp dụng cho các task nhận hàng vào kho.
    /// </summary>
    public int InboundTimeoutMinutes { get; set; } = 15;

    /// <summary>
    /// Timeout cho task outbound tính bằng phút. Mặc định: 15 phút.
    /// Áp dụng cho các task xuất hàng ra khỏi kho.
    /// </summary>
    public int OutboundTimeoutMinutes { get; set; } = 15;

    /// <summary>
    /// Timeout cho task transfer tính bằng phút. Mặc định: 15 phút.
    /// Áp dụng cho các task chuyển hàng nội bộ trong kho.
    /// </summary>
    public int TransferTimeoutMinutes { get; set; } = 15;

    /// <summary>
    /// Lấy timeout cho loại command cụ thể.
    /// </summary>
    /// <param name="commandType">Loại command cần lấy timeout.</param>
    /// <returns>Thời gian timeout tính bằng phút.</returns>
    public int GetTimeoutMinutes(CommandType commandType)
    {
        return commandType switch
        {
            CommandType.Inbound => InboundTimeoutMinutes,
            CommandType.Outbound => OutboundTimeoutMinutes,
            CommandType.Transfer => TransferTimeoutMinutes,
            _ => 15 // Default fallback
        };
    }

    /// <summary>
    /// Xác thực các cài đặt cấu hình để đảm bảo tất cả timeout đều hợp lệ.
    /// </summary>
    /// <exception cref="InvalidOperationException">Ném ra khi cấu hình không hợp lệ.</exception>
    public void Validate()
    {
        if (InboundTimeoutMinutes <= 0)
            throw new InvalidOperationException("InboundTimeoutMinutes must be greater than 0");

        if (OutboundTimeoutMinutes <= 0)
            throw new InvalidOperationException("OutboundTimeoutMinutes must be greater than 0");

        if (TransferTimeoutMinutes <= 0)
            throw new InvalidOperationException("TransferTimeoutMinutes must be greater than 0");
    }
}
