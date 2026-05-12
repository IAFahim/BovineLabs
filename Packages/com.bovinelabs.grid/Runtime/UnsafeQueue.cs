using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BovineLabs.Grid
{
    /// <summary>Extremely fast, non-thread-safe queue for BFS algorithms.</summary>
    public unsafe struct UnsafeFastQueue<T> : IDisposable where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        public T* Data;
        public int Head;
        public int Tail;
        public int Capacity;
        public Allocator Allocator;

        public bool IsCreated => Data != null;
        public int Count => Tail - Head;
        public bool IsEmpty => Head == Tail;

        public static UnsafeFastQueue<T> Create(int capacity, Allocator allocator)
        {
            return new UnsafeFastQueue<T>
            {
                Data = (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>() * capacity, UnsafeUtility.AlignOf<T>(), allocator),
                Head = 0,
                Tail = 0,
                Capacity = capacity,
                Allocator = allocator
            };
        }

        public void Enqueue(T item)
        {
            Data[Tail++] = item;
        }

        public bool TryDequeue(out T item)
        {
            if (Head == Tail)
            {
                item = default;
                return false;
            }
            item = Data[Head++];
            return true;
        }

        public void Clear()
        {
            Head = 0;
            Tail = 0;
        }

        public void Dispose()
        {
            if (Data != null)
            {
                UnsafeUtility.Free(Data, Allocator);
                Data = null;
            }
        }
    }
}
