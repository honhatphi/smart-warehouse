namespace TQG.Automation.SDK.Services.DeviceManagement.Monitoring;

/// <summary>
/// Theo dõi và quản lý trạng thái của các thiết bị tự động hóa.
/// Cung cấp chức năng theo dõi kết nối thiết bị, thay đổi trạng thái, và thông tin vị trí.
/// </summary>
internal interface IDeviceMonitor : IDisposable
{
    /// <summary>
    /// Xảy ra khi trạng thái của thiết bị thay đổi.
    /// </summary>
    event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;

    /// <summary>
    /// Bắt đầu theo dõi thiết bị được chỉ định.
    /// </summary>
    /// <param name="deviceId">Mã định danh duy nhất của thiết bị cần theo dõi.</param>
    /// <returns>Task đại diện cho thao tác bất đồng bộ.</returns>
    /// <exception cref="ArgumentException">Ném ra khi deviceId là null hoặc rỗng.</exception>
    /// <exception cref="DeviceNotRegisteredException">Ném ra khi thiết bị chưa được đăng ký.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi monitor đã bị dispose.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi thiết bị đã được theo dõi.</exception>
    /// <exception cref="Exception">Ném ra khi khởi tạo monitoring thất bại.</exception>
    Task StartMonitoring(string deviceId);

    /// <summary>
    /// Dừng theo dõi thiết bị được chỉ định.
    /// </summary>
    /// <param name="deviceId">Mã định danh duy nhất của thiết bị cần dừng theo dõi.</param>
    /// <exception cref="ArgumentException">Ném ra khi deviceId là null hoặc rỗng.</exception>
    /// <exception cref="DeviceNotRegisteredException">Ném ra khi thiết bị chưa được đăng ký.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi monitor đã bị dispose.</exception>
    void StopMonitoring(string deviceId);

    /// <summary>
    /// Kiểm tra xem thiết bị được chỉ định hiện có kết nối hay không.
    /// </summary>
    /// <param name="deviceId">Mã định danh duy nhất của thiết bị cần kiểm tra.</param>
    /// <returns>True nếu thiết bị đã kết nối; ngược lại là false.</returns>
    /// <exception cref="ArgumentException">Ném ra khi deviceId là null hoặc rỗng.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi monitor đã bị dispose.</exception>
    bool IsConnected(string deviceId);

    /// <summary>
    /// Lấy trạng thái hiện tại của thiết bị được chỉ định.
    /// </summary>
    /// <param name="deviceId">Mã định danh duy nhất của thiết bị.</param>
    /// <returns>Trạng thái thiết bị hiện tại, hoặc DeviceStatus.Offline nếu không tìm thấy thiết bị.</returns>
    /// <exception cref="ArgumentException">Ném ra khi deviceId là null hoặc rỗng.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi monitor đã bị dispose.</exception>
    DeviceStatus GetDeviceStatus(string deviceId);

    /// <summary>
    /// Reset trạng thái của thiết bị được chỉ định sau khi xác minh trạng thái PLC.
    /// </summary>
    /// <param name="deviceId">Mã định danh duy nhất của thiết bị cần reset.</param>
    /// <returns>True nếu trạng thái được reset thành công; ngược lại là false.</returns>
    /// <exception cref="ArgumentException">Ném ra khi deviceId là null hoặc rỗng.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi monitor đã bị dispose.</exception>
    /// <exception cref="TimeoutException">Ném ra khi thao tác đọc PLC timeout.</exception>
    /// <exception cref="PlcConnectionFailedException">Ném ra khi kết nối PLC thất bại.</exception>
    Task<bool> ResetDeviceStatusAsync(string deviceId);

    /// <summary>
    /// Lấy vị trí hiện tại của thiết bị được chỉ định.
    /// </summary>
    /// <param name="deviceId">Mã định danh duy nhất của thiết bị.</param>
    /// <returns>Vị trí hiện tại của thiết bị, hoặc null nếu thiết bị không hoạt động.</returns>
    /// <exception cref="ArgumentException">Ném ra khi deviceId là null hoặc rỗng.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi monitor đã bị dispose.</exception>
    /// <exception cref="TimeoutException">Ném ra khi lấy vị trí timeout.</exception>
    /// <exception cref="Exception">Ném ra khi lấy vị trí thất bại.</exception>
    Task<Location?> GetCurrentLocationAsync(string deviceId);

    /// <summary>
    /// Lấy danh sách tất cả thiết bị idle với vị trí hiện tại của chúng.
    /// </summary>
    /// <returns>Danh sách thiết bị idle với vị trí hiện tại.</returns>
    /// <exception cref="ObjectDisposedException">Ném ra khi monitor đã bị dispose.</exception>
    /// <exception cref="Exception">Ném ra khi liệt kê thiết bị thất bại.</exception>
    Task<List<DeviceInfo>> GetIdleDevices();

    /// <summary>
    /// Lấy PLC connector cho thiết bị được chỉ định.
    /// </summary>
    /// <param name="deviceId">Mã định danh duy nhất của thiết bị.</param>
    /// <returns>PLC connector cho thiết bị.</returns>
    /// <exception cref="ArgumentException">Ném ra khi deviceId là null hoặc rỗng.</exception>
    /// <exception cref="DeviceNotRegisteredException">Ném ra khi thiết bị chưa được đăng ký.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi monitor đã bị dispose.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi tạo connector thất bại.</exception>
    IPlcConnector GetConnector(string deviceId);
}

