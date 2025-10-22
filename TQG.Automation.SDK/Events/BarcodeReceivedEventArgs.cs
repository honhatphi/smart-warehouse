namespace TQG.Automation.SDK.Events;

/// <summary>
/// Tham số sự kiện khi nhận được barcode từ thiết bị.
/// Chứa thông tin về thiết bị, task và barcode được scan.
/// </summary>
/// <param name="deviceId">Mã định danh thiết bị đã scan barcode.</param>
/// <param name="taskId">Mã định danh task đang được thực hiện.</param>
/// <param name="barcode">Chuỗi barcode đã được scan từ thiết bị.</param>
public sealed class BarcodeReceivedEventArgs(string deviceId, string taskId, string barcode) : EventArgs
{
    /// <summary>
    /// Mã định danh thiết bị đã scan barcode.
    /// </summary>
    public string DeviceId { get; } = deviceId;
    
    /// <summary>
    /// Mã định danh task đang được thực hiện.
    /// </summary>
    public string TaskId { get; } = taskId;
    
    /// <summary>
    /// Chuỗi barcode đã được scan từ thiết bị.
    /// </summary>
    public string Barcode { get; } = barcode;
}