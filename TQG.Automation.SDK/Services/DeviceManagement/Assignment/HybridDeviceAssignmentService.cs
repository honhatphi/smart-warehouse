namespace TQG.Automation.SDK.Services.DeviceManagement.Assignment;

/// <summary>
/// Chiến lược gán thiết bị kết hợp sắp xếp theo khoảng cách và lựa chọn round-robin.
/// Sắp xếp thiết bị theo khoảng cách đến vị trí tham chiếu nhiệm vụ, sau đó sử dụng round-robin để lựa chọn công bằng.
/// </summary>
internal sealed class HybridDeviceAssignmentService : IHybridDeviceAssignmentService
{
    #region Fields
    private volatile int _roundRobinIndex = 0;
    #endregion

    #region Public Methods
    public DeviceProfile? SelectDevice(
        List<DeviceInfo> idleDevices,
        IReadOnlyDictionary<string, DeviceProfile> deviceProfiles,
        IReadOnlyDictionary<string, string> assigningDevices,
        TransportTask task,
        IReadOnlyDictionary<CommandType, Location> referenceLocations)
    {
        var referenceLocation = GetReferenceLocation(task, referenceLocations);
        if (referenceLocation == null)
            return null;

        var eligibleDevices = idleDevices
            .Where(device => deviceProfiles.TryGetValue(device.DeviceId, out var _) &&
                           !assigningDevices.ContainsKey(device.DeviceId))
            .OrderBy(device => CalculateDistance(device.Location, referenceLocation))
            .ToList();

        if (eligibleDevices.Count == 0)
            return null;

        var selectedDevice = SelectDeviceUsingRoundRobin(eligibleDevices);
        return deviceProfiles[selectedDevice.DeviceId];
    }

    /// <summary>
    /// Đặt lại chỉ số round-robin (hữu ích cho các kịch bản kiểm thử hoặc khởi động lại).
    /// </summary>
    public void ResetRoundRobinIndex()
    {
        Interlocked.Exchange(ref _roundRobinIndex, 0);
    }
    #endregion

    #region Private Helper Methods
    /// <summary>
    /// Lấy vị trí tham chiếu cho một nhiệm vụ dựa trên loại lệnh của nó.
    /// </summary>
    private static Location? GetReferenceLocation(
        TransportTask task,
        IReadOnlyDictionary<CommandType, Location> referenceLocations)
    {
        return task.CommandType switch
        {
            CommandType.Inbound when referenceLocations.TryGetValue(CommandType.Inbound, out var inboundLoc) => inboundLoc,
            CommandType.Outbound => task.SourceLocation,
            _ => task.SourceLocation
        };
    }

    /// <summary>
    /// Tính toán khoảng cách Manhattan giữa hai vị trí.
    /// </summary>
    private static int CalculateDistance(Location locationA, Location locationB) =>
        Math.Abs(locationA.Floor - locationB.Floor) +
        Math.Abs(locationA.Rail - locationB.Rail) +
        Math.Abs(locationA.Block - locationB.Block);

    /// <summary>
    /// Lựa chọn một thiết bị từ danh sách đã sắp xếp sử dụng thuật toán round-robin.
    /// </summary>
    private DeviceInfo SelectDeviceUsingRoundRobin(List<DeviceInfo> sortedDevices)
    {
        if (sortedDevices.Count == 1)
            return sortedDevices[0];

        // Thread-safe round-robin implementation
        int currentIndex, nextIndex;
        do
        {
            currentIndex = _roundRobinIndex;
            nextIndex = (currentIndex + 1) % sortedDevices.Count;
            
            // Handle potential overflow by resetting to 0
            if (currentIndex >= 1_000_000)
            {
                nextIndex = 0;
            }
        }
        while (Interlocked.CompareExchange(ref _roundRobinIndex, nextIndex, currentIndex) != currentIndex);

        return sortedDevices[currentIndex % sortedDevices.Count];
    }
    #endregion
}