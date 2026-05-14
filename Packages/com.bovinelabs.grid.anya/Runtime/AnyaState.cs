using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.Anya
{
    public struct AnyaNodeKey : IEquatable<AnyaNodeKey>
    {
        public long Lq;
        public long Rq;
        public int Y;
        public long RootXq;
        public long RootYq;
        public int Dy;

        public bool Equals(AnyaNodeKey other) =>
            Lq == other.Lq && Rq == other.Rq && Y == other.Y &&
            RootXq == other.RootXq && RootYq == other.RootYq && Dy == other.Dy;

        public override int GetHashCode()
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + Lq.GetHashCode();
                h = h * 31 + Rq.GetHashCode();
                h = h * 31 + Y;
                h = h * 31 + RootXq.GetHashCode();
                h = h * 31 + RootYq.GetHashCode();
                h = h * 31 + Dy;
                return h;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct AnyaState : IDisposable
    {
        public Grid2D Grid;
        public DoubleMinHeap Heap;
        public UnsafeList<AnyaNode> Pool;
        public NativeHashMap<AnyaNodeKey, int> NodeLookup;
        public double* RootGCost;
        public AllocatorManager.AllocatorHandle Allocator;

        public int2 Start;
        public int2 Goal;
        public int BestNode;
        public double BestCost;
        public byte SearchComplete;

        public void Dispose()
        {
            if (Heap.IsCreated) Heap.Dispose();
            if (Pool.IsCreated) Pool.Dispose();
            if (NodeLookup.IsCreated) NodeLookup.Dispose();
            if (RootGCost!= null)
            {
                AllocatorManager.Free(Allocator, RootGCost);
                RootGCost = null;
            }
            this = default;
        }
    }
}