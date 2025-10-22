namespace TQG.Automation.SDK.Communication;

/// <summary>
/// Lớp factory để tạo các instance kết nối PLC dựa trên profile thiết bị và loại giao thức.
/// Hỗ trợ giao thức S7 và TCP với tự động phát hiện giao thức dựa trên định dạng endpoint.
/// </summary>
internal static class PlcConnectorFactory
{
    /// <summary>
    /// Tạo một instance kết nối PLC dựa trên profile thiết bị và cấu hình giao thức.
    /// Tự động phát hiện loại giao thức từ định dạng endpoint: TCP endpoints sử dụng TcpPlcConnector, các loại khác sử dụng S7PlcConnector.
    /// </summary>
    /// <param name="profile">Profile thiết bị chứa chi tiết kết nối, endpoint và tham số thiết bị.</param>
    /// <param name="isTestMode">Chỉ ra có sử dụng endpoint test hay production từ profile.</param>
    /// <param name="plcConfig">Cấu hình PLC tùy chọn. Nếu null, sẽ sử dụng cấu hình mặc định.</param>
    /// <param name="logger">Instance logger tùy chọn. Nếu null, sẽ sử dụng NullLogger.</param>
    /// <returns>Một instance kết nối PLC được cấu hình cho giao thức đã chỉ định.</returns>
    /// <exception cref="ArgumentNullException">Ném ra khi profile là null.</exception>
    /// <exception cref="ArgumentException">Ném ra khi profile.Id hoặc endpoint là null hoặc rỗng.</exception>
    public static IPlcConnector Create(DeviceProfile profile, bool isTestMode, PlcConfiguration? plcConfig = null, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (string.IsNullOrWhiteSpace(profile.Id)) throw new ArgumentException("Device ID cannot be null or empty", nameof(profile));

        var endpoint = profile.GetEndpoint(isTestMode);
        if (string.IsNullOrWhiteSpace(endpoint)) throw new ArgumentException("Endpoint cannot be null or empty", nameof(profile));

        // Use provided config or create default
        var config = plcConfig ?? new PlcConfiguration();

        if (endpoint.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(endpoint);
            return new TcpPlcConnector(profile.Id, uri.Host, uri.Port, config, logger);
        }

        return new S7PlcConnector(
            deviceId: profile.Id,
            ipAddress: endpoint,
            cpuType: profile.Cpu,
            rack: profile.Rack,
            slot: profile.Slot,
            plcConfig: config,
            logger: logger
        );
    }
}