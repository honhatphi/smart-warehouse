namespace TQG.Automation.SDK.Services.BarcodeHandling;

/// <summary>
/// Xử lý kết quả validation barcode và ghi tín hiệu tới PLC.
/// Tách biệt logic validation khỏi CommandSender để dễ bảo trì hơn.
/// </summary>
internal sealed class ValidationResultHandler(
    DeviceMonitor deviceMonitor,
    IBarcodeHandler barcodeHandler,
    TaskDispatcher taskDispatcher)
{
    private readonly DeviceMonitor _deviceMonitor = deviceMonitor ?? throw new ArgumentNullException(nameof(deviceMonitor));
    private readonly IBarcodeHandler _barcodeHandler = barcodeHandler ?? throw new ArgumentNullException(nameof(barcodeHandler));
    private readonly TaskDispatcher _taskDispatcher = taskDispatcher ?? throw new ArgumentNullException(nameof(taskDispatcher));

    /// <summary>
    /// Xảy ra khi task thất bại trong quá trình xử lý kết quả validation.
    /// </summary>
    public event EventHandler<TaskFailedEventArgs>? TaskFailed;

    /// <summary>
    /// Gửi kết quả validation barcode tới thiết bị được chỉ định.
    /// Ghi các tín hiệu validation và thông tin routing tới PLC dựa trên kết quả validation.
    /// </summary>
    /// <param name="deviceId">Mã định danh duy nhất của thiết bị đích.</param>
    /// <param name="taskId">Mã định danh duy nhất của task.</param>
    /// <param name="isValid">True nếu barcode hợp lệ; ngược lại là false.</param>
    /// <param name="targetLocation">Vị trí đích cho barcode hợp lệ (tùy chọn).</param>
    /// <param name="direction">Hướng di chuyển của tài liệu.</param>
    /// <param name="gateNumber">Số gate để routing.</param>
    /// <returns>Task đại diện cho thao tác bất đồng bộ.</returns>
    /// <exception cref="DeviceNotRegisteredException">Ném ra khi thiết bị chưa được đăng ký.</exception>
    /// <exception cref="Exception">Ném ra khi ghi tín hiệu PLC thất bại.</exception>
    public async Task SendValidationResultAsync(
        string deviceId,
        string taskId,
        bool isValid,
        Location? targetLocation,
        Direction direction,
        short gateNumber)
    {
        try
        {       
            if (!_barcodeHandler.TryCompleteValidationTask(taskId, deviceId))
            {
                return;
            }

            var profile = _deviceMonitor.GetProfile(deviceId);
            var connector = _deviceMonitor.GetConnector(deviceId);
            var signals = profile.Signals;
            bool inDirBlockValue = direction != Direction.Bottom;

            if (isValid && targetLocation != null)
            {
                await WriteValidBarcodeSignals(connector, signals, targetLocation, gateNumber, inDirBlockValue);
            }
            else
            {
                await WriteInvalidBarcodeSignals(connector, signals);
            }
        }
        catch (DeviceNotRegisteredException)
        {
            _taskDispatcher.Pause();
            OnTaskFailed(deviceId, taskId, ErrorDetail.DeviceNotRegistered(deviceId));
        }
        catch (Exception ex)
        {
            _taskDispatcher.Pause();
            OnTaskFailed(deviceId, taskId, ErrorDetail.ValidationException(taskId, deviceId, ex));
        }
    }

    /// <summary>
    /// Ghi các tín hiệu PLC cho barcode hợp lệ bao gồm thông tin routing.
    /// </summary>
    /// <param name="connector">PLC connector để ghi tín hiệu.</param>
    /// <param name="signals">Signal map chứa các địa chỉ tín hiệu.</param>
    /// <param name="targetLocation">Vị trí đích để routing.</param>
    /// <param name="gateNumber">Số gate để routing.</param>
    /// <param name="inDirBlockValue">Giá trị cho tín hiệu InDirBlock.</param>
    /// <returns>Task đại diện cho thao tác bất đồng bộ.</returns>
    private static async Task WriteValidBarcodeSignals(
        IPlcConnector connector,
        SignalMap signals,
        Location targetLocation,
        short gateNumber,
        bool inDirBlockValue)
    {
        await connector.WriteAsync(signals.BarcodeValid, true);
        await connector.WriteAsync(signals.BarcodeInvalid, false);

        await connector.WriteAsync(signals.TargetFloor, targetLocation.Floor);
        await connector.WriteAsync(signals.TargetRail, targetLocation.Rail);
        await connector.WriteAsync(signals.TargetBlock, targetLocation.Block);
        await connector.WriteAsync(signals.InDirBlock, inDirBlockValue);
        await connector.WriteAsync(signals.GateNumber, gateNumber);
    }

    /// <summary>
    /// Ghi các tín hiệu PLC cho barcode không hợp lệ.
    /// </summary>
    /// <param name="connector">PLC connector để ghi tín hiệu.</param>
    /// <param name="signals">Signal map chứa các địa chỉ tín hiệu.</param>
    /// <returns>Task đại diện cho thao tác bất đồng bộ.</returns>
    private static async Task WriteInvalidBarcodeSignals(IPlcConnector connector, SignalMap signals)
    {
        await connector.WriteAsync(signals.BarcodeValid, false);
        await connector.WriteAsync(signals.BarcodeInvalid, true);
    }

    /// <summary>
    /// Phát sinh event TaskFailed với thông tin lỗi chi tiết.
    /// </summary>
    /// <param name="deviceId">Mã định danh thiết bị gặp lỗi.</param>
    /// <param name="taskId">Mã định danh task thất bại.</param>
    /// <param name="error">Chi tiết thông tin lỗi.</param>
    private void OnTaskFailed(string deviceId, string taskId, ErrorDetail error)
    {
        TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, taskId, error));
    }
}
