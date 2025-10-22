using System.ComponentModel;
using System.Text.Json.Serialization;

namespace TQG.Automation.SDK.Shared;

/// <summary>
/// Các loại lệnh vận chuyển có thể được thực thi bởi thiết bị.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommandType
{
    /// <summary>
    /// Lệnh nhập kho - di chuyển pallet vào hệ thống lưu trữ.
    /// </summary>
    [Description("Inbound")]
    Inbound,

    /// <summary>
    /// Lệnh xuất kho - di chuyển pallet ra khỏi hệ thống lưu trữ.
    /// </summary>
    [Description("Outbound")]
    Outbound,

    /// <summary>
    /// Lệnh chuyển kho - di chuyển pallet giữa các vị trí trong hệ thống lưu trữ.
    /// </summary>
    [Description("Transfer")]
    Transfer
}

/// <summary>
/// Hướng di chuyển cho các thao tác shuttle.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Direction
{
    /// <summary>
    /// Di chuyển từ dưới lên trên.
    /// </summary>
    [Description("Bottom to Top")]
    Bottom = 0,

    /// <summary>
    /// Di chuyển từ trên xuống dưới.
    /// </summary>
    [Description("Top to Bottom")]
    Top = 1
}

/// <summary>
/// Trạng thái hoạt động hiện tại của thiết bị.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeviceStatus
{
    /// <summary>
    /// Thiết bị offline và không khả dụng cho các thao tác.
    /// </summary>
    [Description("Offline")]
    Offline,

    /// <summary>
    /// Thiết bị rảnh rỗi và sẵn sàng nhận nhiệm vụ mới.
    /// </summary>
    [Description("Idle")]
    Idle,

    /// <summary>
    /// Thiết bị đang thực thi một nhiệm vụ.
    /// </summary>
    [Description("Busy")]
    Busy,

    /// <summary>
    /// Thiết bị ở trạng thái lỗi và cần được xử lý.
    /// </summary>
    [Description("Error")]
    Error,

    /// <summary>
    /// Thiết bị đang sạc pin.
    /// </summary>
    [Description("Charging")]
    Charging
}

/// <summary>
/// Đại diện cho các mức độ ưu tiên cho nhiệm vụ vận chuyển.
/// Giá trị cao hơn cho biết ưu tiên cao hơn.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskPriority
{
    /// <summary>
    /// Nhiệm vụ ưu tiên thấp.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Nhiệm vụ ưu tiên bình thường.
    /// </summary>
    Normal = 5,

    /// <summary>
    /// Nhiệm vụ ưu tiên cao.
    /// </summary>
    High = 8,

    /// <summary>
    /// Nhiệm vụ ưu tiên quan trọng (cao nhất).
    /// </summary>
    Critical = 10
}

/// <summary>
/// Đại diện cho các trạng thái có thể có của task dispatcher.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DispatcherState
{
    /// <summary>
    /// Dispatcher đã dừng và không xử lý nhiệm vụ.
    /// </summary>
    Stopped,

    /// <summary>
    /// Dispatcher đang chạy và xử lý nhiệm vụ tích cực.
    /// </summary>
    Running,

    /// <summary>
    /// Dispatcher đã tạm dừng nhưng có thể tiếp tục.
    /// </summary>
    Paused,

    /// <summary>
    /// Dispatcher đã bị dispose và không thể sử dụng.
    /// </summary>
    Disposed
}

/// <summary>
/// Các mức độ log cho automation SDK.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LogLevel
{
    /// <summary>
    /// Mức Debug - thông tin chi tiết để gỡ lỗi.
    /// </summary>
    Debug = 0,

    /// <summary>
    /// Mức Information - thông tin chung về luồng ứng dụng.
    /// </summary>
    Information = 1,

    /// <summary>
    /// Mức Warning - các tình huống có khả năng gây hại.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Mức Error - các sự kiện lỗi có thể vẫn cho phép ứng dụng tiếp tục.
    /// </summary>
    Error = 3,

    /// <summary>
    /// Mức Critical - các sự kiện lỗi rất nghiêm trọng có thể khiến ứng dụng phải dừng.
    /// </summary>
    Critical = 4
}

/// <summary>
/// Đại diện cho các trạng thái có thể có của AutomationGateway singleton.
/// </summary>
public enum GatewayState
{
    /// <summary>
    /// Gateway chưa được khởi tạo.
    /// </summary>
    Uninitialized = 0,

    /// <summary>
    /// Gateway đã được khởi tạo và sẵn sàng sử dụng.
    /// </summary>
    Initialized = 1,

    /// <summary>
    /// Gateway đã bị dispose và không thể sử dụng lại.
    /// </summary>
    Disposed = 2
}