using S7.Net;
using System.ComponentModel;

namespace TQG.Automation.SDK.Communication.S7;

/// <summary>
/// Triển khai kết nối PLC cho giao tiếp giao thức Siemens S7.
/// Cung cấp các thao tác đọc/ghi với quản lý kết nối tự động, logic thử lại và chuyển đổi kiểu dữ liệu.
/// </summary>
internal sealed class S7PlcConnector(
    string deviceId,
    string ipAddress,
    CpuType cpuType,
    short rack,
    short slot,
    PlcConfiguration? plcConfig = null,
    ILogger? logger = null) : BasePlcConnector(deviceId, plcConfig, logger)
{
    private readonly Plc _plc = new(cpuType, ipAddress, rack, slot);
    private readonly AsyncLock _connectLock = new();

    public override async Task<T> ReadAsync<T>(string address)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address cannot be null or empty", nameof(address));

        await EnsureConnectedAsync().ConfigureAwait(false);

        var value = await TimeoutHelper.RunWithTimeout(async ct =>
        {
            var task = _plc.ReadAsync(address, ct);
            var result = await task.WaitAsync(ct).ConfigureAwait(false);
            return result ?? throw new InvalidOperationException($"Reading '{address}' returned null");
        }, PlcConfig.ReadTimeout).ConfigureAwait(false);

        var converted = ConvertTo<T>(value, $"address '{address}'");
        return converted;
    }

    public override async Task WriteAsync<T>(string address, T value)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address cannot be null or empty", nameof(address));
        if (value is null) throw new ArgumentNullException(nameof(value), "Cannot write null");
        await EnsureConnectedAsync().ConfigureAwait(false);

        await TimeoutHelper.RunWithTimeout(async ct =>
        {
            var task = _plc.WriteAsync(address, value!, cancellationToken: ct);
            await task.WaitAsync(ct).ConfigureAwait(false);
        }, PlcConfig.WriteTimeout).ConfigureAwait(false);
    }

    public override Task<bool> IsConnectedAsync() => Task.FromResult(_plc.IsConnected);

    public override async Task EnsureConnectedAsync()
    {
        if (_plc.IsConnected) return;

        using var _ = await _connectLock.LockAsync().ConfigureAwait(false);
        if (_plc.IsConnected) return;

        var maxRetries = PlcConfig.MaxConnectionRetries;
        var delay = new FixedDelayStrategy(PlcConfig.RetryDelay);
        var endpoint = _plc.IP;

        await RetryPolicy.ExecuteAsync(async () =>
        {
            await _plc.OpenAsync().ConfigureAwait(false);
            Logger.LogInformation($"[S7][{DeviceId}] Connected {endpoint}");
        }, maxRetries, delay, Logger, $"Connect S7 {endpoint}").ConfigureAwait(false);
    }

    /// <summary>
    /// Chuyển đổi một giá trị từ định dạng S7 PLC sang kiểu đích được chỉ định.
    /// Xử lý các ánh xạ kiểu dữ liệu S7 thông thường và thực hiện chuyển đổi kiểu an toàn.
    /// </summary>
    /// <typeparam name="T">Kiểu đích để chuyển đổi.</typeparam>
    /// <param name="value">Giá trị cần chuyển đổi từ định dạng S7.</param>
    /// <param name="context">Thông tin ngữ cảnh tùy chọn cho thông báo lỗi.</param>
    /// <returns>Giá trị đã chuyển đổi có kiểu T.</returns>
    /// <exception cref="InvalidCastException">Ném ra khi việc chuyển đổi không thể thực hiện hoặc thất bại.</exception>
    private static T ConvertTo<T>(object value, string? context = null)
    {
        if (value is T t) return t;

        var targetType = typeof(T);
        var sourceType = value.GetType();

        if (!CanConvert(sourceType, targetType))
        {
            var ctx = string.IsNullOrWhiteSpace(context) ? string.Empty : $" for {context}";
            throw new InvalidCastException($"Cannot convert '{sourceType.Name}' -> '{targetType.Name}'{ctx}");
        }

        try
        {
            return (T)Convert.ChangeType(value, targetType);
        }
        catch (Exception ex)
        {
            var ctx = string.IsNullOrWhiteSpace(context) ? string.Empty : $" for {context}";
            throw new InvalidCastException($"Conversion failed to '{targetType.Name}'{ctx}", ex);
        }
    }

    private static bool CanConvert(Type sourceType, Type targetType)
    {
        if (sourceType == typeof(byte) && targetType == typeof(bool)) return true;
        if (sourceType == typeof(string) && targetType == typeof(bool)) return true;
        if (sourceType == typeof(ushort) && targetType == typeof(int)) return true;
        if (sourceType == typeof(byte[]))
        {
            if (targetType == typeof(string)) return true;
            if (targetType.IsArray) return true;
        }
        var converter = TypeDescriptor.GetConverter(sourceType);
        return converter.CanConvertTo(targetType);
    }

    /// <summary>
    /// Giải phóng tài nguyên kết nối S7 PLC và xử lý lỗi disposal một cách mượt mà.
    /// </summary>
    protected override void DisposeCore()
    {
        try
        {
            _plc.Close();
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"[S7][{DeviceId}] Close error: {ex.Message}");
        }
    }
}
