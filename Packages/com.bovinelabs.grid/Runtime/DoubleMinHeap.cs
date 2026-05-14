using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BovineLabs.Grid
{
    [BurstCompile]
    public unsafe struct DoubleMinHeap
    {
        [NativeDisableUnsafePtrRestriction] public DoubleHeapNode* Data;
        [NativeDisableUnsafePtrRestriction] public int* Positions;

        public int Capacity;
        public int Count;
        public AllocatorManager.AllocatorHandle Allocator;

        public bool IsCreated => Data != null;

        public static bool TryCreate(int maxId, AllocatorManager.AllocatorHandle allocator, out DoubleMinHeap result)
        {
            if (maxId <= 0)
            {
                result = default;
                return false;
            }

            result = new DoubleMinHeap
            {
                Data = (DoubleHeapNode*)AllocatorManager.Allocate(allocator, UnsafeUtility.SizeOf<DoubleHeapNode>(),
                    UnsafeUtility.AlignOf<DoubleHeapNode>(), maxId),
                Positions = (int*)AllocatorManager.Allocate(allocator, UnsafeUtility.SizeOf<int>(),
                    UnsafeUtility.AlignOf<int>(), maxId),
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
        public bool TryPop(out DoubleHeapNode result)
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

        private void Swap(int a, int b)
        {
            (Data[a], Data[b]) = (Data[b], Data[a]);
            Positions[Data[a].Id] = a;
            Positions[Data[b].Id] = b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Less(DoubleHeapNode a, DoubleHeapNode b)
        {
            int c = a.Key0.CompareTo(b.Key0);
            if (c!= 0) return c < 0;
            return a.Key1.CompareTo(b.Key1) < 0;
        }

        public void Dispose()
        {
            if (Data != null)
            {
                AllocatorManager.Free(Allocator, Data);
                AllocatorManager.Free(Allocator, Positions);
                Data = null;
                Positions = null;
            }
        }
        
        public bool TryUpdate(DoubleHeapNode node)
        {
            if ((uint)node.Id >= (uint)Capacity) return false;
            var pos = Positions[node.Id];
            if (pos < 0) return false;

            var old = Data[pos];
            Data[pos] = node;
            if (Less(node, old)) SiftUp(pos);
            else if (Less(old, node)) SiftDown(pos);
            return true;
        }

        public bool Contains(int id) => (uint)id < (uint)Capacity && Positions[id] >= 0;
        public DoubleHeapNode Peek() => Data[0];
    }
}