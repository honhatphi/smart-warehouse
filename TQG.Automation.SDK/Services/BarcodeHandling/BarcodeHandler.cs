using System.Collections.Concurrent;
using System.Threading.Channels;

namespace TQG.Automation.SDK.Services.BarcodeHandling;

/// <summary>
/// Xử lý các thao tác đọc và validation barcode cho thiết bị tự động hóa.
/// Cung cấp chức năng đọc barcode từ thiết bị PLC và quản lý workflow validation barcode.
/// </summary>
///
/// Tổng quan luồng dữ liệu (đường đi barcode):
/// - ReadBarcodeAsync đọc tuần tự các signal ký tự barcode từ PLC thông qua connector.
/// - SendBarcodeAsync đưa BarcodeRequest vào channel validation chia sẻ và chờ
///   TaskCompletionSource sẽ được hoàn thành bởi consumer của channel (validation loop).
/// - Validation loop (ProcessValidationQueue) phát sinh BarcodeReceived events để điều khiển
///   logic validation cấp cao hơn và cuối cùng gọi TryCompleteValidationTask.
internal sealed class BarcodeHandler : IBarcodeHandler
{
    #region Fields and Properties
    private readonly DeviceMonitor _monitor;
    private readonly Channel<BarcodeRequest> _validationChannel;
    private readonly ConcurrentDictionary<string, (string DeviceId, TaskCompletionSource<bool> Tcs, CancellationTokenSource Cts)> _pendingTasks = new();
    private readonly Task _validationTask;
    private readonly BarcodeHandlerConfiguration _config;
    private readonly SemaphoreSlim _cleanupSemaphore = new(1, 1);
    private readonly ILogger _logger;
    private bool _disposed = false;

    public event EventHandler<BarcodeReceivedEventArgs>? BarcodeReceived;

    public event EventHandler<TaskFailedEventArgs>? TaskFailed;
    #endregion

    #region Constructor
    /// <summary>
    /// Khởi tạo instance mới của class BarcodeHandler.
    /// </summary>
    /// <param name="monitor">Device monitor để theo dõi trạng thái thiết bị.</param>
    /// <param name="validationChannel">Channel cho các yêu cầu validation barcode.</param>
    /// <param name="config">Cấu hình cho hành vi xử lý barcode.</param>
    /// <param name="logger">Logger để ghi log các thao tác (tùy chọn).</param>
    /// <exception cref="ArgumentNullException">Ném ra khi bất kỳ tham số nào là null.</exception>
    public BarcodeHandler(
        DeviceMonitor monitor,
        Channel<BarcodeRequest> validationChannel,
        BarcodeHandlerConfiguration config,
        ILogger? logger = null)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _validationChannel = validationChannel ?? throw new ArgumentNullException(nameof(validationChannel));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? NullLogger.Instance;
        _validationTask = ProcessValidationQueue(validationChannel);

