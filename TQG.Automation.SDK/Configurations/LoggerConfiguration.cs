namespace TQG.Automation.SDK.Configurations;

/// <summary>
/// Cấu hình cho hành vi logging dựa trên chế độ ứng dụng.
/// Định nghĩa các tùy chọn đầu ra log và định dạng thông báo.
/// </summary>
public record LoggerConfiguration
{
    /// <summary>
    /// Mức log tối thiểu để xuất ra. Các log dưới mức này sẽ bị bỏ qua.
    /// </summary>
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;

    /// <summary>
    /// Có bật đầu ra console hay không. Hữu ích cho debug và môi trường development.
    /// </summary>
    public bool EnableConsoleOutput { get; init; } = false;

    /// <summary>
    /// Có bật đầu ra file hay không. Thường được bật để lưu trữ log lâu dài.
    /// </summary>
    public bool EnableFileOutput { get; init; } = true;

    /// <summary>
    /// Có bật đầu ra debug hay không. Chỉ nên bật trong môi trường development.
    /// </summary>
    public bool EnableDebugOutput { get; init; } = true;

    /// <summary>
    /// Có bao gồm timestamp trong thông báo log hay không.
    /// </summary>
    public bool IncludeTimestamp { get; init; } = true;

    /// <summary>
    /// Có bao gồm tên component trong thông báo log hay không.
    /// </summary>
    public bool IncludeComponentName { get; init; } = true;
}