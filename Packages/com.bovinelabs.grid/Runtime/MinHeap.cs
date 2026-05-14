using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BovineLabs.Grid
{
    [BurstCompile]
    public unsafe struct MinHeap
    {
        [NativeDisableUnsafePtrRestriction] public HeapNode* Data;
        [NativeDisableUnsafePtrRestriction] public int* Positions;

        public int Capacity;
        public int Count;
        public Allocator Allocator;

        public bool IsCreated => Data != null;

        public static bool TryCreate(int maxId, Allocator allocator, out MinHeap result)
        {
            if (maxId <= 0)
            {
                result = default;
                return false;
            }

            result = new MinHeap
            {
                Data = (HeapNode*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<HeapNode>() * maxId,
                    UnsafeUtility.AlignOf<HeapNode>(), allocator),
                Positions = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * maxId,
                    UnsafeUtility.AlignOf<int>(), allocator),
                Capacity = maxId,
                Count = 0,
                Allocator = allocator
            };

            result.Clear();
            return true;
        }

        public int Length => Count;

        public bool IsEmpty => Count == 0;

        public void Clear()
        {
            Count = 0;
            UnsafeUtility.MemSet(Positions, 0xFF, (long)Capacity * UnsafeUtility.SizeOf<int>());
        }

        [BurstCompile]
        public bool TryInsertOrDecrease([NoAlias] HeapNode node)
        {
            if (Hint.Unlikely((uint)node.Id >= (uint)Capacity)) return false;

            var pos = Positions[node.Id];
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
                if (Hint.Unlikely(Count >= Capacity)) return false;
                var idx = Count++;
                Data[idx] = node;
                Positions[node.Id] = idx;
                SiftUp(idx);
            }

            return true;
        }

        [BurstCompile]
        public bool TryPop(out HeapNode result)
        {
            if (Hint.Unlikely(Count == 0))
            {
                result = default;
                return false;
            }

            result = Data[0];
            Positions[result.Id] = -1;

            var last = --Count;
            if (Hint.Likely(last > 0))
            {
                Data[0] = Data[last];
                Positions[Data[0].Id] = 0;
                SiftDown(0);
            }

            return true;
        }

        [BurstCompile]
        public bool TryRemove(int id)
        {
            if (Hint.Unlikely((uint)id >= (uint)Capacity)) return false;
            var pos = Positions[id];
            if (pos < 0) return false;

            Positions[id] = -1;
            var last = --Count;
            if (pos < last)
            {
                Data[pos] = Data[last];
                Positions[Data[pos].Id] = pos;
                SiftUp(pos);
                SiftDown(pos);
            }

            return true;
        }

        public bool Contains(int id)
        {
            return (uint)id < (uint)Capacity && Positions[id] >= 0;
        }

        public bool TryPeek(out HeapNode result)
        {
            if (Hint.Unlikely(Count == 0))
            {
                result = default;
                return false;
            }

            result = Data[0];
            return true;
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SiftUp(int i)
        {
            while (i > 0)
            {
                var parent = (i - 1) >> 1;
                if (!Less(Data[i], Data[parent])) break;
                Swap(i, parent);
                i = parent;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SiftDown(int i)
        {
            while (true)
            {
                var left = (i << 1) + 1;
                var right = left + 1;
                var smallest = i;

                if (left < Count && Less(Data[left], Data[smallest])) smallest = left;
                if (right < Count && Less(Data[right], Data[smallest])) smallest = right;

                if (smallest == i) break;
                Swap(i, smallest);
                i = smallest;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Swap(int a, int b)
        {
            var tmp = Data[a];
            Data[a] = Data[b];
            Data[b] = tmp;
            Positions[Data[a].Id] = a;
            Positions[Data[b].Id] = b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Less(HeapNode a, HeapNode b)
        {
            if (a.Key0 != b.Key0) return a.Key0 < b.Key0;
            return a.Key1 < b.Key1;
        }
    }

    public static class NativeArrayExtensions
    {
        public static void Fill<T>(this NativeArray<T> arr, T value) where T : unmanaged
        {
            for (var i = 0; i < arr.Length; i++)
                arr[i] = value;
        }
    }
}