using System;
using System.Collections;
using System.Collections.Generic;

namespace JL.Splitting
{
    /// <summary>
    /// A faster variation of the standard C# list that does less error checking and allows for access to the underlying array. 
    /// Should be used with caution.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class FList<T>
    {
        private static readonly T[] EmptyArray;

        private T[] _rawArray;
        public int Count;

        public int Capacity
        {
            get
            {
                return _rawArray.Length;
            }
            set
            {
                if ((uint)value < (uint)Count)
                    throw new ArgumentOutOfRangeException();
                Array.Resize<T>(ref _rawArray, value);
            }
        }

        public T[] RawArray => _rawArray;

        static FList()
        {
            EmptyArray = new T[0];
        }

        public FList()
        {
            _rawArray = EmptyArray;
        }

        public FList(int capacity)
        {
            _rawArray = new T[capacity];
        }

        public void Add(T item)
        {
            if (Count == _rawArray.Length)
                GrowIfNeeded(1);
            _rawArray[Count++] = item;
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf<T>(_rawArray, item, 0, Count);
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index != -1)
                RemoveAt(index);
            return index != -1;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || (uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException("index");
            Shift(index, -1);
            Array.Clear(_rawArray, Count, 1);
        }

        #region private

        private void GrowIfNeeded(int newCount)
        {
            int val2 = Count + newCount;
            if (val2 <= _rawArray.Length)
                return;
            Capacity = Math.Max(Math.Max(Capacity * 2, 4), val2);
        }

        private void Shift(int start, int delta)
        {
            if (delta < 0)
                start -= delta;
            if (start < Count)
                Array.Copy(_rawArray, start, _rawArray, start + delta, Count - start);
            Count += delta;
            if (delta >= 0)
                return;
            Array.Clear(_rawArray, Count, -delta);
        }

        #endregion
    }
}