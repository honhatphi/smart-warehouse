namespace TQG.Automation.SDK.Events;

/// <summary>
/// Tham số sự kiện khi trạng thái dispatcher thay đổi.
/// Chứa thông tin về trạng thái cũ, mới và thời điểm thay đổi.
/// </summary>
public class DispatcherStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Trạng thái trước đó của dispatcher.
    /// </summary>
    public DispatcherState PreviousState { get; }
    
    /// <summary>
    /// Trạng thái mới của dispatcher.
    /// </summary>
    public DispatcherState NewState { get; }
    
    /// <summary>
    /// Thời điểm thay đổi trạng thái (UTC).
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Khởi tạo tham số sự kiện thay đổi trạng thái dispatcher.
    /// </summary>
    /// <param name="previousState">Trạng thái trước đó của dispatcher.</param>
    /// <param name="newState">Trạng thái mới của dispatcher.</param>
    public DispatcherStateChangedEventArgs(DispatcherState previousState, DispatcherState newState)
    {
        PreviousState = previousState;
        NewState = newState;
        Timestamp = DateTime.UtcNow;
    }
}

