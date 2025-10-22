namespace TQG.Automation.SDK.Exceptions;

/// <summary>
/// Exception được ném ra khi không thể thiết lập kết nối PLC sau tất cả các lần thử lại.
/// Thường xảy ra do sự cố mạng, PLC offline, hoặc cấu hình kết nối không chính xác.
/// </summary>
/// <param name="ipAddress">Địa chỉ IP của PLC không thể kết nối.</param>
/// <param name="maxRetries">Số lần thử kết nối tối đa đã được thực hiện.</param>
public sealed class PlcConnectionFailedException(string ipAddress, int maxRetries) : Exception($"Failed to connect to PLC at {ipAddress} after {maxRetries} attempts.")
{
    /// <summary>
    /// Địa chỉ IP của PLC không thể kết nối.
    /// </summary>
    public string IpAddress { get; } = ipAddress;
    
    /// <summary>
    /// Số lần thử kết nối tối đa đã được thực hiện.
    /// </summary>
    public int MaxRetries { get; } = maxRetries;
}

