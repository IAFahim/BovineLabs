using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.Anya
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct AnyaState : IDisposable
    {
        public Grid2D Grid;
        public DoubleMinHeap Heap;
        public UnsafeList<AnyaNode> Pool;
        public double* RootGCost;
        public AllocatorManager.AllocatorHandle Allocator;

        public int2 Start;
        public int2 Goal;
        public int BestNode;
        public double BestCost;
        public byte SearchComplete;

        public unsafe void Dispose()
        {
            if (Heap.IsCreated) Heap.Dispose();
            if (Pool.IsCreated) Pool.Dispose();
            if (RootGCost != null)
            {
                AllocatorManager.Free(Allocator, RootGCost);
                RootGCost = null;
            }

            this = default;
        }
    }
}