namespace TQG.Automation.SDK.Services.TaskManagement.Queue;

/// <summary>
/// Đại diện cho hàng đợi nhiệm vụ dựa trên độ ưu tiên để quản lý các nhiệm vụ vận chuyển.
/// Cung cấp các thao tác enqueue, dequeue và xóa hiệu quả.
/// </summary>
public interface ITaskQueue
{
    /// <summary>
    /// Lấy số lượng nhiệm vụ hiện có trong hàng đợi.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Lấy giá trị cho biết hàng đợi có rỗng hay không.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Đưa một nhiệm vụ vận chuyển vào hàng đợi với độ ưu tiên được chỉ định.
    /// </summary>
    /// <param name="task">Nhiệm vụ cần đưa vào hàng đợi.</param>
    /// <param name="priority">Độ ưu tiên của nhiệm vụ (số cao hơn = ưu tiên cao hơn).</param>
    /// <exception cref="ArgumentNullException">Ném ra khi task là null.</exception>
    /// <exception cref="ArgumentException">Ném ra khi task có TaskId không hợp lệ hoặc priority không hợp lệ.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi hàng đợi đầy hoặc ở trạng thái không hợp lệ.</exception>
    void Enqueue(TransportTask task, TaskPriority priority = TaskPriority.Normal);

    /// <summary>
    /// Cố gắng xem nhiệm vụ có độ ưu tiên cao nhất mà không lấy ra khỏi hàng đợi.
    /// </summary>
    /// <param name="task">Nhiệm vụ được xem, hoặc null nếu hàng đợi rỗng.</param>
    /// <returns>True nếu có nhiệm vụ để xem; ngược lại false.</returns>
    /// <exception cref="InvalidOperationException">Ném ra khi hàng đợi ở trạng thái không hợp lệ.</exception>
    bool TryPeek(out TransportTask? task);

    /// <summary>
    /// Cố gắng lấy nhiệm vụ có độ ưu tiên cao nhất ra khỏi hàng đợi.
    /// </summary>
    /// <param name="task">Nhiệm vụ được lấy ra, hoặc null nếu hàng đợi rỗng.</param>
    /// <returns>True nếu một nhiệm vụ được lấy ra; ngược lại false.</returns>
    /// <exception cref="InvalidOperationException">Ném ra khi hàng đợi ở trạng thái không hợp lệ.</exception>
    bool TryDequeue(out TransportTask? task);

    /// <summary>
    /// Cố gắng xóa một nhiệm vụ cụ thể khỏi hàng đợi.
    /// </summary>
    /// <param name="taskId">ID của nhiệm vụ cần xóa.</param>
    /// <returns>True nếu nhiệm vụ được tìm thấy và xóa; ngược lại false.</returns>
    /// <exception cref="ArgumentException">Ném ra khi taskId là null hoặc rỗng.</exception>
    /// <exception cref="InvalidOperationException">Ném ra khi hàng đợi ở trạng thái không hợp lệ.</exception>
    bool TryRemove(string taskId);

    /// <summary>
    /// Lấy tất cả các nhiệm vụ hiện có trong hàng đợi mà không xóa chúng.
    /// </summary>
    /// <returns>Mảng tất cả các nhiệm vụ trong hàng đợi.</returns>
    /// <exception cref="InvalidOperationException">Ném ra khi hàng đợi ở trạng thái không hợp lệ.</exception>
    TransportTask[] GetAll();

    /// <summary>
    /// Xóa tất cả các nhiệm vụ khỏi hàng đợi.
    /// </summary>
    /// <exception cref="InvalidOperationException">Ném ra khi hàng đợi ở trạng thái không hợp lệ.</exception>
    void Clear();
}
