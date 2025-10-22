using System.Collections.Concurrent;

namespace TQG.Automation.SDK.Services.TaskManagement.Queue;

/// <summary>
/// Triển khai hàng đợi ưu tiên thread-safe cho các nhiệm vụ vận chuyển.
/// Sử dụng hàng đợi ưu tiên concurrent với các thao tác O(log n) hiệu quả.
/// </summary>
internal sealed class PriorityTaskQueue : ITaskQueue
{
    private readonly ConcurrentPriorityQueue<TransportTask> _priorityQueue;
    private readonly ConcurrentDictionary<string, TransportTask> _taskLookup;
    private readonly object _lockObject = new();

    public PriorityTaskQueue()
    {
        _priorityQueue = new ConcurrentPriorityQueue<TransportTask>();
        _taskLookup = new ConcurrentDictionary<string, TransportTask>();
    }

    public int Count => _taskLookup.Count;

    public bool IsEmpty => _taskLookup.IsEmpty;

    public void Enqueue(TransportTask task, TaskPriority priority = TaskPriority.Normal)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (string.IsNullOrWhiteSpace(task.TaskId))
            throw new ArgumentException("Task ID cannot be null or empty.", nameof(task));

        lock (_lockObject)
        {
            if (_taskLookup.ContainsKey(task.TaskId))
                throw new InvalidOperationException($"Task with ID '{task.TaskId}' already exists in the queue.");

            _priorityQueue.Enqueue(task, (int)priority);
            _taskLookup[task.TaskId] = task;
        }
    }

    public bool TryDequeue(out TransportTask? task)
    {
        lock (_lockObject)
        {
            // Keep trying to dequeue until we find a valid task or the queue is empty
            while (_priorityQueue.TryDequeue(out task))
            {
                if (task != null)
                {
                    // Check if this task is still valid (not removed)
                    if (_taskLookup.TryRemove(task.TaskId, out _))
                    {
                        return true; // Found a valid task
                    }
                    // Task was already removed, continue to next task
                }
            }

            // No valid tasks found
            task = null;
            return false;
        }
    }

    public bool TryRemove(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            return false;

        lock (_lockObject)
        {
            if (_taskLookup.TryRemove(taskId, out var task))
            {
                // Also remove from the priority queue to prevent memory leaks
                // and ensure the task won't be dequeued later
                _priorityQueue.Remove(task);
                return true;
            }

            return false;
        }
    }

    public TransportTask[] GetAll()
    {
        lock (_lockObject)
        {
            return [.. _taskLookup.Values];
        }
    }

    public void Clear()
    {
        lock (_lockObject)
        {
            _priorityQueue.Clear();
            _taskLookup.Clear();
        }
    }
}