        _logger.LogInformation($"BarcodeHandler initialized with max barcode length {_config.MaxBarcodeLength} and timeout {_config.ValidationTimeoutMinutes} minutes");
    }
    #endregion

    #region Barcode Reading

    public async Task<string> ReadBarcodeAsync(IPlcConnector connector, SignalMap signals)
    {
        try
        {
            var readTasks = new Task<string>[]
            {
                connector.ReadAsync<string>(signals.BarcodeChar1),
                connector.ReadAsync<string>(signals.BarcodeChar2),
                connector.ReadAsync<string>(signals.BarcodeChar3),
                connector.ReadAsync<string>(signals.BarcodeChar4),
                connector.ReadAsync<string>(signals.BarcodeChar5),
                connector.ReadAsync<string>(signals.BarcodeChar6),
                connector.ReadAsync<string>(signals.BarcodeChar7),
                connector.ReadAsync<string>(signals.BarcodeChar8),
                connector.ReadAsync<string>(signals.BarcodeChar9),
                connector.ReadAsync<string>(signals.BarcodeChar10)
            };

            await Task.WhenAll(readTasks).ConfigureAwait(false);

            string result = string.Empty;
            for (int i = 0; i < _config.MaxBarcodeLength; i++)
            {
                string val = await readTasks[i].ConfigureAwait(false);

                if (val.Length > 1)
                {
                    break;
                }

                if (val.Length == 1)
                {
                    result += val;
                }
                else if (string.IsNullOrEmpty(val))
                {
                    break;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to read barcode from device {connector.DeviceId}: {ex.Message}");
            return string.Empty;
        }
    }
    #endregion

    #region Barcode Validation
    public async Task SendBarcodeAsync(string deviceId, string taskId, string barcode)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(BarcodeHandler));

        var tcs = new TaskCompletionSource<bool>();
        var cts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMinutes(_config.ValidationTimeoutMinutes);

        try
        {
            _logger.LogInformation($"Sending barcode '{barcode}' for validation. Device: {deviceId}, Task: {taskId}");

            _pendingTasks[taskId] = (deviceId, tcs, cts);
            var actualLocation = await _monitor.GetCurrentLocationAsync(deviceId);
            var req = new BarcodeRequest
            {
                DeviceId = deviceId,
                TaskId = taskId,
                Barcode = barcode,
                ActualLocation = actualLocation
            };

            // Try to write without blocking indefinitely. If the channel is full,
            // perform bounded retries with a short delay. If still full, log and
            // fail the validation for this task to avoid blocking the producer.
            var enqueued = false;
            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (_validationChannel.Writer.TryWrite(req))
                {
                    enqueued = true;
                    break;
                }

                if (attempt == 1)
                {
                    _logger.LogWarning($"Validation channel full; will retry enqueueing barcode for device {deviceId}, task {taskId}");
                }

                await Task.Delay(100).ConfigureAwait(false);
            }

            if (!enqueued)
            {
                _logger.LogWarning($"Dropping barcode validation for device {deviceId}, task {taskId} after {maxAttempts} attempts (channel full)");
                // Clean up and signal failure
                _pendingTasks.TryRemove(taskId, out _);
                try { cts.Cancel(); } catch { }
                try { cts.Dispose(); } catch { }
                TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, taskId, ErrorDetail.ValidationException(taskId, deviceId, new InvalidOperationException("Validation channel full"))));
                throw new InvalidOperationException("Validation channel is full, cannot enqueue barcode");
            }

            await tcs.Task.WaitAsync(timeout, cts.Token);

            _logger.LogInformation($"Barcode validation completed for task {taskId}");
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            _logger.LogWarning($"Barcode validation cancelled for task {taskId}");
            throw;
        }
        catch (TimeoutException)
        {
            _logger.LogError($"Barcode validation timeout for task {taskId} after {_config.ValidationTimeoutMinutes} minutes", null);
            TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, taskId, ErrorDetail.Timeout(deviceId, taskId, timeout)));
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Barcode validation failed for task {taskId}", ex);
            TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, taskId, ErrorDetail.Unknown(deviceId, taskId, ex.Message)));
            throw;
        }
        finally
        {
            cts.Dispose();
            _pendingTasks.TryRemove(taskId, out _);
        }
    }

    public bool TryCompleteValidationTask(string taskId, string deviceId)
    {
        if (_disposed)
        {
            _logger.LogWarning($"Cannot complete validation task {taskId} - handler is disposed");
            return false;
        }

        try
        {
            if (!_pendingTasks.TryGetValue(taskId, out var entry))
            {
                _logger.LogWarning($"Validation task {taskId} not found in pending tasks");
                TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, taskId, ErrorDetail.NotFoundTask(deviceId, taskId)));
                return false;
            }

            if (entry.DeviceId != deviceId)
            {
                _logger.LogError($"Device mismatch for task {taskId}. Expected: {entry.DeviceId}, Actual: {deviceId}", null);
                entry.Cts.Cancel();
                _pendingTasks.TryRemove(taskId, out _);
                TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, taskId, ErrorDetail.MismatchedDevice(taskId, entry.DeviceId, deviceId)));
                return false;
            }

            if (_pendingTasks.TryRemove(taskId, out _))
            {
                entry.Tcs.SetResult(true);
                entry.Cts.Dispose();
                _logger.LogInformation($"Successfully completed validation task {taskId} for device {deviceId}");
                return true;
            }

            _logger.LogWarning($"Failed to remove task {taskId} from pending tasks");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error completing validation task {taskId}", ex);
            return false;
        }
    }
    #endregion

    #region Private Helper Methods
    private Task ProcessValidationQueue(Channel<BarcodeRequest> validationChannel)
        => Task.Run(async () =>
        {
            try
            {
                await foreach (var req in validationChannel.Reader.ReadAllAsync())
                {
                    if (_disposed)
                    {
                        break;
                    }

                    BarcodeReceived?.Invoke(this, new BarcodeReceivedEventArgs(req.DeviceId, req.TaskId, req.Barcode));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in validation queue processing", ex);
            }
        });
    #endregion

    #region Disposal
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _logger.LogInformation($"Disposing BarcodeHandler with {_pendingTasks.Count} pending validation tasks");

        try
        {
            _validationChannel.Writer.Complete();
            _validationTask.Wait(TimeSpan.FromSeconds(5));

            foreach ((_, _, CancellationTokenSource Cts) in _pendingTasks.Values)
            {
                try
                {
                    Cts.Cancel();
                    Cts.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error disposing validation task cancellation token: {ex.Message}");
                }
            }
            _pendingTasks.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during BarcodeHandler disposal", ex);
        }
        finally
        {
            _cleanupSemaphore.Dispose();
        }
    }
    #endregion
}