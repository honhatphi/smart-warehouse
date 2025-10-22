using System.Text.Json.Serialization;

namespace TQG.Automation.SDK.Configurations;

/// <summary>
/// Cấu hình ứng dụng được tải từ file `appsettings.json` hoặc file cấu hình tương tự.
/// Chứa chế độ chạy (test/production) và danh sách cấu hình thiết bị.
/// </summary>
public class ApplicationConfiguration
{
    /// <summary>
    /// Chế độ hoạt động của ứng dụng. Giá trị mặc định là "test".
    /// - "production" => Chế độ sản xuất
    /// - "test" => Chế độ thử nghiệm
    /// </summary>
    public string Mode { get; set; } = "test";

    /// <summary>
    /// Danh sách cấu hình thiết bị (DeviceProfile) được sử dụng để khởi tạo gateway.
    /// Mặc định là danh sách rỗng; file cấu hình nên chứa phần tử này.
    /// </summary>
    public List<DeviceProfile> Devices { get; set; } = [];

    /// <summary>
    /// Cấu hình cho BarcodeHandler - xử lý barcode và validation.
    /// </summary>
    public BarcodeHandlerConfiguration BarcodeHandler { get; set; } = new BarcodeHandlerConfiguration();

    /// <summary>
    /// Cấu hình cho DeviceMonitor - giám sát trạng thái thiết bị.
    /// </summary>
    public DeviceMonitorConfiguration DeviceMonitor { get; set; } = new DeviceMonitorConfiguration();

    /// <summary>
    /// Cấu hình cho TaskDispatcher - điều phối và xử lý task.
    /// </summary>
    public TaskDispatcherConfiguration TaskDispatcher { get; set; } = new TaskDispatcherConfiguration();

    /// <summary>
    /// Cấu hình cho Logger - ghi log và chẩn đoán hệ thống.
    /// </summary>
    public LoggerConfiguration Logger { get; set; } = new LoggerConfiguration();

    /// <summary>
    /// Cấu hình timeout cho các task - kiểm soát thời gian chờ.
    /// </summary>
    public TaskTimeoutConfiguration TaskTimeout { get; set; } = new TaskTimeoutConfiguration();

    /// <summary>
    /// Cấu hình cho các thao tác PLC (timeout đọc/ghi, hành vi retry kết nối).
    /// </summary>
    public PlcConfiguration Plc { get; set; } = new PlcConfiguration();

    /// <summary>
    /// Kiểm tra xem ứng dụng có đang ở chế độ test hay không.
    /// </summary>
    [JsonIgnore]
    public bool IsTestMode => !Mode.Equals("production", StringComparison.InvariantCultureIgnoreCase);
}
