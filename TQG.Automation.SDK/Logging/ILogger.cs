namespace TQG.Automation.SDK.Logging;

/// <summary>
/// Giao diện logging đơn giản cho automation SDK.
/// Sử dụng .NET logging tích hợp sẵn mà không cần thư viện bên ngoài.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Lấy cấu hình mức log hiện tại.
    /// </summary>
    LogLevel MinimumLevel { get; }

    /// <summary>
    /// Kiểm tra xem một mức log có được bật hay không.
    /// </summary>
    /// <param name="level">Mức log cần kiểm tra.</param>
    /// <returns>True nếu mức log được bật, ngược lại là false.</returns>
    bool IsEnabled(LogLevel level);

    /// <summary>
    /// Ghi log thông báo thông tin.
    /// </summary>
    /// <param name="message">Thông báo cần ghi log.</param>
    /// <exception cref="ArgumentNullException">Ném ra khi message là null.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi logger đã bị dispose.</exception>
    void LogInformation(string message);

    /// <summary>
    /// Ghi log thông báo cảnh báo.
    /// </summary>
    /// <param name="message">Thông báo cần ghi log.</param>
    /// <exception cref="ArgumentNullException">Ném ra khi message là null.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi logger đã bị dispose.</exception>
    void LogWarning(string message);

    /// <summary>
    /// Ghi log thông báo lỗi.
    /// </summary>
    /// <param name="message">Thông báo cần ghi log.</param>
    /// <param name="exception">Exception tùy chọn để bao gồm trong log.</param>
    /// <exception cref="ArgumentNullException">Ném ra khi message là null.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi logger đã bị dispose.</exception>
    void LogError(string message, Exception? exception = null);

    /// <summary>
    /// Ghi log thông báo lỗi nghiêm trọng.
    /// </summary>
    /// <param name="message">Thông báo cần ghi log.</param>
    /// <param name="exception">Exception tùy chọn để bao gồm trong log.</param>
    /// <exception cref="ArgumentNullException">Ném ra khi message là null.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi logger đã bị dispose.</exception>
    void LogCritical(string message, Exception? exception = null);
}

