namespace BaseStationReader.Entities.Tracking
{
    public class FixedSizeQueue<T>
    {
        private readonly Queue<T> _queue = new();
        private readonly int _maxSize;

        public IEnumerable<T> Items { get { return _queue; }}

        public FixedSizeQueue(int maximumQueueSize)
            => _maxSize = maximumQueueSize;

        /// <summary>
        /// Enqueue a new item
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            _queue.Enqueue(item);
            if (_queue.Count > _maxSize)
            {
                _ = _queue.Dequeue();
            }
        }
    }
}