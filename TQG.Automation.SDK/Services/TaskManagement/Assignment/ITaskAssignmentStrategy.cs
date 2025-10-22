namespace TQG.Automation.SDK.Services.TaskManagement.Assignment;

/// <summary>
/// Interface chiến lược gán nhiệm vụ vận chuyển cho các thiết bị có sẵn.
/// Cho phép các thuật toán gán nhiệm vụ có thể thay thế được.
/// </summary>
public interface ITaskAssignmentStrategy
{
    /// <summary>
    /// Cố gắng gán một nhiệm vụ vận chuyển cho một thiết bị có sẵn.
    /// </summary>
    /// <param name="task">Nhiệm vụ cần gán.</param>
    /// <param name="idleDevices">Danh sách các thiết bị hiện đang rảnh rỗi.</param>
    /// <param name="deviceProfiles">Hồ sơ thiết bị cho tất cả thiết bị đã đăng ký.</param>
    /// <param name="currentlyAssigning">Từ điển các thiết bị hiện đang được gán nhiệm vụ.</param>
    /// <param name="referenceLocations">Vị trí tham chiếu để tính toán khoảng cách.</param>
    /// <returns>Phân công thiết bị nếu thành công, hoặc null nếu không có thiết bị phù hợp.</returns>
    /// <exception cref="ArgumentNullException">Ném ra khi bất kỳ tham số bắt buộc nào là null.</exception>
    /// <exception cref="ArgumentException">Ném ra khi task có thuộc tính không hợp lệ hoặc idleDevices rỗng.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi chiến lược gán nhiệm vụ ở trạng thái không hợp lệ.</exception>
    /// <exception cref="Exception">Ném ra khi tính toán gán nhiệm vụ thất bại.</exception>
    Task<DeviceAssignment?> AssignTaskAsync(
        TransportTask task,
        IEnumerable<DeviceInfo> idleDevices,
        IReadOnlyDictionary<string, DeviceProfile> deviceProfiles,
        IReadOnlyDictionary<string, string> currentlyAssigning,
        IReadOnlyDictionary<CommandType, Location> referenceLocations);
}
