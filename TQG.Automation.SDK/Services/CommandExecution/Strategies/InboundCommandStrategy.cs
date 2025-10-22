namespace TQG.Automation.SDK.Services.CommandExecution.Strategies;

/// <summary>
/// Strategy xử lý thực thi lệnh inbound (nhập kho).
/// </summary>
internal sealed class InboundCommandStrategy(
    DeviceMonitor deviceMonitor,
    IBarcodeHandler barcodeHandler,
    TaskDispatcher taskDispatcher,
    ILogger logger) : BaseCommandExecutionStrategy(deviceMonitor, barcodeHandler, taskDispatcher, logger)
{
    public override CommandType CommandType => CommandType.Inbound;

    public override async Task TriggerCommandAsync(IPlcConnector connector, SignalMap signals, TransportTask task)
    {
        Logger.LogInformation($"[{CommandType}] Triggering inbound command for task {task.TaskId}");
        await WriteTriggerFlagsAsync(connector, signals);
        await WriteTargetLocationAsync(connector, signals, task);
        Logger.LogInformation($"[{CommandType}] Inbound command triggered successfully for task {task.TaskId}");
    }

    public override async Task StartPollingAsync(
        string deviceId,
        string taskId,
        IPlcConnector connector,
        SignalMap signals,
        int timeoutMinutes,
        CancellationToken cancellationToken)
    {
        await ExecuteInboundPollingLoopAsync(
            deviceId,
            taskId,
            connector,
            signals,
            timeoutMinutes,
            cancellationToken);
    }

    private async Task ExecuteInboundPollingLoopAsync(
        string deviceId,
        string taskId,
        IPlcConnector connector,
        SignalMap signals,
        int timeoutMinutes,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation($"[{CommandType}] Starting inbound polling loop for device {deviceId}, task {taskId}");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromMinutes(timeoutMinutes);
            bool barcodeProcessed = false;
            string defaultBarcode = "0000000000";

            while (DateTime.UtcNow - startTime < timeout && !cancellationToken.IsCancellationRequested)
            {
                if (!await timer.WaitForNextTickAsync(cancellationToken)) break;

                // Check for device errors
                if (await CheckDeviceErrorsAsync(connector, signals, deviceId, taskId))
                {
                    break;
                }

                // Process barcode if not already processed
                if (!barcodeProcessed)
                {
                    string barcode = await BarcodeHandler.ReadBarcodeAsync(connector, signals);

                    if (!string.IsNullOrEmpty(barcode) && barcode != defaultBarcode)
                    {
                        Logger.LogInformation($"[{CommandType}] Barcode read: {barcode} for device {deviceId}, task {taskId}");
                        await BarcodeHandler.SendBarcodeAsync(deviceId, taskId, barcode);
                        barcodeProcessed = true;
                        Logger.LogInformation($"[{CommandType}] Barcode sent successfully for device {deviceId}, task {taskId}");
                    }
                    else
                    {
                        continue;
                    }
                }

                // Check completion
                if (await CheckInboundCompletionAsync(connector, signals, deviceId, taskId))
                {
                    break;
                }
            }

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed >= timeout)
            {
                Logger.LogWarning($"[{CommandType}] Inbound polling timeout reached for device {deviceId}, task {taskId} after {elapsed.TotalMinutes:F1} minutes");
            }
        }
        catch (OperationCanceledException)
        {
            // Inbound polling cancelled
        }
        catch (Exception ex)
        {
            Logger.LogError($"[{CommandType}] Inbound polling exception for device {deviceId}, task {taskId}", ex);
            OnTaskFailed(deviceId, taskId, ErrorDetail.PollingException("Inbound", deviceId, taskId, ex));
            throw;
        }
    }

    private async Task<bool> CheckInboundCompletionAsync(
        IPlcConnector connector,
        SignalMap signals,
        string deviceId,
        string taskId) => await CheckCompletionAsync(connector, signals, deviceId, taskId, signals.InboundComplete);

    private static Task WriteTriggerFlagsAsync(IPlcConnector connector, SignalMap signals) =>
        Task.WhenAll(
            connector.WriteAsync(signals.InboundCommand, true),
            connector.WriteAsync(signals.StartProcessCommand, true));

    private static Task WriteTargetLocationAsync(IPlcConnector connector, SignalMap signals, TransportTask task) =>
        Task.WhenAll(
            connector.WriteAsync(signals.GateNumber, task.GateNumber),
            connector.WriteAsync(signals.InDirBlock, task.InDirBlock != Direction.Bottom));

}

