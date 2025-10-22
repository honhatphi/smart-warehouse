namespace TQG.Automation.SDK.Services.CommandExecution.Communication;

/// <summary>
/// Gửi lệnh tới thiết bị tự động hóa và quản lý việc thực thi lệnh.
/// Cung cấp chức năng gửi transport tasks và kết quả validation tới thiết bị.
/// </summary>
internal interface ICommandSender : IDisposable
{
    /// <summary>
    /// Xảy ra khi task được hoàn thành thành công.
    /// </summary>
    event EventHandler<TaskSucceededEventArgs>? TaskSucceeded;

    /// <summary>
    /// Xảy ra khi task thất bại trong quá trình thực thi.
    /// </summary>
    event EventHandler<TaskFailedEventArgs>? TaskFailed;

    /// <summary>
    /// Gửi một lệnh transport đơn lẻ tới hệ thống.
    /// </summary>
    /// <param name="task">Transport task cần thực thi.</param>
    /// <returns>Task đại diện cho thao tác bất đồng bộ.</returns>
    /// <exception cref="ArgumentNullException">Ném ra khi task là null.</exception>
    /// <exception cref="ArgumentException">Ném ra khi task không hợp lệ hoặc thiếu thuộc tính bắt buộc.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi sender đã bị dispose.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi không có thiết bị phù hợp cho task.</exception>
    /// <exception cref="TimeoutException">Ném ra khi gửi lệnh timeout.</exception>
    /// <exception cref="Exception">Ném ra khi gửi lệnh thất bại.</exception>
    Task SendCommand(TransportTask task);

    /// <summary>
    /// Gửi nhiều lệnh transport tới hệ thống.
    /// </summary>
    /// <param name="tasks">Danh sách transport tasks cần thực thi.</param>
    /// <returns>Task đại diện cho thao tác bất đồng bộ.</returns>
    /// <exception cref="ArgumentNullException">Ném ra khi tasks là null.</exception>
    /// <exception cref="ArgumentException">Ném ra khi danh sách tasks rỗng hoặc chứa task không hợp lệ.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi sender đã bị dispose.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi không có thiết bị phù hợp cho các tasks.</exception>
    /// <exception cref="TimeoutException">Ném ra khi gửi lệnh timeout.</exception>
    /// <exception cref="Exception">Ném ra khi gửi lệnh thất bại.</exception>
    Task SendMultipleCommands(List<TransportTask> tasks);

    /// <summary>
    /// Gửi kết quả validation barcode tới thiết bị được chỉ định.
    /// </summary>
    /// <param name="deviceId">Mã định danh duy nhất của thiết bị đích.</param>
    /// <param name="taskId">Mã định danh duy nhất của task.</param>
    /// <param name="isValid">True nếu barcode hợp lệ; ngược lại là false.</param>
    /// <param name="targetLocation">Vị trí đích (bắt buộc nếu barcode hợp lệ).</param>
    /// <param name="direction">Hướng để vào block.</param>
    /// <param name="gateNumber">Số gate để vào warehouse.</param>
    /// <returns>Task đại diện cho thao tác bất đồng bộ.</returns>
    /// <exception cref="ArgumentException">Ném ra khi deviceId hoặc taskId là null hoặc rỗng.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi sender đã bị dispose.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi thiết bị không khả dụng hoặc validation thất bại.</exception>
    /// <exception cref="TimeoutException">Ném ra khi gửi kết quả validation timeout.</exception>
    /// <exception cref="Exception">Ném ra khi gửi kết quả validation thất bại.</exception>
    Task SendValidationResult(
        string deviceId,
        string taskId,
        bool isValid,
        Location? targetLocation,
        Direction direction,
        short gateNumber);
}
