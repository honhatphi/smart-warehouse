using System.Text.Json;

namespace TQG.Automation.SDK;

/// <summary>
/// Triển khai singleton thread-safe của AutomationGateway.
/// Cung cấp khởi tạo lazy và khả năng giải phóng tài nguyên an toàn.
/// </summary>
public sealed class AutomationGateway : AutomationGatewayBase
{
    private static readonly object _lock = new();
    private static volatile AutomationGateway? _instance;
    private static volatile GatewayState _state = GatewayState.Uninitialized;

    private const string DisposedErrorMessage = "AutomationGateway has been disposed and cannot be accessed. This instance cannot be reused; create a new process or use the internal Reset() during testing to recreate the singleton.";
    private const string NotInitializedErrorMessage = "AutomationGateway has not been initialized. Call AutomationGateway.Initialize(config) before accessing the Instance.";
    private const string AlreadyInitializedErrorMessage = "AutomationGateway has already been initialized. It can only be initialized once per process.";
    private const string DisposedReinitializeErrorMessage = "AutomationGateway has been disposed and cannot be reinitialized. For tests, use the internal Reset() to clear state.";

    /// <summary>
    /// Lấy instance singleton của AutomationGateway.
    /// </summary>
    /// <exception cref="InvalidOperationException">Ném ra khi gateway chưa được khởi tạo hoặc đã bị dispose.</exception>
    public static AutomationGateway Instance
    {
        get
        {
            var currentState = _state; // Direct read is atomic for enum
            
            if (currentState == GatewayState.Disposed)
                throw new InvalidOperationException(DisposedErrorMessage);
            
            if (currentState != GatewayState.Initialized)
                throw new InvalidOperationException(NotInitializedErrorMessage);

            return _instance!;
        }
    }

    /// <summary>
    /// Constructor private để ngăn chặn khởi tạo từ bên ngoài.
    /// </summary>
    private AutomationGateway(List<DeviceProfile> devices, ApplicationConfiguration config) : base(devices, config)
    {
    }

    /// <summary>
    /// Khởi tạo singleton instance với cấu hình được cung cấp.
    /// Phương thức này thread-safe và chỉ có thể gọi một lần duy nhất.
    /// </summary>
    /// <param name="config">Cấu hình ứng dụng.</param>
    /// <returns>Instance singleton của AutomationGateway.</returns>
    /// <exception cref="ArgumentNullException">Ném ra khi config là null.</exception>
    /// <exception cref="ArgumentException">Ném ra khi không tìm thấy thiết bị nào trong cấu hình.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi đã được khởi tạo trước đó hoặc đã bị dispose.</exception>
    public static AutomationGateway Initialize(ApplicationConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.Devices == null || config.Devices.Count == 0)
            throw new ArgumentException("Configuration must contain at least one device.", nameof(config));

        return InitializeInternal(config.Devices, config);
    }

    public static AutomationGateway Initialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        
        ApplicationConfiguration config = JsonSerializer.Deserialize<ApplicationConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new ArgumentException("Invalid JSON configuration format.");

        return Initialize(config);
    }

    private static AutomationGateway InitializeInternal(List<DeviceProfile> devices, ApplicationConfiguration config)
    {
        ThrowIfStateInvalid(_state, allowUninitialized: true);

        lock (_lock)
        {
            // Double-check inside lock
            ThrowIfStateInvalid(_state, allowUninitialized: true);

            _instance = new AutomationGateway(devices, config);
            _state = GatewayState.Initialized;
            return _instance;
        }
    }

    /// <summary>
    /// Phương thức helper để kiểm tra trạng thái và ném ngoại lệ phù hợp.
    /// </summary>
    /// <param name="currentState">Trạng thái hiện tại của gateway.</param>
    /// <param name="allowUninitialized">Có cho phép trạng thái chưa khởi tạo hay không.</param>
    private static void ThrowIfStateInvalid(GatewayState currentState, bool allowUninitialized = false)
    {
        switch (currentState)
        {
            case GatewayState.Disposed:
                throw new InvalidOperationException(DisposedReinitializeErrorMessage);
            case GatewayState.Initialized when allowUninitialized:
                throw new InvalidOperationException(AlreadyInitializedErrorMessage);
            case GatewayState.Uninitialized when !allowUninitialized:
                throw new InvalidOperationException(NotInitializedErrorMessage);
        }
    }

    /// <summary>
    /// Kiểm tra xem AutomationGateway đã được khởi tạo hay chưa.
    /// </summary>
    public static bool IsInitialized => _state == GatewayState.Initialized;

    /// <summary>
    /// Kiểm tra xem AutomationGateway đã bị dispose hay chưa.
    /// </summary>
    public static bool IsDisposed => _state == GatewayState.Disposed;

    /// <summary>
    /// Dispose singleton instance và đặt lại trạng thái khởi tạo.
    /// Cho phép gateway có thể được khởi tạo lại nếu cần.
    /// </summary>
    public static void DisposeInstance()
    {
        lock (_lock)
        {
            var currentState = _state;
            
            if (currentState != GatewayState.Initialized) // Not initialized or already disposed
                return;

            _instance?.Dispose();
            _instance = null;
            _state = GatewayState.Disposed;
        }
    }

    /// <summary>
    /// Đặt lại singleton về trạng thái chưa khởi tạo.
    /// Chỉ nên sử dụng cho mục đích testing.
    /// </summary>
    internal static void Reset()
    {
        // INTERNAL / TEST-ONLY: Reset the singleton state so tests can reinitialize the gateway.
        lock (_lock)
        {
            _instance?.Dispose();
            _instance = null;
            _state = GatewayState.Uninitialized;
        }
    }

}