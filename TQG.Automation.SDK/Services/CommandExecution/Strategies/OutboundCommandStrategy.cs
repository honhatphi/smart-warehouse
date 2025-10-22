namespace TQG.Automation.SDK.Services.CommandExecution.Strategies;

/// <summary>
/// Strategy xử lý thực thi lệnh outbound (xuất kho).
/// </summary>
internal sealed class OutboundCommandStrategy(
    DeviceMonitor deviceMonitor,
    IBarcodeHandler barcodeHandler,
    TaskDispatcher taskDispatcher,
    ILogger logger) : BaseCommandExecutionStrategy(deviceMonitor, barcodeHandler, taskDispatcher, logger)
{
    public override CommandType CommandType => CommandType.Outbound;

    public override async Task TriggerCommandAsync(IPlcConnector connector, SignalMap signals, TransportTask task)
    {
        Logger.LogInformation($"[{CommandType}] Triggering outbound command for task {task.TaskId} from location {task.SourceLocation}");

        await WriteTriggerFlagsAsync(connector, signals);

        await WriteSourceLocationAsync(connector, signals, task);

        Logger.LogInformation(
            $"[{CommandType}] Outbound command triggered successfully for task {task.TaskId}. " +
            $"Source: Floor={task.SourceLocation!.Floor}, Rail={task.SourceLocation!.Rail}, " +
            $"Block={task.SourceLocation!.Block}, Gate={task.GateNumber}, OutDir={task.OutDirBlock}");
    }

    public override async Task StartPollingAsync(
        string deviceId,
        string taskId,
        IPlcConnector connector,
        SignalMap signals,
        int timeoutMinutes,
        CancellationToken cancellationToken) => await ExecutePollingLoopAsync(
            deviceId,
            taskId,
            timeoutMinutes,
            () => CheckOutboundCompletionAsync(connector, signals, deviceId, taskId),
            cancellationToken);

    private async Task<bool> CheckOutboundCompletionAsync(
        IPlcConnector connector,
        SignalMap signals,
        string deviceId,
        string taskId) => await CheckCompletionAsync(connector, signals, deviceId, taskId, signals.OutboundComplete);

    // Local helpers to reduce repetition and parallelize PLC writes
    private static Task WriteTriggerFlagsAsync(IPlcConnector connector, SignalMap signals) =>
        Task.WhenAll(
            connector.WriteAsync(signals.OutboundCommand, true),
            connector.WriteAsync(signals.StartProcessCommand, true));

    private static Task WriteSourceLocationAsync(IPlcConnector connector, SignalMap signals, TransportTask task) =>
        Task.WhenAll(
            connector.WriteAsync(signals.SourceFloor, task.SourceLocation!.Floor),
            connector.WriteAsync(signals.SourceRail, task.SourceLocation!.Rail),
            connector.WriteAsync(signals.SourceBlock, task.SourceLocation!.Block),
            connector.WriteAsync(signals.GateNumber, task.GateNumber),
            connector.WriteAsync(signals.OutDirBlock, task.OutDirBlock != Direction.Bottom));

}
