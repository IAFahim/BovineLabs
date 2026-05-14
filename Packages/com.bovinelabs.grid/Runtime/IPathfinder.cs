using System;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid
{
    public interface IPathfinder : IDisposable
    {
        PathResult FindPath(int2 start, int2 goal, Allocator allocator);
    }


    [Obsolete("Use MinHeap from BovineLabs.Grid instead — it uses raw pointers and is Burst-optimized.")]
    public struct NativeMinHeap : IDisposable
    {
        private NativeArray<float> priorities;
        private NativeArray<int> ids;
        private NativeArray<int> heapToNode;
        private NativeArray<int> nodeToHeap;
        private int capacity;

        public NativeMinHeap(int initialCapacity, Allocator allocator)
        {
            resizeAllocator = allocator;
            capacity = initialCapacity;
            priorities = new NativeArray<float>(capacity, allocator);
            ids = new NativeArray<int>(capacity, allocator);
            heapToNode = new NativeArray<int>(capacity, allocator);
            nodeToHeap = new NativeArray<int>(capacity, allocator);
            Count = 0;
            for (var i = 0; i < capacity; i++) nodeToHeap[i] = -1;
        }

        public int Count { get; private set; }

        public unsafe void Push(int nodeId, float priority)
        {
            if (Count >= capacity) Resize();
            var heapIdx = Count;
            priorities[heapIdx] = priority;
            ids[heapIdx] = nodeId;
            nodeToHeap[nodeId] = heapIdx;
            heapToNode[heapIdx] = nodeId;
            Count++;
            SiftUp(heapIdx);
        }

        public int Pop()
        {
            var nodeId = ids[0];
            nodeToHeap[nodeId] = -1;
            Count--;
            if (Count > 0)
            {
                ids[0] = ids[Count];
                priorities[0] = priorities[Count];
                heapToNode[0] = heapToNode[Count];
                nodeToHeap[ids[0]] = 0;
                SiftDown(0);
            }

            return nodeId;
        }

        public bool Contains(int nodeId)
        {
            return nodeId >= 0 && nodeId < capacity && nodeToHeap[nodeId] >= 0;
        }

        public unsafe void TryUpdate(int nodeId, float newPriority)
        {
            if (!Contains(nodeId)) return;
            var heapIdx = nodeToHeap[nodeId];
            var old = priorities[heapIdx];
            priorities[heapIdx] = newPriority;
            if (newPriority < old) SiftUp(heapIdx);
            else SiftDown(heapIdx);
        }

        private void SiftUp(int idx)
        {
            while (idx > 0)
            {
                var parent = (idx - 1) / 2;
                if (priorities[idx] >= priorities[parent]) break;
                Swap(idx, parent);
                idx = parent;
            }
        }

        private void SiftDown(int idx)
        {
            while (true)
            {
                var smallest = idx;
                var left = 2 * idx + 1;
                var right = 2 * idx + 2;
                if (left < Count && priorities[left] < priorities[smallest]) smallest = left;
                if (right < Count && priorities[right] < priorities[smallest]) smallest = right;
                if (smallest == idx) break;
                Swap(idx, smallest);
                idx = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            var tmpP = priorities[a];
            priorities[a] = priorities[b];
            priorities[b] = tmpP;
            var tmpId = ids[a];
            ids[a] = ids[b];
            ids[b] = tmpId;
            var tmpH = heapToNode[a];
            heapToNode[a] = heapToNode[b];
            heapToNode[b] = tmpH;
            nodeToHeap[ids[a]] = a;
            nodeToHeap[ids[b]] = b;
        }

        private readonly Allocator resizeAllocator;

        private void Resize()
        {
            var newCap = capacity * 2;
            var newP = new NativeArray<float>(newCap, resizeAllocator);
            var newIds = new NativeArray<int>(newCap, resizeAllocator);
            var newH2N = new NativeArray<int>(newCap, resizeAllocator);
            var newN2H = new NativeArray<int>(newCap, resizeAllocator);
            NativeArray<float>.Copy(priorities, newP, Count);
            NativeArray<int>.Copy(ids, newIds, Count);
            NativeArray<int>.Copy(heapToNode, newH2N, Count);
            NativeArray<int>.Copy(nodeToHeap, newN2H, capacity);
            for (var i = capacity; i < newCap; i++) newN2H[i] = -1;
            priorities.Dispose();
            ids.Dispose();
            heapToNode.Dispose();
            nodeToHeap.Dispose();
            priorities = newP;
            ids = newIds;
            heapToNode = newH2N;
            nodeToHeap = newN2H;
            capacity = newCap;
        }

        public unsafe void Dispose()
        {
            if (priorities.IsCreated) priorities.Dispose();
            if (ids.IsCreated) ids.Dispose();
            if (heapToNode.IsCreated) heapToNode.Dispose();
            if (nodeToHeap.IsCreated) nodeToHeap.Dispose();
        }
    }
}