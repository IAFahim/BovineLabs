using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid
{
    /// <summary>
    /// Interface for all pathfinding algorithms in the grid system.
    /// Burst-compatible via struct-based job pattern.
    /// </summary>
    public interface IPathfinder : IDisposable
    {
        PathResult FindPath(int2 start, int2 goal, Allocator allocator);
    }

    /// <summary>
    /// Min-heap priority queue for A*-style searches.
    /// Stores integer node IDs with float priorities.
    /// </summary>
    [Obsolete("Use MinHeap from BovineLabs.Grid instead — it uses raw pointers and is Burst-optimized.")]
    public struct NativeMinHeap : IDisposable
    {
        private NativeArray<float> priorities;
        private NativeArray<int> ids;
        private NativeArray<int> heapToNode;
        private NativeArray<int> nodeToHeap;
        private int count;
        private int capacity;

        public NativeMinHeap(int initialCapacity, Allocator allocator)
        {
            resizeAllocator = allocator;
            capacity = initialCapacity;
            priorities = new NativeArray<float>(capacity, allocator);
            ids = new NativeArray<int>(capacity, allocator);
            heapToNode = new NativeArray<int>(capacity, allocator);
            nodeToHeap = new NativeArray<int>(capacity, allocator);
            count = 0;
            for (int i = 0; i < capacity; i++) nodeToHeap[i] = -1;
        }

        public int Count => count;

        public void Push(int nodeId, float priority)
        {
            if (count >= capacity) Resize();
            int heapIdx = count;
            priorities[heapIdx] = priority;
            ids[heapIdx] = nodeId;
            nodeToHeap[nodeId] = heapIdx;
            heapToNode[heapIdx] = nodeId;
            count++;
            SiftUp(heapIdx);
        }

        public int Pop()
        {
            int nodeId = ids[0];
            nodeToHeap[nodeId] = -1;
            count--;
            if (count > 0)
            {
                ids[0] = ids[count];
                priorities[0] = priorities[count];
                heapToNode[0] = heapToNode[count];
                nodeToHeap[ids[0]] = 0;
                SiftDown(0);
            }
            return nodeId;
        }

        public bool Contains(int nodeId) =>
            nodeId >= 0 && nodeId < capacity && nodeToHeap[nodeId] >= 0;

        public void TryUpdate(int nodeId, float newPriority)
        {
            if (!Contains(nodeId)) return;
            int heapIdx = nodeToHeap[nodeId];
            float old = priorities[heapIdx];
            priorities[heapIdx] = newPriority;
            if (newPriority < old) SiftUp(heapIdx);
            else SiftDown(heapIdx);
        }

        private void SiftUp(int idx)
        {
            while (idx > 0)
            {
                int parent = (idx - 1) / 2;
                if (priorities[idx] >= priorities[parent]) break;
                Swap(idx, parent);
                idx = parent;
            }
        }

        private void SiftDown(int idx)
        {
            while (true)
            {
                int smallest = idx;
                int left = 2 * idx + 1;
                int right = 2 * idx + 2;
                if (left < count && priorities[left] < priorities[smallest]) smallest = left;
                if (right < count && priorities[right] < priorities[smallest]) smallest = right;
                if (smallest == idx) break;
                Swap(idx, smallest);
                idx = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            float tmpP = priorities[a]; priorities[a] = priorities[b]; priorities[b] = tmpP;
            int tmpId = ids[a]; ids[a] = ids[b]; ids[b] = tmpId;
            int tmpH = heapToNode[a]; heapToNode[a] = heapToNode[b]; heapToNode[b] = tmpH;
            nodeToHeap[ids[a]] = a;
            nodeToHeap[ids[b]] = b;
        }

        private Allocator resizeAllocator;

        private void Resize()
        {
            int newCap = capacity * 2;
            var newP = new NativeArray<float>(newCap, resizeAllocator);
            var newIds = new NativeArray<int>(newCap, resizeAllocator);
            var newH2N = new NativeArray<int>(newCap, resizeAllocator);
            var newN2H = new NativeArray<int>(newCap, resizeAllocator);
            NativeArray<float>.Copy(priorities, newP, count);
            NativeArray<int>.Copy(ids, newIds, count);
            NativeArray<int>.Copy(heapToNode, newH2N, count);
            NativeArray<int>.Copy(nodeToHeap, newN2H, capacity);
            for (int i = capacity; i < newCap; i++) newN2H[i] = -1;
            priorities.Dispose(); ids.Dispose(); heapToNode.Dispose(); nodeToHeap.Dispose();
            priorities = newP; ids = newIds; heapToNode = newH2N; nodeToHeap = newN2H;
            capacity = newCap;
        }

        public void Dispose()
        {
            if (priorities.IsCreated) priorities.Dispose();
            if (ids.IsCreated) ids.Dispose();
            if (heapToNode.IsCreated) heapToNode.Dispose();
            if (nodeToHeap.IsCreated) nodeToHeap.Dispose();
        }
    }
}
