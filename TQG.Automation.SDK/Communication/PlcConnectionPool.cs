using System.Collections.Concurrent;

namespace TQG.Automation.SDK.Communication;

/// <summary>
/// Quản lý một pool các kết nối PLC để đảm bảo sử dụng tài nguyên hiệu quả và tái sử dụng kết nối.
/// Cung cấp tạo, truy xuất và giải phóng kết nối PLC an toàn với thread.
/// </summary>
internal sealed class PlcConnectionPool(bool isTestMode, ILogger? logger = null) : IDisposable
{
    // Use Lazy<IPlcConnector> so that under concurrent GetConnection calls we do not
    // allocate/create multiple connector instances for the same device. Lazy with
    // ExecutionAndPublication guarantees a single successful creation.
    private readonly ConcurrentDictionary<string, Lazy<IPlcConnector>> _connections = new();
    private readonly ILogger _logger = logger ?? NullLogger.Instance;
    private bool _disposed;

    /// <summary>
    /// Lấy hoặc tạo một kết nối PLC cho profile thiết bị được chỉ định.
    /// Đảm bảo tạo singleton an toàn với thread cho mỗi ID thiết bị.
    /// </summary>
    /// <param name="profile">Profile thiết bị chứa chi tiết kết nối và ID thiết bị.</param>
    /// <returns>Một instance kết nối PLC cho thiết bị đã chỉ định.</returns>
    /// <remarks>
    /// PlcConnectionPool giữ quyền sở hữu tất cả connectors và chịu trách nhiệm giải phóng chúng.
    /// Các caller KHÔNG ĐƯỢC giải phóng trực tiếp các connectors lấy từ pool này.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Ném ra khi profile là null.</exception>
    /// <exception cref="ArgumentException">Ném ra khi profile.Id là null hoặc rỗng.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi việc tạo connector thất bại.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi pool đã bị giải phóng.</exception>
    public IPlcConnector GetConnection(DeviceProfile profile)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(profile);
        if (string.IsNullOrWhiteSpace(profile.Id)) throw new ArgumentException("Device ID cannot be null or empty", nameof(profile));

        var lazy = _connections.GetOrAdd(profile.Id, _ => new Lazy<IPlcConnector>(() => CreateConnector(profile), LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return lazy.Value;
        }
        catch
        {
            // If creation failed, remove the Lazy so that future attempts may retry.
            _connections.TryRemove(profile.Id, out _);
            throw;
        }
    }

    /// <summary>
    /// Xóa và giải phóng kết nối PLC cho ID thiết bị được chỉ định.
    /// </summary>
    /// <param name="deviceId">Mã định danh duy nhất của thiết bị mà connector cần được xóa.</param>
    /// <returns>True nếu tìm thấy và xóa connector; ngược lại là false.</returns>
    /// <exception cref="ObjectDisposedException">Ném ra khi pool đã bị giải phóng.</exception>
    public bool RemoveConnection(string deviceId)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(deviceId)) return false;

        if (_connections.TryRemove(deviceId, out var lazy))
        {
            // Only dispose the connector if it was actually created to avoid
            // forcing initialization during removal.
            if (lazy.IsValueCreated)
            {
                try { lazy.Value.Dispose(); }
                catch (Exception ex) { _logger.LogWarning($"Dispose connector '{deviceId}' error: {ex.Message}"); }
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Tạo một instance kết nối PLC mới dựa trên cấu hình profile thiết bị.
    /// </summary>
    /// <param name="profile">Profile thiết bị chứa các tham số kết nối.</param>
    /// <returns>Một instance kết nối PLC mới.</returns>
    /// <exception cref="InvalidOperationException">Ném ra khi việc tạo connector thất bại.</exception>
    private IPlcConnector CreateConnector(DeviceProfile profile)
    {
        try { return PlcConnectorFactory.Create(profile, isTestMode); }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create connector for '{profile.Id}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Xác thực rằng connection pool chưa bị giải phóng.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Ném ra khi pool đã bị giải phóng.</exception>
    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, nameof(PlcConnectionPool));

    /// <summary>
    /// Giải phóng tất cả các kết nối PLC được quản lý và các tài nguyên.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var kv in _connections)
        {
            var deviceId = kv.Key;
            var lazy = kv.Value;
            if (lazy.IsValueCreated)
            {
                try { lazy.Value.Dispose(); }
                catch (Exception ex) { _logger.LogWarning($"Dispose connector '{deviceId}' error: {ex.Message}"); }
            }
        }

        _connections.Clear();
    }
}