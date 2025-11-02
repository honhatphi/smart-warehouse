namespace TQG.Automation.SDK.Services.TaskManagement.Queue;

/// <summary>
/// Triển khai hàng đợi ưu tiên concurrent đơn giản.
/// Sử dụng binary heap cho các thao tác dựa trên độ ưu tiên hiệu quả.
/// Đảm bảo FIFO cho các items có cùng priority bằng sequence number.
/// </summary>
/// <typeparam name="T">Loại các item trong hàng đợi.</typeparam>
internal sealed class ConcurrentPriorityQueue<T> where T : class
{
    private readonly List<(T item, int priority, long sequence)> _heap = [];
    private readonly object _lockObject = new();
    private long _sequenceCounter = 0;

    public int Count
    {
        get
        {
            lock (_lockObject)
            {
                return _heap.Count;
            }
        }
    }

    public bool IsEmpty
    {
        get
        {
            lock (_lockObject)
            {
                return _heap.Count == 0;
            }
        }
    }

    public void Enqueue(T item, int priority)
    {
        lock (_lockObject)
        {
            var sequence = _sequenceCounter++;
            _heap.Add((item, priority, sequence));
            HeapifyUp(_heap.Count - 1);
        }
    }

    public bool TryDequeue(out T? item)
    {
        lock (_lockObject)
        {
            if (_heap.Count == 0)
            {
                item = null;
                return false;
            }

            item = _heap[0].item;

            if (_heap.Count == 1)
            {
                _heap.Clear();
            }
            else
            {
                _heap[0] = _heap[^1];
                _heap.RemoveAt(_heap.Count - 1);
                HeapifyDown(0);
            }

            return true;
        }
    }

    public bool Remove(T item)
    {
        lock (_lockObject)
        {
            for (int i = 0; i < _heap.Count; i++)
            {
                if (ReferenceEquals(_heap[i].item, item))
                {
                    // Replace the item to remove with the last item
                    _heap[i] = _heap[^1];
                    _heap.RemoveAt(_heap.Count - 1);

                    // If we removed the last item, we're done
                    if (i >= _heap.Count)
                        return true;

                    // Restore heap property by trying both up and down
                    int parent = (i - 1) / 2;
                    if (i > 0 && ComparePriority(_heap[i], _heap[parent]) > 0)
                    {
                        HeapifyUp(i);
                    }
                    else
                    {
                        HeapifyDown(i);
                    }

                    return true;
                }
            }
            return false;
        }
    }

    public void Clear()
    {
        lock (_lockObject)
        {
            _heap.Clear();
        }
    }

    /// <summary>
    /// So sánh priority giữa hai items.
    /// Trả về: > 0 nếu a có priority cao hơn b
    ///         = 0 nếu bằng nhau
    ///         < 0 nếu a có priority thấp hơn b
    /// </summary>
    private static int ComparePriority((T item, int priority, long sequence) a, (T item, int priority, long sequence) b)
    {
        // So sánh priority trước
        if (a.priority != b.priority)
            return a.priority.CompareTo(b.priority);
        
        // Nếu priority bằng nhau, sequence nhỏ hơn = ưu tiên cao hơn (FIFO)
        return b.sequence.CompareTo(a.sequence);
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            
            // So sánh: priority cao hơn = ưu tiên cao hơn
            // Nếu cùng priority, sequence nhỏ hơn = vào trước = ưu tiên cao hơn (FIFO)
            if (ComparePriority(_heap[parentIndex], _heap[index]) >= 0)
                break;

            Swap(parentIndex, index);
            index = parentIndex;
        }
    }

    private void HeapifyDown(int index)
    {
        while (true)
        {
            int leftChild = 2 * index + 1;
            int rightChild = 2 * index + 2;
            int largest = index;

            if (leftChild < _heap.Count && ComparePriority(_heap[leftChild], _heap[largest]) > 0)
                largest = leftChild;

            if (rightChild < _heap.Count && ComparePriority(_heap[rightChild], _heap[largest]) > 0)
                largest = rightChild;

            if (largest == index)
                break;

            Swap(index, largest);
            index = largest;
        }
    }

    private void Swap(int i, int j)
    {
        (_heap[i], _heap[j]) = (_heap[j], _heap[i]);
    }
}
