using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid
{
    [BurstCompile]
    public unsafe struct DoubleMinHeap
    {
        [NativeDisableUnsafePtrRestriction]
        public DoubleHeapNode* Data;
        [NativeDisableUnsafePtrRestriction]
        public int* Positions;

        public int Capacity;
        public int Count;
        public Allocator Allocator;

        public bool IsCreated => Data != null;

        public static bool TryCreate(int maxId, Allocator allocator, out DoubleMinHeap result)
        {
            if (maxId <= 0)
            {
                result = default;
                return false;
            }

            result = new DoubleMinHeap
            {
                Data = (DoubleHeapNode*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<DoubleHeapNode>() * maxId, UnsafeUtility.AlignOf<DoubleHeapNode>(), allocator),
                Positions = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * maxId, UnsafeUtility.AlignOf<int>(), allocator),
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
        public bool TryInsertOrDecrease([NoAlias] DoubleHeapNode node)
        {
            if (Hint.Unlikely((uint)node.Id >= (uint)Capacity)) return false;

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
                if (Hint.Unlikely(Count >= Capacity)) return false;
                int idx = Count++;
                Data[idx] = node;
                Positions[node.Id] = idx;
                SiftUp(idx);
            }
            return true;
        }

        [BurstCompile]
        public bool TryPop(out DoubleHeapNode result)
        {
            if (Hint.Unlikely(Count == 0))
            {
                result = default;
                return false;
            }

            result = Data[0];
            Positions[result.Id] = -1;

            int last = --Count;
            if (Hint.Likely(last > 0))
            {
                Data[0] = Data[last];
                Positions[Data[0].Id] = 0;
                SiftDown(0);
            }

            return true;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Less(DoubleHeapNode a, DoubleHeapNode b)
        {
            if (a.Key0 != b.Key0) return a.Key0 < b.Key0;
            return a.Key1 < b.Key1;
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
    }
}
