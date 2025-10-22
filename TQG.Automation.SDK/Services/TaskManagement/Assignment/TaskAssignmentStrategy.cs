namespace TQG.Automation.SDK.Services.TaskManagement.Assignment;

/// <summary>
/// Triển khai mặc định của chiến lược gán nhiệm vụ sử dụng phương pháp kết hợp.
/// Kết hợp gán thiết bị cụ thể với cân bằng tải để phân phối nhiệm vụ tối ưu.
/// </summary>
internal sealed class TaskAssignmentStrategy : ITaskAssignmentStrategy
{
    #region Fields

    private readonly HybridDeviceAssignmentService _hybridService;

    #endregion

    #region Constructor

    public TaskAssignmentStrategy()
    {
        _hybridService = new HybridDeviceAssignmentService();
    }

    #endregion

    #region Public Methods

    public async Task<DeviceAssignment?> AssignTaskAsync(
        TransportTask task,
        IEnumerable<DeviceInfo> idleDevices,
        IReadOnlyDictionary<string, DeviceProfile> deviceProfiles,
        IReadOnlyDictionary<string, string> currentlyAssigning,
        IReadOnlyDictionary<CommandType, Location> referenceLocations)
    {
        await Task.Yield(); // Make method async for future extensibility

        // First, check if task has a specific device requirement
        if (!string.IsNullOrEmpty(task.DeviceId))
        {
            var specificDevice = idleDevices.FirstOrDefault(d => d.DeviceId == task.DeviceId);
            if (specificDevice != null &&
                deviceProfiles.TryGetValue(task.DeviceId, out var profile) &&
                !currentlyAssigning.ContainsKey(task.DeviceId))
            {
                return new DeviceAssignment(task.DeviceId, profile, task);
            }
        }

        // Use hybrid service for general assignment
        var idleDevicesList = idleDevices.ToList();
        var suitableProfile = _hybridService.SelectDevice(
            idleDevicesList,
            deviceProfiles,
            currentlyAssigning,
            task,
            referenceLocations);

        if (suitableProfile != null)
        {
            var deviceId = suitableProfile.Id;
            return new DeviceAssignment(deviceId, suitableProfile, task);
        }

        return null;
    }

    #endregion
}
