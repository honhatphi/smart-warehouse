namespace TQG.Automation.SDK.Communication;

/// <summary>
/// Lớp cơ sở trừu tượng cung cấp chức năng chung cho các kết nối PLC.
/// Xử lý việc nhận diện thiết bị, xác thực cấu hình, ghi log và giải phóng tài nguyên.
/// </summary>
internal abstract class BasePlcConnector : IPlcConnector
{
    private bool _disposed;
    protected readonly ILogger Logger;
    protected readonly PlcConfiguration PlcConfig;

    public string DeviceId { get; }

    /// <summary>
    /// Khởi tạo một thể hiện mới của lớp BasePlcConnector.
    /// </summary>
    /// <param name="deviceId">Mã định danh duy nhất cho thiết bị PLC. Không được null hoặc rỗng.</param>
    /// <param name="plcConfig">Cấu hình kết nối PLC. Nếu null, sẽ sử dụng cấu hình mặc định.</param>
    /// <param name="logger">Đối tượng logger cho các thông báo chẩn đoán. Nếu null, sẽ sử dụng NullLogger.</param>
    /// <exception cref="ArgumentNullException">Ném ra khi deviceId là null.</exception>
    protected BasePlcConnector(
        string deviceId,
        PlcConfiguration? plcConfig,
        ILogger? logger)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        PlcConfig = plcConfig ?? new PlcConfiguration();
        Logger = logger ?? NullLogger.Instance;

        // Validate configuration
        PlcConfig.Validate();
    }

    public abstract Task<T> ReadAsync<T>(string address);
    
    public abstract Task WriteAsync<T>(string address, T value);
    
    public abstract Task<bool> IsConnectedAsync();
    
    public abstract Task EnsureConnectedAsync();

    protected void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, GetType().Name);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { DisposeCore(); }
        catch (Exception ex) { Logger.LogWarning($"Dispose error: {ex.Message}"); }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Khi được ghi đè trong lớp dẫn xuất, giải phóng các tài nguyên không được quản lý bởi connector
    /// và tùy chọn giải phóng các tài nguyên được quản lý.
    /// </summary>
    protected virtual void DisposeCore() { }
}
