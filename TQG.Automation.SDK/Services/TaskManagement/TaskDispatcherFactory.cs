namespace TQG.Automation.SDK.Services.TaskManagement;

internal static class TaskDispatcherFactory
{
    public static TaskDispatcher Create(
        DeviceMonitor deviceMonitor,
        IReadOnlyDictionary<string, DeviceProfile> deviceProfiles,
        TaskDispatcherConfiguration? configuration = null,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(deviceMonitor);
        ArgumentNullException.ThrowIfNull(deviceProfiles);

        var cfg = configuration ?? new TaskDispatcherConfiguration();
        cfg.Validate();

        var taskQueue = new PriorityTaskQueue();
        var assignmentStrategy = new TaskAssignmentStrategy();

        return new TaskDispatcher(
            taskQueue,
            assignmentStrategy,
            deviceMonitor,
            cfg,
            deviceProfiles,
            logger);
    }
}
