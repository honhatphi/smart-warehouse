namespace TQG.Automation.SDK.Services.CommandExecution;

/// <summary>
/// Factory tạo các strategy thực thi lệnh.
/// Triển khai Factory pattern để tạo strategy phù hợp dựa trên loại lệnh.
/// </summary>
internal sealed class CommandExecutionStrategyFactory(
    DeviceMonitor deviceMonitor,
    IBarcodeHandler barcodeHandler,
    TaskDispatcher taskDispatcher,
    ILogger? logger = null)
{
    private readonly DeviceMonitor _deviceMonitor = deviceMonitor ?? throw new ArgumentNullException(nameof(deviceMonitor));
    private readonly IBarcodeHandler _barcodeHandler = barcodeHandler ?? throw new ArgumentNullException(nameof(barcodeHandler));
    private readonly TaskDispatcher _taskDispatcher = taskDispatcher ?? throw new ArgumentNullException(nameof(taskDispatcher));
    private readonly ILogger _logger = logger ?? NullLogger.Instance;

    /// <summary>
    /// Tạo strategy thực thi lệnh cho loại lệnh được chỉ định.
    /// </summary>
    /// <param name="commandType">Loại lệnh cần tạo strategy.</param>
    /// <returns>Strategy thực thi lệnh phù hợp.</returns>
    /// <exception cref="ArgumentException">Ném ra khi loại lệnh không được hỗ trợ.</exception>
    public ICommandExecutionStrategy CreateStrategy(CommandType commandType)
    {
        return commandType switch
        {
            CommandType.Inbound => new InboundCommandStrategy(_deviceMonitor, _barcodeHandler, _taskDispatcher, _logger),
            CommandType.Outbound => new OutboundCommandStrategy(_deviceMonitor, _barcodeHandler, _taskDispatcher, _logger),
            CommandType.Transfer => new TransferCommandStrategy(_deviceMonitor, _barcodeHandler, _taskDispatcher, _logger),
            _ => throw new ArgumentException($"Unsupported command type: {commandType}", nameof(commandType))
        };
    }
}
