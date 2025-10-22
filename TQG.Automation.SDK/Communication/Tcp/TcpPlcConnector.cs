using System.Net.Sockets;

namespace TQG.Automation.SDK.Communication.Tcp;

/// <summary>
/// Triển khai kết nối PLC cho giao thức giao tiếp dựa trên TCP.
/// Cung cấp các thao tác đọc/ghi sử dụng lệnh dựa trên văn bản qua kết nối TCP socket.
/// </summary>
internal sealed class TcpPlcConnector(
    string deviceId,
    string host,
    int port,
    PlcConfiguration? plcConfig = null,
    ILogger? logger = null) : BasePlcConnector(deviceId, plcConfig, logger)
{
    private readonly string _host = host ?? throw new ArgumentNullException(nameof(host));
    private readonly AsyncLock _ioLock = new();
    private TcpClient? _client;
    private StreamWriter? _writer;
    private StreamReader? _reader;

    public override async Task<T> ReadAsync<T>(string address)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address cannot be null or empty", nameof(address));

        var response = await SendCommandAsync($"READ {DeviceId} {address}").ConfigureAwait(false);
        if (!response.StartsWith("OK ")) throw new InvalidOperationException($"Read failed '{address}': {response}");

        var payload = response[3..];
        var result = ConvertTo<T>(payload);
        return result;
    }

    public override async Task WriteAsync<T>(string address, T value)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address cannot be null or empty", nameof(address));
        if (value is null) throw new ArgumentNullException(nameof(value));

        var response = await SendCommandAsync($"WRITE {DeviceId} {address} {value}").ConfigureAwait(false);
        if (!response.StartsWith("OK")) throw new InvalidOperationException($"Write failed '{address}': {response}");

    }

    public override async Task<bool> IsConnectedAsync()
    {
        try
        {
            var resp = await SendCommandAsync("PING").ConfigureAwait(false);
            return resp == "PONG";
        }
        catch { return false; }
    }

    public override async Task EnsureConnectedAsync()
    {
        if (await IsConnectedAsync().ConfigureAwait(false)) return;
        throw new InvalidOperationException($"Cannot connect to TCP PLC at {_host}:{port}");
    }

    /// <summary>
    /// Gửi một lệnh đến TCP PLC và chờ phản hồi với xử lý timeout.
    /// Thao tác an toàn với thread sử dụng khóa I/O để ngăn chặn thực thi lệnh đồng thời.
    /// </summary>
    /// <param name="command">Lệnh cần gửi đến PLC.</param>
    /// <returns>Phản hồi nhận được từ PLC.</returns>
    private async Task<string> SendCommandAsync(string command)
    {
        using var _ = await _ioLock.LockAsync().ConfigureAwait(false);

        await EnsureConnectionAsync().ConfigureAwait(false);

        return await TimeoutHelper.RunWithTimeout(async ct =>
        {
            await _writer!.WriteLineAsync(command).ConfigureAwait(false);
            var response = await _reader!.ReadLineAsync(ct).ConfigureAwait(false);
            if (response is null) return "ERR NoResponse";
            return response;
        }, command.StartsWith("READ") ? PlcConfig.ReadTimeout : PlcConfig.WriteTimeout).ConfigureAwait(false);
    }

    /// <summary>
    /// Đảm bảo kết nối TCP được thiết lập và sẵn sàng cho giao tiếp.
    /// Tự động kết nối lại nếu kết nối hiện tại không hợp lệ.
    /// </summary>
    private async Task EnsureConnectionAsync()
    {
        if (_client?.Connected == true && _writer is not null && _reader is not null) return;

        CloseConnection();

        _client = new TcpClient();
        await _client.ConnectAsync(_host, port).ConfigureAwait(false);

        var stream = _client.GetStream();
        _writer = new StreamWriter(stream) { AutoFlush = true };
        _reader = new StreamReader(stream);
    }

    /// <summary>
    /// Chuyển đổi một giá trị chuỗi từ phản hồi TCP PLC sang kiểu đích được chỉ định.
    /// Xử lý các chuyển đổi kiểu thông thường với tối ưu hóa hiệu suất cho các kiểu cơ bản.
    /// </summary>
    /// <typeparam name="T">Kiểu đích để chuyển đổi.</typeparam>
    /// <param name="valueStr">Giá trị chuỗi nhận được từ PLC.</param>
    /// <returns>Giá trị đã chuyển đổi có kiểu T.</returns>
    /// <exception cref="InvalidCastException">Ném ra khi việc chuyển đổi thất bại.</exception>
    private static T ConvertTo<T>(string valueStr)
    {
        if (string.IsNullOrEmpty(valueStr))
            return default!;

        var targetType = typeof(T);

        // Handle common types directly for better performance
        if (targetType == typeof(bool) && bool.TryParse(valueStr, out var boolVal))
            return (T)(object)boolVal;

        if (targetType == typeof(int) && int.TryParse(valueStr, out var intVal))
            return (T)(object)intVal;

        if (targetType == typeof(string))
            return (T)(object)valueStr;

        // Fallback to general conversion
        try
        {
            return (T)Convert.ChangeType(valueStr, targetType);
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Cannot convert value '{valueStr}' to type '{targetType.Name}'", ex);
        }
    }

    /// <summary>
    /// Đóng kết nối TCP và giải phóng tất cả các tài nguyên liên quan.
    /// </summary>
    private void CloseConnection()
    {
        try { _reader?.Dispose(); } catch { }
        try { _writer?.Dispose(); } catch { }
        try { _client?.Close(); } catch { }
        _reader = null; _writer = null; _client = null;
    }

    /// <summary>
    /// Giải phóng tài nguyên kết nối TCP và xử lý lỗi disposal một cách mượt mà.
    /// </summary>
    protected override void DisposeCore()
    {
        CloseConnection();
    }
}
