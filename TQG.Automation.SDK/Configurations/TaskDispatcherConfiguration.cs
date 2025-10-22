namespace TQG.Automation.SDK.Configurations;

/// <summary>
/// Cấu hình cho TaskDispatcher - điều phối và xử lý task.
/// Kiểm soát hiệu suất, queue management và hành vi xử lý task.
/// </summary>
public class TaskDispatcherConfiguration
{
    #region Processing Configuration

    /// <summary>
    /// Số lượng tối đa task có thể được xử lý trong một chu kỳ.
    /// Mặc định: 10. Giữ giá trị nhỏ để tránh CPU/PLC burst dài; chỉ tăng khi host và PLC có thể xử lý throughput cao hơn.
    /// Khi không được cấu hình, dispatcher sẽ sử dụng giá trị mặc định này để cung cấp hành vi có thể dự đoán.
    /// </summary>
    public int MaxTasksPerCycle { get; set; } = 10;

    #endregion

    #region Queue Management

    /// <summary>
    /// Có tự động tạm dừng khi không có task khả dụng hay không.
    /// Mặc định: true. Tạm dừng giảm CPU/PLC polling khi idle; chỉ đặt false cho các scenario polling luôn bật.
    /// </summary>
    public bool AutoPauseWhenEmpty { get; set; } = true;

    /// <summary>
    /// Kích thước queue tối đa trước khi từ chối task mới.
    /// Mặc định: 50. Là giới hạn an toàn để tránh tăng trưởng bộ nhớ không giới hạn khi producer nhanh hơn consumer.
    /// </summary>
    public int MaxQueueSize { get; set; } = 50;

    #endregion

    #region Validation

    /// <summary>
    /// Xác thực các cài đặt cấu hình để đảm bảo tính hợp lệ.
    /// </summary>
    /// <exception cref="ArgumentException">Ném ra khi cấu hình không hợp lệ.</exception>
    public void Validate()
    {
        if (MaxTasksPerCycle <= 0)
            throw new ArgumentException("MaxTasksPerCycle must be greater than zero.", nameof(MaxTasksPerCycle));

        if (MaxQueueSize <= 0)
            throw new ArgumentException("MaxQueueSize must be greater than zero.", nameof(MaxQueueSize));
    }

    #endregion
}
