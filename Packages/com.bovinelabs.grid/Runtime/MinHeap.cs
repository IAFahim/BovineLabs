using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.CompilerServices;

namespace BovineLabs.Grid
{
    /// <summary>Min-heap for A*/Dijkstra-style priority queues. Optimized with unsafe pointers and Burst hints.</summary>
    [BurstCompile]
    public unsafe struct MinHeap
    {
        [NativeDisableUnsafePtrRestriction]
        public HeapNode* Data;
        [NativeDisableUnsafePtrRestriction]
        public int* Positions; // id -> index in Data, -1 if not present
        
        public int Capacity;
        public int Count;
        public Allocator Allocator;

        public bool IsCreated => Data != null;

        public static MinHeap Create(int maxId, Allocator allocator)
        {
            var h = new MinHeap
            {
                Data = (HeapNode*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<HeapNode>() * maxId, UnsafeUtility.AlignOf<HeapNode>(), allocator),
                Positions = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * maxId, UnsafeUtility.AlignOf<int>(), allocator),
                Capacity = maxId,
                Count = 0,
                Allocator = allocator
            };
            
            h.Clear();
            return h;
        }

        public int Length => Count;

        public bool IsEmpty => Count == 0;

        public void Clear()
        {
            Count = 0;
            UnsafeUtility.MemSet(Positions, 0xFF, (long)Capacity * UnsafeUtility.SizeOf<int>());
        }

        [BurstCompile]
        public void InsertOrDecrease([NoAlias] HeapNode node)
        {
            CheckBounds(node.Id);
            int pos = Positions[node.Id];
            if (pos >= 0)
            {
                if (Less(node, Data[pos]))
                {
                    Data[pos] = node;
                    SiftUp(pos);
                }
            }
            else
            {
                int idx = Count++;
                Data[idx] = node;
                Positions[node.Id] = idx;
                SiftUp(idx);
            }
        }

        [BurstCompile]
        public HeapNode Pop()
        {
            HeapNode root = Data[0];
            Positions[root.Id] = -1;

            int last = --Count;
            if (Hint.Likely(last > 0))
            {
                Data[0] = Data[last];
                Positions[Data[0].Id] = 0;
                SiftDown(0);
            }

            return root;
        }

        public bool Contains(int id) => Positions[id] >= 0;

        public HeapNode Peek() => Data[0];

        public void Dispose()
        {
            if (Data != null)
            {
                UnsafeUtility.Free(Data, Allocator);
                UnsafeUtility.Free(Positions, Allocator);
                Data = null;
                Positions = null;
            }
        }

        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (!Less(Data[i], Data[parent])) break;
                Swap(i, parent);
                i = parent;
            }
        }

        private void SiftDown(int i)
        {
            while (true)
            {
                int left = (i << 1) + 1;
                int right = left + 1;
                int smallest = i;

                if (left < Count && Less(Data[left], Data[smallest])) smallest = left;
                if (right < Count && Less(Data[right], Data[smallest])) smallest = right;

                if (smallest == i) break;
                Swap(i, smallest);
                i = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            var tmp = Data[a];
            Data[a] = Data[b];
            Data[b] = tmp;
            Positions[Data[a].Id] = a;
            Positions[Data[b].Id] = b;
        }

        private static bool Less(HeapNode a, HeapNode b)
        {
            if (a.Key0 != b.Key0) return a.Key0 < b.Key0;
            return a.Key1 < b.Key1;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckBounds(int id)
        {
            if (Hint.Unlikely((uint)id >= (uint)Capacity))
                throw new IndexOutOfRangeException($"Heap ID {id} out of capacity {Capacity}");
        }
    }

    public static class NativeArrayExtensions
    {
        public static void Fill<T>(this NativeArray<T> arr, T value) where T : unmanaged
        {
            for (int i = 0; i < arr.Length; i++)
                arr[i] = value;
        }
    }
}
