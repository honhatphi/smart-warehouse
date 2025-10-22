namespace TQG.Automation.SDK.Services.CommandExecution.Core;

/// <summary>
/// Giao diện strategy cho các pattern thực thi lệnh.
/// Triển khai Strategy pattern để xử lý các loại lệnh khác nhau.
/// </summary>
internal interface ICommandExecutionStrategy
{
    /// <summary>
    /// Kích hoạt thực thi lệnh trên thiết bị.
    /// </summary>
    /// <param name="connector">PLC connector để kết nối thiết bị.</param>
    /// <param name="signals">Signal map cho thiết bị.</param>
    /// <param name="task">Transport task cần thực thi.</param>
    /// <returns>Task đại diện cho thao tác bất đồng bộ.</returns>
    /// <exception cref="ArgumentNullException">Ném ra khi connector, signals, hoặc task là null.</exception>
    /// <exception cref="ArgumentException">Ném ra khi task có thuộc tính không hợp lệ.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi connector chưa được kết nối.</exception>
    /// <exception cref="TimeoutException">Ném ra khi thực thi lệnh timeout.</exception>
    /// <exception cref="Exception">Ném ra khi thực thi lệnh thất bại.</exception>
    Task TriggerCommandAsync(IPlcConnector connector, SignalMap signals, TransportTask task);

    /// <summary>
    /// Bắt đầu polling task để theo dõi hoàn thành lệnh.
    /// </summary>
    /// <param name="deviceId">Mã định danh thiết bị.</param>
    /// <param name="taskId">Mã định danh task.</param>
    /// <param name="connector">PLC connector để kết nối thiết bị.</param>
    /// <param name="signals">Signal map cho thiết bị.</param>
    /// <param name="timeoutMinutes">Timeout tính bằng phút.</param>
    /// <param name="cancellationToken">Token để hủy thao tác.</param>
    /// <returns>Task đại diện cho thao tác polling.</returns>
    /// <exception cref="ArgumentException">Ném ra khi deviceId, taskId là null hoặc rỗng, hoặc timeoutMinutes không hợp lệ.</exception>
    /// <exception cref="ArgumentNullException">Ném ra khi connector hoặc signals là null.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi connector chưa được kết nối.</exception>
    /// <exception cref="OperationCanceledException">Ném ra khi thao tác bị hủy.</exception>
    /// <exception cref="TimeoutException">Ném ra khi polling timeout.</exception>
    /// <exception cref="Exception">Ném ra khi polling thất bại.</exception>
    Task StartPollingAsync(
        string deviceId,
        string taskId,
        IPlcConnector connector,
        SignalMap signals,
        int timeoutMinutes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lấy loại lệnh mà strategy này xử lý.
    /// </summary>
    CommandType CommandType { get; }

    /// <summary>
    /// Occurs when a task succeeds.
    /// </summary>
    event EventHandler<TaskSucceededEventArgs>? TaskSucceeded;

    /// <summary>
    /// Occurs when a task fails.
    /// </summary>
    event EventHandler<TaskFailedEventArgs>? TaskFailed;
}
