using System.Diagnostics;

namespace TQG.Automation.SDK.Logging;

/// <summary>
/// Triển khai console logger cho automation SDK.
/// Sử dụng Console và Debug output để ghi log với các mức log có thể cấu hình.
/// </summary>
/// <param name="componentName">Tên component sẽ được hiển thị trong log.</param>
/// <param name="config">Cấu hình logger để điều khiển hành vi logging.</param>
internal sealed class ConsoleLogger(string componentName, LoggerConfiguration config) : ILogger
{
    private readonly string _componentName = componentName ?? throw new ArgumentNullException(nameof(componentName));
    private readonly LoggerConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));

    public LogLevel MinimumLevel => _config.MinimumLevel;

    public bool IsEnabled(LogLevel level)
    {
        return level >= _config.MinimumLevel;
    }

    public void LogDebug(string message)
    {
        if (IsEnabled(LogLevel.Debug))
            Log(LogLevel.Debug, message, null);
    }

    public void LogInformation(string message)
    {
        if (IsEnabled(LogLevel.Information))
            Log(LogLevel.Information, message, null);
    }

    public void LogWarning(string message)
    {
        if (IsEnabled(LogLevel.Warning))
            Log(LogLevel.Warning, message, null);
    }

    public void LogError(string message, Exception? exception = null)
    {
        if (IsEnabled(LogLevel.Error))
            Log(LogLevel.Error, message, exception);
    }

    public void LogCritical(string message, Exception? exception = null)
    {
        if (IsEnabled(LogLevel.Critical))
            Log(LogLevel.Critical, message, exception);
    }

    private void Log(LogLevel level, string message, Exception? exception)
    {
        var logEntry = BuildLogEntry(level, message, exception);

        if (_config.EnableConsoleOutput)
        {
            var originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = GetConsoleColor(level);
                Console.WriteLine(logEntry);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }

        if (_config.EnableDebugOutput)
        {
            Debug.WriteLine(logEntry);
        }
    }

    private string BuildLogEntry(LogLevel level, string message, Exception? exception)
    {
        var parts = new List<string>();

        if (_config.IncludeTimestamp)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            parts.Add($"[{timestamp}]");
        }

        parts.Add($"[{level}]");

        if (_config.IncludeComponentName)
        {
            parts.Add($"[{_componentName}]");
        }

        parts.Add(message);

        var logEntry = string.Join(" ", parts);

        if (exception != null)
        {
            logEntry += $"\nException: {exception}";
        }

        return logEntry;
    }

    private static ConsoleColor GetConsoleColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Information => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.Magenta,
            _ => ConsoleColor.White
        };
    }
}
