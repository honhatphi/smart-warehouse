using System.Collections.Concurrent;

namespace TQG.Automation.SDK.Services.DeviceManagement.Monitoring;

/// <summary>
/// Theo dõi và quản lý trạng thái của các thiết bị tự động hóa.
/// Cung cấp chức năng theo dõi kết nối thiết bị, thay đổi trạng thái và thông tin vị trí.
/// </summary>
///
/// Tổng quan luồng dữ liệu (đường dẫn thiết bị/PLC):
/// - DeviceMonitor tương tác với PlcConnectionPool để lấy các instance IPlcConnector cho thiết bị.
/// - Nó đọc các tín hiệu trạng thái/vị trí từ PLC để xác định Idle/Busy/Offline và phát ra
///   các sự kiện DeviceStatusChanged được TaskDispatcher và các component khác sử dụng.
/// - StartMonitoring/StopMonitoring quản lý các token theo dõi từng thiết bị và kết nối PLC.
///
/// <remarks>
/// Khởi tạo một instance mới của lớp DeviceMonitor.
/// </remarks>
/// <param name="profiles">Từ điển các hồ sơ thiết bị.</param>
/// <param name="isTestMode">Chế độ hoạt động cho hệ thống tự động hóa.</param>
/// <param name="config">Cấu hình cho hành vi theo dõi thiết bị.</param>
/// <param name="connectionPool">Instance pool kết nối PLC tùy chọn.</param>
/// <exception cref="ArgumentNullException">Ném ra khi profiles hoặc config là null.</exception>
internal sealed class DeviceMonitor(
    IReadOnlyDictionary<string, DeviceProfile> profiles,
    bool isTestMode,
    DeviceMonitorConfiguration config,
    PlcConnectionPool? connectionPool = null,
    ILogger? logger = null) : IDeviceMonitor
{
    #region Fields and Properties
    private readonly IReadOnlyDictionary<string, DeviceProfile> _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
    private readonly PlcConnectionPool _connectionPool = connectionPool ?? new PlcConnectionPool(isTestMode);
    private readonly DeviceMonitorConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));
    private readonly bool _isTestMode = isTestMode;
    private readonly ConcurrentDictionary<string, DeviceStatus> _deviceStatuses = new();
    private readonly Dictionary<string, CancellationTokenSource> _monitoringTokens = [];
    private readonly object _lock = new();
    private readonly object _statusLock = new();
    private readonly ILogger _logger = logger ?? NullLogger.Instance;
    private bool _disposed = false;

    public event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;

    public IReadOnlyDictionary<string, DeviceProfile> DeviceProfiles => _profiles;
    #endregion

    #region Device Monitoring

    public async Task StartMonitoring(string deviceId)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        _logger.LogInformation($"Starting monitoring for device {deviceId}");

        try
        {
            // If device already busy we keep current behavior: log and return early.
            if (GetDeviceStatus(deviceId) == DeviceStatus.Busy)
            {
                return;
            }

            var profile = GetProfile(deviceId);
            var connector = GetConnector(deviceId);

            bool isReady = await connector.ReadAsync<bool>(profile.Signals.DeviceReady);
            var newStatus = isReady ? DeviceStatus.Idle : DeviceStatus.Busy;

            UpdateDeviceStatus(deviceId, newStatus);

            _logger.LogInformation($"Monitoring started for device {deviceId} (status: {newStatus})");
        }
        catch
        {
            _logger.LogWarning($"Failed to start monitoring device {deviceId}; marking offline");
            UpdateDeviceStatus(deviceId, DeviceStatus.Offline);
            throw;
        }
    }

    public void StopMonitoring(string deviceId)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return;
        }

        _logger.LogInformation($"Stopping monitoring for device {deviceId}");

        CancellationTokenSource? toCancel = null;

        try
        {
            // Remove token under lock to avoid races, but cancel/dispose outside lock to avoid
            // long-running operations while holding the lock.
            lock (_lock)
            {
                if (_monitoringTokens.TryGetValue(deviceId, out var cts))
                {
                    _monitoringTokens.Remove(deviceId);
                    toCancel = cts;
                }
            }

            if (toCancel != null)
            {
                try { toCancel.Cancel(); } catch { }
                try { toCancel.Dispose(); } catch { }
            }

            if (_connectionPool.RemoveConnection(deviceId))
            {
            }

            _deviceStatuses.TryRemove(deviceId, out _);

            _logger.LogInformation($"Stopped monitoring for device {deviceId}");
        }
        catch (Exception)
        {
            _logger.LogWarning($"Error while stopping monitoring for device {deviceId}");
        }
    }
    #endregion

    #region Device Status Management
    public bool IsConnected(string deviceId)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(deviceId))
            return false;

        var status = GetDeviceStatus(deviceId);
        return status is DeviceStatus.Idle or DeviceStatus.Busy;
    }

    public DeviceStatus GetDeviceStatus(string deviceId)
    {
        ThrowIfDisposed();
        lock (_statusLock)
        {
            return _deviceStatuses.GetValueOrDefault(deviceId, DeviceStatus.Offline);
        }
    }

    // Centralized internal method that updates device status and raises the
    // DeviceStatusChanged event if the status actually changed. This method
    // intentionally does not call GetDeviceStatus() while holding the
    // `_statusLock` to avoid re-entrancy/deadlock (GetDeviceStatus also
    // locks `_statusLock`). All status updates should go through this method.
    internal void UpdateDeviceStatus(string deviceId, DeviceStatus newStatus)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(deviceId))
            return;

        lock (_statusLock)
        {
            // Read existing status without calling GetDeviceStatus (avoids double-lock)
            _deviceStatuses.TryGetValue(deviceId, out var oldStatus);

            if (oldStatus != newStatus)
            {
                _deviceStatuses[deviceId] = newStatus;
                DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs(deviceId, newStatus, oldStatus));
            }
        }
    }

    public async Task<bool> ResetDeviceStatusAsync(string deviceId)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(deviceId))
            return false;

        // Check internal status first
        if (GetDeviceStatus(deviceId) == DeviceStatus.Busy)
            return false;

        try
        {
            // Check actual PLC status before resetting
            var connector = GetConnector(deviceId);
            var profile = GetProfile(deviceId);
            
            bool alarm = await connector.ReadAsync<bool>(profile.Signals.Alarm);
            short errorCode = await connector.ReadAsync<short>(profile.Signals.ErrorCode);

            // Only reset if device is actually clear (no alarm and error code is 0)
            if (alarm || errorCode != 0)
            {
                _logger.LogWarning($"Cannot reset device {deviceId}: alarm={alarm}, errorCode={errorCode}");
                return false;
            }

            UpdateDeviceStatus(deviceId, DeviceStatus.Idle);
            _logger.LogInformation($"Device {deviceId} status reset to Idle");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to reset device {deviceId} status: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Device Information
    public IPlcConnector GetConnector(string deviceId)
    {
        ThrowIfDisposed();

        DeviceProfile profile = GetProfile(deviceId);
        return _connectionPool.GetConnection(profile);
    }

    public DeviceProfile GetProfile(string deviceId)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        _profiles.TryGetValue(deviceId, out var profile);

        if (profile is null)
        {
            throw new DeviceNotRegisteredException(deviceId);
        }

        return profile;
    }

    public async Task<bool> ReadDeviceReadyAsync(string deviceId)
    {
        var profile = GetProfile(deviceId);
        var connector = GetConnector(deviceId);

        try
        {
            return await connector.ReadAsync<bool>(profile.Signals.DeviceReady);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to read device ready state for {deviceId}: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> ReadConnectionStatusAsync(string deviceId)
    {
        var profile = GetProfile(deviceId);
        var connector = GetConnector(deviceId);

        try
        {
            return await connector.ReadAsync<bool>(profile.Signals.ConnectedToSoftware);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to read connection status for {deviceId}: {ex.Message}");
            throw;
        }
    }
    #endregion

    #region Device Location and Idle Devices
    public async Task<List<DeviceInfo>> GetIdleDevices()
    {
        ThrowIfDisposed();

        try
        {
            var concurrencyLimiter = new SemaphoreSlim(_config.MaxConcurrentOperations);

            var deviceInfoTasks = DeviceProfiles.Keys
                .Select(async deviceId =>
                {
                    await concurrencyLimiter.WaitAsync();
                    try
                    {
                        return await GetDeviceInfoIfIdle(deviceId);
                    }
                    finally
                    {
                        concurrencyLimiter.Release();
                    }
                });

            var deviceInfoResults = await Task.WhenAll(deviceInfoTasks);
            var idleDevices = deviceInfoResults.Where(info => info != null).Cast<DeviceInfo>().ToList();

            return idleDevices;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting idle devices", ex);
            return [];
        }
    }

    private async Task<DeviceInfo?> GetDeviceInfoIfIdle(string deviceId)
    {
        try
        {
            var profile = GetProfile(deviceId);
            var connector = GetConnector(deviceId);

            bool isBusy = await connector.ReadAsync<bool>(profile.Signals.CommandAcknowledged);
            if (isBusy)
            {
                UpdateDeviceStatus(deviceId, DeviceStatus.Busy);
                return null;
            }

            var location = await GetCurrentLocationAsync(deviceId);
            if (location != null)
            {
                UpdateDeviceStatus(deviceId, DeviceStatus.Idle);
                return new DeviceInfo(deviceId, location);
            }

            _logger.LogWarning($"Device {deviceId} is idle but location could not be read");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error checking device {deviceId} status", ex);
            UpdateDeviceStatus(deviceId, DeviceStatus.Offline);
            return null;
        }
    }

    #endregion

    #region Public Location Methods
    public async Task<Location?> GetCurrentLocationAsync(string deviceId)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(deviceId))
            return null;

        try
        {
            var profile = GetProfile(deviceId);
            var connector = GetConnector(deviceId);
            var signals = profile.Signals;

            var locationTasks = new Task<short>[]
            {
                connector.ReadAsync<short>(signals.ActualFloor),
                connector.ReadAsync<short>(signals.ActualRail),
                connector.ReadAsync<short>(signals.ActualBlock)
            };

            await Task.WhenAll(locationTasks);

            // Tasks are completed after Task.WhenAll; read results directly from the tasks
            // to avoid awaiting the same tasks again (double-await).
            // This is safe because Task.WhenAll ensures completion or faulting.
            var floor = locationTasks[0].Result;
            var rail = locationTasks[1].Result;
            var block = locationTasks[2].Result;

            return new Location(floor, rail, block);
        }
        catch (Exception)
        {
            // Returning null signals the caller that we couldn't read location data from the PLC.
            // This preserves previous behavior: callers treat null as 'location unavailable'
            // (e.g., device considered not idle or skipped). We intentionally swallow the
            // PLC read exception here to keep monitoring resilient and avoid noisy failures
            // higher up the stack.
            return null;
        }
    }
    #endregion

    #region Reset System
    public async Task ResetSystemAsync(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId, nameof(deviceId));
        ThrowIfDisposed();

        // Get device profile and connector
        var profile = GetProfile(deviceId);
        var connector = GetConnector(deviceId);
        var signals = profile.Signals;

        // Security check: Verify all signals are in DB66 range
        if (!ValidateDb66Signals(signals))
        {
            throw new InvalidOperationException("Reset operation requires all signals to be in DB66 range for security validation.");
        }

        try
        {
            _logger.LogInformation($"Starting system reset for device {deviceId} in test mode");

            // Reset all command signals to false
            await ResetCommandSignals(connector, signals);

            // Reset all status signals to false
            await ResetStatusSignals(connector, signals);

            // Reset location data to default values
            await ResetLocationData(connector, signals);

            // Reset barcode data to 0
            await ResetBarcodeData(connector, signals);

            // Reset direction and gate data
            await ResetDirectionAndGateData(connector, signals);

            // Reset device status to idle
            UpdateDeviceStatus(deviceId, DeviceStatus.Idle);

            _logger.LogInformation($"System reset completed successfully for device {deviceId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to reset system for device {deviceId}", ex);
            throw;
        }
    }

    /// Validates that all signals are in DB66 range for security.
    private static bool ValidateDb66Signals(SignalMap signals)
    {
        var allSignals = new[]
        {
            signals.InboundCommand, signals.OutboundCommand, signals.TransferCommand,
            signals.StartProcessCommand, signals.CommandAcknowledged, signals.CommandRejected,
            signals.InboundComplete, signals.OutboundComplete, signals.TransferComplete,
            signals.Alarm, signals.OutDirBlock, signals.InDirBlock, signals.GateNumber,
            signals.SourceFloor, signals.SourceRail, signals.SourceBlock,
            signals.TargetFloor, signals.TargetRail, signals.TargetBlock,
            signals.BarcodeValid, signals.BarcodeInvalid, signals.ActualFloor,
            signals.ActualRail, signals.ActualBlock, signals.ErrorCode,
            signals.BarcodeChar1, signals.BarcodeChar2, signals.BarcodeChar3,
            signals.BarcodeChar4, signals.BarcodeChar5, signals.BarcodeChar6,
            signals.BarcodeChar7, signals.BarcodeChar8, signals.BarcodeChar9, signals.BarcodeChar10
        };

        return allSignals.All(signal => signal.StartsWith("DB66", StringComparison.OrdinalIgnoreCase));
    }

    /// Resets all command signals to false.
    private static async Task ResetCommandSignals(IPlcConnector connector, SignalMap signals)
    {
        await Task.WhenAll(
            connector.WriteAsync(signals.InboundCommand, false),
            connector.WriteAsync(signals.OutboundCommand, false),
            connector.WriteAsync(signals.TransferCommand, false),
            connector.WriteAsync(signals.StartProcessCommand, false)
        );
    }

    /// Resets all status signals to false.
    private static async Task ResetStatusSignals(IPlcConnector connector, SignalMap signals)
    {
        await Task.WhenAll(
            connector.WriteAsync(signals.CommandAcknowledged, false),
            connector.WriteAsync(signals.CommandRejected, false),
            connector.WriteAsync(signals.InboundComplete, false),
            connector.WriteAsync(signals.OutboundComplete, false),
            connector.WriteAsync(signals.TransferComplete, false),
            connector.WriteAsync(signals.Alarm, false),
            connector.WriteAsync(signals.BarcodeValid, false),
            connector.WriteAsync(signals.BarcodeInvalid, false)
        );
    }

    /// Resets location data to default values.
    private static async Task ResetLocationData(IPlcConnector connector, SignalMap signals)
    {
        await Task.WhenAll(
            connector.WriteAsync(signals.SourceFloor, (short)0),
            connector.WriteAsync(signals.SourceRail, (short)0),
            connector.WriteAsync(signals.SourceBlock, (short)0),
            connector.WriteAsync(signals.TargetFloor, (short)0),
            connector.WriteAsync(signals.TargetRail, (short)0),
            connector.WriteAsync(signals.TargetBlock, (short)0),
            connector.WriteAsync(signals.ActualFloor, (short)0),
            connector.WriteAsync(signals.ActualRail, (short)0),
            connector.WriteAsync(signals.ActualBlock, (short)0)
        );
    }

    /// Resets barcode data to 0.
    private static async Task ResetBarcodeData(IPlcConnector connector, SignalMap signals)
    {
        await Task.WhenAll(
            connector.WriteAsync(signals.BarcodeChar1, (short)0),
            connector.WriteAsync(signals.BarcodeChar2, (short)0),
            connector.WriteAsync(signals.BarcodeChar3, (short)0),
            connector.WriteAsync(signals.BarcodeChar4, (short)0),
            connector.WriteAsync(signals.BarcodeChar5, (short)0),
            connector.WriteAsync(signals.BarcodeChar6, (short)0),
            connector.WriteAsync(signals.BarcodeChar7, (short)0),
            connector.WriteAsync(signals.BarcodeChar8, (short)0),
            connector.WriteAsync(signals.BarcodeChar9, (short)0),
            connector.WriteAsync(signals.BarcodeChar10, (short)0)
        );
    }

    /// Resets direction and gate data to default values.
    private static async Task ResetDirectionAndGateData(IPlcConnector connector, SignalMap signals)
    {
        await Task.WhenAll(
            connector.WriteAsync(signals.OutDirBlock, false),
            connector.WriteAsync(signals.InDirBlock, false),
            connector.WriteAsync(signals.GateNumber, (short)0),
            connector.WriteAsync(signals.ErrorCode, (short)0)
        );
    }
    #endregion

    #region Disposal
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(DeviceMonitor));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            lock (_lock)
            {
                foreach (var kvp in _monitoringTokens)
                {
                    kvp.Value.Cancel();
                    kvp.Value.Dispose();
                }
                _monitoringTokens.Clear();
            }

            _connectionPool?.Dispose();
            _deviceStatuses.Clear();
        }
        catch (Exception)
        {
        }
    }
    #endregion
}