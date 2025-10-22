namespace TQG.Automation.SDK.Services.DeviceManagement.Assignment;

/// <summary>
/// Dịch vụ lựa chọn thiết bị phù hợp nhất cho việc gán nhiệm vụ sử dụng thuật toán kết hợp.
/// Kết hợp nhiều yếu tố như khoảng cách, khả năng thiết bị và khối lượng công việc hiện tại để đưa ra lựa chọn tối ưu.
/// </summary>
internal interface IHybridDeviceAssignmentService
{
    /// <summary>
    /// Lựa chọn thiết bị phù hợp nhất cho tác vụ vận chuyển sử dụng thuật toán gán nhiệm vụ kết hợp.
    /// </summary>
    /// <param name="idleDevices">Danh sách các thiết bị đang rảnh rỗi kèm vị trí của chúng.</param>
    /// <param name="deviceProfiles">Từ điển hồ sơ thiết bị với khóa là ID thiết bị.</param>
    /// <param name="assigningDevices">Từ điển các thiết bị đang được gán nhiệm vụ (deviceId -> taskId).</param>
    /// <param name="task">Tác vụ vận chuyển cần được gán.</param>
    /// <param name="referenceLocations">Vị trí tham chiếu cho các loại lệnh khác nhau dùng trong tính toán khoảng cách.</param>
    /// <returns>Hồ sơ thiết bị được chọn nếu tìm thấy thiết bị phù hợp; ngược lại trả về null.</returns>
    /// <exception cref="ArgumentNullException">Ném ra khi bất kỳ tham số bắt buộc nào là null.</exception>
    /// <exception cref="ArgumentException">Ném ra khi idleDevices rỗng hoặc task có thuộc tính không hợp lệ.</exception>
    DeviceProfile? SelectDevice(
        List<DeviceInfo> idleDevices,
        IReadOnlyDictionary<string, DeviceProfile> deviceProfiles,
        IReadOnlyDictionary<string, string> assigningDevices,
        TransportTask task,
        IReadOnlyDictionary<CommandType, Location> referenceLocations);
}
