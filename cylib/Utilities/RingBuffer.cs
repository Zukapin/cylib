using BepuUtilities.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cylib
{
    public struct RingBuffer<T> where T : unmanaged
    {
        T[] Buffer;
        int CurReadPointer;
        int CurWritePointer;

        public int Capacity
        {
            get
            {
                return Buffer.Length;
            }
        }

        public int Count { get; private set; }

        public ref T this[int index]
        {
            get
            {
                Debug.Assert(index < Count, "Accessing an index that doesn't exist");
                Debug.Assert(index >= 0, "Index must be >= 0");
                return ref Buffer[(CurReadPointer + index) % Buffer.Length];
            }
        }

        public RingBuffer(int initialCapacity)
        {
            Debug.Assert(initialCapacity > 0, "Ring buffer capacity must be greater than zero.");
            Buffer = new T[initialCapacity];

            CurReadPointer = 0;
            CurWritePointer = 0;
            Count = 0;
        }

        public void Add(T toAdd)
        {
            EnsureCapacity(Count + 1);
            AddUnsafe(toAdd);
        }

        public void AddUnsafe(T toAdd)
        {
            Debug.Assert(Count != Buffer.Length, "Cannot add an element when ring buffer is at capacity");
            Buffer[CurWritePointer] = toAdd;
            Count++;
            CurWritePointer = (CurWritePointer + 1) % Buffer.Length;
        }

        public ref T AllocateUnsafely()
        {
            Debug.Assert(Count != Buffer.Length, "Cannot allocate an element when ring buffer is at capacity");
            ref var toRet = ref Buffer[CurWritePointer];
            CurWritePointer = (CurWritePointer + 1) % Buffer.Length;
            Count++;
            return ref toRet;
        }

        public ref T ReadLast()
        {
            Debug.Assert(Count > 0, "Reading from an empty buffer");
            return ref this[CurReadPointer + Count];
        }

        public ref T ReadFirst()
        {
            Debug.Assert(Count > 0, "Reading from an empty buffer");
            return ref Buffer[CurReadPointer];
        }

        public void RemoveFirst()
        {
            Debug.Assert(Count > 0, "Removing from an empty buffer");
            CurReadPointer = (CurReadPointer + 1) % Buffer.Length;
            Count--;
        }

        public void RemoveLast()
        {
            Debug.Assert(Count > 0, "Removing from an empty buffer");
            if (CurWritePointer == 0)
                CurWritePointer = Buffer.Length - 1;
            else
                CurWritePointer--;
            Count--;
        }

        public void Clear()
        {
            CurWritePointer = 0;
            CurReadPointer = 0;
            Count = 0;
        }

        public void EnsureCapacity(int capacity)
        {
            if (Buffer == null || capacity > Buffer.Length)
            {
                var newBuffer = new T[Math.Max(capacity, Buffer.Length * 2)];
                for (int i = 0; i < Count; i++)
                {
                    newBuffer[i] = Buffer[CurReadPointer];
                    CurReadPointer = (CurReadPointer + 1) % Buffer.Length;
                }

                Buffer = newBuffer;
                CurReadPointer = 0;
                CurWritePointer = Count;
            }
        }
    }
}
