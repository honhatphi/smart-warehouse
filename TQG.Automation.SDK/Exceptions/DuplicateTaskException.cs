namespace TQG.Automation.SDK.Exceptions;

/// <summary>
/// Exception được ném ra khi cố gắng thêm task trùng lặp vào queue.
/// Xảy ra khi task có cùng ID đã tồn tại trong hệ thống và chưa được xử lý hoàn tất.
/// </summary>
/// <param name="taskId">Mã định danh task bị trùng lặp trong queue.</param>
public sealed class DuplicateTaskException(string taskId) : Exception($"Task {taskId} already exists in queue.")
{
    /// <summary>
    /// Mã định danh task bị trùng lặp trong queue.
    /// </summary>
    public string TaskId { get; } = taskId;
}
