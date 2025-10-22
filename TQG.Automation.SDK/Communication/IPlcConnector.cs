namespace TQG.Automation.SDK.Communication;

/// <summary>
/// Định nghĩa giao diện thống nhất cho giao tiếp PLC qua các giao thức khác nhau (S7, TCP, v.v.).
/// Cung cấp các thao tác chuẩn để đọc, ghi và quản lý kết nối PLC.
/// </summary>
internal interface IPlcConnector : IDisposable
{
    /// <summary>
    /// Lấy mã định danh duy nhất cho thiết bị PLC mà connector này quản lý.
    /// </summary>
    string DeviceId { get; }

    /// <summary>
    /// Đọc một giá trị từ địa chỉ bộ nhớ PLC được chỉ định.
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu mong đợi của giá trị cần đọc.</typeparam>
    /// <param name="address">Địa chỉ bộ nhớ PLC để đọc (ví dụ: "DB1.DBX0.0", "M0.0").</param>
    /// <returns>Giá trị được đọc từ địa chỉ đã chỉ định, được chuyển đổi sang kiểu T.</returns>
    /// <exception cref="ArgumentException">Ném ra khi address là null hoặc rỗng.</exception>
    /// <exception cref="PlcConnectionFailedException">Ném ra khi giao tiếp PLC thất bại.</exception>
    /// <exception cref="InvalidCastException">Ném ra khi giá trị đọc được không thể chuyển đổi sang kiểu T.</exception>
    Task<T> ReadAsync<T>(string address);

    /// <summary>
    /// Ghi một giá trị vào địa chỉ bộ nhớ PLC được chỉ định.
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu của giá trị cần ghi.</typeparam>
    /// <param name="address">Địa chỉ bộ nhớ PLC để ghi (ví dụ: "DB1.DBX0.0", "M0.0").</param>
    /// <param name="value">Giá trị cần ghi vào địa chỉ đã chỉ định.</param>
    /// <exception cref="ArgumentException">Ném ra khi address là null hoặc rỗng.</exception>
    /// <exception cref="ArgumentNullException">Ném ra khi value là null.</exception>
    /// <exception cref="PlcConnectionFailedException">Ném ra khi giao tiếp PLC thất bại.</exception>
    Task WriteAsync<T>(string address, T value);

    /// <summary>
    /// Kiểm tra xem kết nối PLC hiện tại có đang hoạt động và phản hồi hay không.
    /// </summary>
    /// <returns>True nếu kết nối đang hoạt động; ngược lại là false.</returns>
    Task<bool> IsConnectedAsync();

    /// <summary>
    /// Đảm bảo kết nối PLC được thiết lập và sẵn sàng cho giao tiếp.
    /// Nếu chưa kết nối, sẽ thử thiết lập kết nối.
    /// </summary>
    /// <exception cref="PlcConnectionFailedException">Ném ra khi không thể thiết lập kết nối sau các lần thử lại.</exception>
    Task EnsureConnectedAsync();
}