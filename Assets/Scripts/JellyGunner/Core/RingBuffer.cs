using System;

namespace JellyGunner
{
    public class RingBuffer<T>
    {
        private readonly T[] _buffer;
        private int _head;
        private int _tail;
        private int _count;

        public int Count => _count;
        public int Capacity => _buffer.Length;
        public bool IsFull => _count == _buffer.Length;
        public bool IsEmpty => _count == 0;

        public RingBuffer(int capacity)
        {
            _buffer = new T[capacity];
        }

        public bool TryEnqueue(T item)
        {
            if (IsFull) return false;
            _buffer[_tail] = item;
            _tail = (_tail + 1) % _buffer.Length;
            _count++;
            return true;
        }

        public bool TryDequeue(out T item)
        {
            if (IsEmpty)
            {
                item = default;
                return false;
            }
            item = _buffer[_head];
            _buffer[_head] = default;
            _head = (_head + 1) % _buffer.Length;
            _count--;
            return true;
        }

        public T PeekAt(int index)
        {
            if (index < 0 || index >= _count)
                throw new IndexOutOfRangeException();
            return _buffer[(_head + index) % _buffer.Length];
        }

        public void Clear()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _head = 0;
            _tail = 0;
            _count = 0;
        }
    }
}
