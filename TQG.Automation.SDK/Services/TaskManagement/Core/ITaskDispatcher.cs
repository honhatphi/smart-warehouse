namespace TQG.Automation.SDK.Services.TaskManagement.Core;

/// <summary>
/// Điều phối và quản lý các nhiệm vụ vận chuyển cho thiết bị tự động hóa.
/// Cung cấp chức năng xếp hàng, gán và theo dõi việc thực thi nhiệm vụ.
/// </summary>
internal interface ITaskDispatcher : IDisposable
{
    /// <summary>
    /// Xảy ra khi một nhiệm vụ được gán cho thiết bị.
    /// </summary>
    event EventHandler<TaskAssignedEventArgs>? TaskAssigned;

    /// <summary>
    /// Lấy giá trị cho biết hàng đợi nhiệm vụ hiện có đang tạm dừng hay không.
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// Đưa danh sách các nhiệm vụ vận chuyển vào hàng đợi để xử lý.
    /// </summary>
    /// <param name="tasks">Danh sách nhiệm vụ cần đưa vào hàng đợi.</param>
    /// <exception cref="ArgumentNullException">Ném ra khi tasks là null.</exception>
    /// <exception cref="ArgumentException">Ném ra khi tasks chứa nhiệm vụ không hợp lệ hoặc trùng lặp.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi dispatcher đã bị dispose.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi dispatcher ở trạng thái không hợp lệ.</exception>
    void EnqueueTasks(IEnumerable<TransportTask> tasks);

    /// <summary>
    /// Xử lý hàng đợi nhiệm vụ nếu cần thiết.
    /// </summary>
    /// <returns>Task đại diện cho thao tác bất đồng bộ.</returns>
    /// <exception cref="ObjectDisposedException">Ném ra khi dispatcher đã bị dispose.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi dispatcher ở trạng thái không hợp lệ.</exception>
    /// <exception cref="Exception">Ném ra khi xử lý hàng đợi thất bại.</exception>
    Task ProcessQueueIfNeeded();

    /// <summary>
    /// Lấy tất cả các nhiệm vụ hiện đang trong hàng đợi.
    /// </summary>
    /// <returns>Mảng các nhiệm vụ vận chuyển trong hàng đợi.</returns>
    /// <exception cref="ObjectDisposedException">Ném ra khi dispatcher đã bị dispose.</exception>
    TransportTask[] GetQueuedTasks();

    /// <summary>
    /// Xóa các nhiệm vụ được chỉ định khỏi hàng đợi.
    /// </summary>
    /// <param name="taskIds">Danh sách ID nhiệm vụ cần xóa.</param>
    /// <returns>True nếu các nhiệm vụ được xóa thành công; ngược lại false.</returns>
    /// <exception cref="ArgumentNullException">Ném ra khi taskIds là null.</exception>
    /// <exception cref="ArgumentException">Ném ra khi taskIds chứa chuỗi null hoặc rỗng.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi dispatcher đã bị dispose.</exception>
    bool RemoveTasks(IEnumerable<string> taskIds);

    /// <summary>
    /// Xóa một nhiệm vụ đơn lẻ khỏi hàng đợi.
    /// </summary>
    /// <param name="taskId">ID của nhiệm vụ cần xóa.</param>
    /// <returns>True nếu nhiệm vụ được xóa thành công; ngược lại false.</returns>
    /// <exception cref="ArgumentException">Ném ra khi taskId là null hoặc rỗng.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi dispatcher đã bị dispose.</exception>
    bool RemoveTask(string taskId);

    /// <summary>
    /// Lấy nhiệm vụ hiện tại đang được thực thi bởi thiết bị được chỉ định.
    /// </summary>
    /// <param name="deviceId">Mã định danh duy nhất của thiết bị.</param>
    /// <returns>ID nhiệm vụ hiện tại, hoặc null nếu không có nhiệm vụ nào đang được thực thi.</returns>
    /// <exception cref="ArgumentException">Ném ra khi deviceId là null hoặc rỗng.</exception>
    /// <exception cref="ObjectDisposedException">Ném ra khi dispatcher đã bị dispose.</exception>
    string? GetCurrentTask(string deviceId);

    /// <summary>
    /// Tạm dừng xử lý hàng đợi nhiệm vụ.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Ném ra khi dispatcher đã bị dispose.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi dispatcher không ở trạng thái có thể tạm dừng.</exception>
    void Pause();

    /// <summary>
    /// Tiếp tục xử lý hàng đợi nhiệm vụ.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Ném ra khi dispatcher đã bị dispose.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi dispatcher không ở trạng thái có thể tiếp tục.</exception>
    void Resume();
}
