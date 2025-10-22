namespace TQG.Automation.SDK.Logging;

/// <summary>
/// Factory để tạo các instance logger dựa trên chế độ ứng dụng.
/// Tự động chọn loại logger phù hợp theo cấu hình.
/// </summary>
public static class LoggerFactory
{
    /// <summary>
    /// Tạo logger cho component được chỉ định với cấu hình tùy chỉnh.
    /// </summary>
    /// <param name="componentName">Tên component sẽ được hiển thị trong log.</param>
    /// <param name="config">Cấu hình logger để điều khiển hành vi logging.</param>
    /// <returns>Logger với cấu hình đã chỉ định. FileLogger nếu EnableFileOutput=true, ngược lại ConsoleLogger.</returns>
    /// <exception cref="ArgumentNullException">Ném ra khi componentName hoặc config là null.</exception>
    public static ILogger CreateLogger(string componentName, LoggerConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(componentName);
        ArgumentNullException.ThrowIfNull(config);

        if (config.EnableFileOutput)
        {
            return new FileLogger(componentName, config);
        }
        else
        {
            return new ConsoleLogger(componentName, config);
        }
    }
}