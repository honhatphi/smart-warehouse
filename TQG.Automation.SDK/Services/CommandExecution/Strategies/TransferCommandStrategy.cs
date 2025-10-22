namespace TQG.Automation.SDK.Services.CommandExecution.Strategies;

/// <summary>
/// Strategy xử lý thực thi lệnh transfer (chuyển kho).
/// </summary>
internal sealed class TransferCommandStrategy(
    DeviceMonitor deviceMonitor,
    IBarcodeHandler barcodeHandler,
    TaskDispatcher taskDispatcher,
    ILogger logger) : BaseCommandExecutionStrategy(deviceMonitor, barcodeHandler, taskDispatcher, logger)
{
    public override CommandType CommandType => CommandType.Transfer;

    public override async Task TriggerCommandAsync(IPlcConnector connector, SignalMap signals, TransportTask task)
    {
        Logger.LogInformation($"[{CommandType}] Triggering transfer command for task {task.TaskId} from {task.SourceLocation} to {task.TargetLocation}");

        await WriteTriggerFlagsAsync(connector, signals);

        await Task.WhenAll(
            WriteSourceLocationAsync(connector, signals, task),
            WriteTargetLocationAsync(connector, signals, task),
            connector.WriteAsync(signals.GateNumber, task.GateNumber),
            connector.WriteAsync(signals.InDirBlock, task.InDirBlock != Direction.Bottom),
            connector.WriteAsync(signals.OutDirBlock, task.OutDirBlock != Direction.Bottom)
        );

        Logger.LogInformation($"[{CommandType}] Transfer command triggered successfully for task {task.TaskId}. " +
            $"Source: Floor={task.SourceLocation!.Floor}, Rail={task.SourceLocation!.Rail}, Block={task.SourceLocation!.Block}. " +
            $"Target: Floor={task.TargetLocation!.Floor}, Rail={task.TargetLocation!.Rail}, Block={task.TargetLocation!.Block}. " +
            $"Gate={task.GateNumber}, InDir={task.InDirBlock}, OutDir={task.OutDirBlock}");
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
            () => CheckTransferCompletionAsync(connector, signals, deviceId, taskId),
            cancellationToken);

    private async Task<bool> CheckTransferCompletionAsync(
        IPlcConnector connector,
        SignalMap signals,
        string deviceId,
        string taskId) => await CheckCompletionAsync(
            connector,
            signals,
            deviceId,
            taskId,
            signals.TransferComplete);

    private static Task WriteTriggerFlagsAsync(IPlcConnector connector, SignalMap signals) =>
        Task.WhenAll(
            connector.WriteAsync(signals.TransferCommand, true),
            connector.WriteAsync(signals.StartProcessCommand, true));

    private static Task WriteSourceLocationAsync(IPlcConnector connector, SignalMap signals, TransportTask task) =>
        Task.WhenAll(
            connector.WriteAsync(signals.SourceFloor, task.SourceLocation!.Floor),
            connector.WriteAsync(signals.SourceRail, task.SourceLocation!.Rail),
            connector.WriteAsync(signals.SourceBlock, task.SourceLocation!.Block));

    private static Task WriteTargetLocationAsync(IPlcConnector connector, SignalMap signals, TransportTask task) =>
        Task.WhenAll(
            connector.WriteAsync(signals.TargetFloor, task.TargetLocation!.Floor),
            connector.WriteAsync(signals.TargetRail, task.TargetLocation!.Rail),
            connector.WriteAsync(signals.TargetBlock, task.TargetLocation!.Block));

}
