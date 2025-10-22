namespace TQG.Automation.SDK.Logging;

/// <summary>
/// Triển khai null logger loại bỏ tất cả thông báo log.
/// Hữu ích để tắt logging trong môi trường production hoặc testing.
/// Sử dụng Singleton pattern để tiết kiệm bộ nhớ.
/// </summary>
internal sealed class NullLogger : ILogger
{
    /// <summary>
    /// Instance singleton của NullLogger để tái sử dụng.
    /// </summary>
    public static readonly NullLogger Instance = new();

    public LogLevel MinimumLevel => LogLevel.Critical;

    private NullLogger() { }

    public bool IsEnabled(LogLevel level) => false;

    public void LogDebug(string message) { }
    public void LogInformation(string message) { }
    public void LogWarning(string message) { }
    public void LogError(string message, Exception? exception = null) { }
    public void LogCritical(string message, Exception? exception = null) { }
}
