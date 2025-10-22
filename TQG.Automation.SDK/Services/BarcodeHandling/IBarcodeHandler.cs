namespace TQG.Automation.SDK.Services.BarcodeHandling;

/// <summary>
/// Xử lý các thao tác đọc và validation barcode cho các thiết bị automation.
/// Cung cấp chức năng đọc barcode từ thiết bị PLC và quản lý workflow validation barcode.
/// </summary>
internal interface IBarcodeHandler : IDisposable
{
    /// <summary>
    /// Xảy ra khi nhận được barcode từ thiết bị.
    /// </summary>
    event EventHandler<BarcodeReceivedEventArgs>? BarcodeReceived;

    /// <summary>
    /// Xảy ra khi task thất bại trong quá trình xử lý barcode.
    /// </summary>
    event EventHandler<TaskFailedEventArgs>? TaskFailed;

    /// <summary>
    /// Đọc barcode từ PLC connector được chỉ định sử dụng signal map được cung cấp.
    /// </summary>
    /// <param name="connector">PLC connector để đọc dữ liệu.</param>
    /// <param name="signals">Signal map chứa các signal character của barcode.</param>
    /// <returns>Task đại diện cho thao tác bất đồng bộ. Kết quả task chứa chuỗi barcode đã đọc.</returns>
    /// <exception cref="ArgumentNullException">Ném ra khi connector hoặc signals là null.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi handler đã bị dispose.</exception>
    /// <exception cref="TimeoutException">Ném ra khi thao tác đọc timeout.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi connector chưa được kết nối.</exception>
    /// <exception cref="Exception">Ném ra khi thao tác đọc thất bại sau tất cả lần thử lại.</exception>
    Task<string> ReadBarcodeAsync(IPlcConnector connector, SignalMap signals);

    /// <summary>
    /// Gửi barcode để validation tới thiết bị và task được chỉ định.
    /// </summary>
    /// <param name="deviceId">Mã định danh duy nhất của thiết bị đích.</param>
    /// <param name="taskId">Mã định danh duy nhất của task.</param>
    /// <param name="barcode">Chuỗi barcode cần validation.</param>
    /// <returns>Task đại diện cho thao tác bất đồng bộ.</returns>
    /// <exception cref="ArgumentException">Ném ra khi deviceId, taskId, hoặc barcode là null hoặc rỗng.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi handler đã bị dispose.</exception>
    /// <exception cref="TimeoutException">Ném ra khi validation timeout.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi thiết bị không khả dụng cho validation.</exception>
    /// <exception cref="Exception">Ném ra khi validation thất bại.</exception>
    Task SendBarcodeAsync(string deviceId, string taskId, string barcode);

    /// <summary>
    /// Thử hoàn thành task validation cho thiết bị được chỉ định.
    /// </summary>
    /// <param name="taskId">Mã định danh duy nhất của task cần hoàn thành.</param>
    /// <param name="deviceId">Mã định danh duy nhất của thiết bị.</param>
    /// <returns>True nếu task được hoàn thành thành công; ngược lại là false.</returns>
    /// <exception cref="ArgumentException">Ném ra khi taskId hoặc deviceId là null hoặc rỗng.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi handler đã bị dispose.</exception>
    bool TryCompleteValidationTask(string taskId, string deviceId);
}
