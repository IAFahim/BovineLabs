using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid.MeshA
{
    public struct MotionPrimitive
    {
        public int Id;
        public int StartTheta;
        public int2 GoalOffset;
        public int GoalTheta;
        public float ArcLength;
        public float HeadingChange;


        public NativeArray<int> SweptCellsI;
        public NativeArray<int> SweptCellsJ;
        public int SweptCellCount;

        public MotionPrimitive(int id, int startTheta, int2 goalOffset, int goalTheta,
            float arcLength, float headingChange, NativeArray<int> sweptI, NativeArray<int> sweptJ)
        {
            Id = id;
            StartTheta = startTheta;
            GoalOffset = goalOffset;
            GoalTheta = goalTheta;
            ArcLength = arcLength;
            HeadingChange = headingChange;
            SweptCellsI = sweptI;
            SweptCellsJ = sweptJ;
            SweptCellCount = sweptI.Length;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsCollisionFree(in NativeGrid2D grid, int baseX, int baseY, int startIdx)
        {
            for (var k = startIdx; k < SweptCellCount; k++)
            {
                var cx = baseX + SweptCellsI[k];
                var cy = baseY + SweptCellsJ[k];
                if (!grid.InBounds(new int2(cx, cy)) || !grid.IsFree(new int2(cx, cy)))
                    return false;
            }

            return true;
        }
    }


    public struct PrimitiveSet : IDisposable
    {
        public NativeList<MotionPrimitive> Primitives;
        public NativeParallelMultiHashMap<int, int> PrimsByHeading;

        public PrimitiveSet(int capacity, Allocator allocator)
        {
            Primitives = new NativeList<MotionPrimitive>(capacity, allocator);
            PrimsByHeading = new NativeParallelMultiHashMap<int, int>(capacity, allocator);
        }

        public unsafe void Add(MotionPrimitive prim)
        {
            var idx = Primitives.Length;
            Primitives.Add(prim);
            PrimsByHeading.Add(prim.StartTheta, idx);
        }

        public unsafe void Dispose()
        {
            if (Primitives.IsCreated)
            {
                foreach (var p in Primitives)
                {
                    if (p.SweptCellsI.IsCreated) p.SweptCellsI.Dispose();
                    if (p.SweptCellsJ.IsCreated) p.SweptCellsJ.Dispose();
                }

                Primitives.Dispose();
            }

            if (PrimsByHeading.IsCreated) PrimsByHeading.Dispose();
        }
    }


    public struct SuccessorTransition
    {
        public int Di;
        public int Dj;
        public int NextConfigId;
        public int ConnectingPrimId;

        public SuccessorTransition(int di, int dj, int nextConfig, int primId)
        {
            Di = di;
            Dj = dj;
            NextConfigId = nextConfig;
            ConnectingPrimId = primId;
        }
    }


    public struct PrimEndpoint
    {
        public int FinalTheta;
        public int Di;
        public int Dj;
        public int KInTrace;
        public int PrimId;
    }


    public struct MeshGraphData : IDisposable
    {
        public NativeArray<SuccessorTransition> SuccessorsFlat;
        public NativeArray<int> SuccOffsets;
        public NativeArray<int> SuccCounts;


        public NativeArray<int> InitialConfigByTheta;


        public NativeArray<int> ThetaByInitialConfig;

        public int NumHeadings;
        public int MaxConfigs;

        public MeshGraphData(int numHeadings, int maxConfigs, Allocator allocator)
        {
            NumHeadings = numHeadings;
            MaxConfigs = maxConfigs;
            SuccessorsFlat = default;
            SuccOffsets = new NativeArray<int>(maxConfigs, allocator);
            SuccCounts = new NativeArray<int>(maxConfigs, allocator);
            InitialConfigByTheta = new NativeArray<int>(numHeadings, allocator);
            ThetaByInitialConfig = new NativeArray<int>(maxConfigs, allocator);

            for (var i = 0; i < numHeadings; i++) InitialConfigByTheta[i] = -1;
            for (var i = 0; i < maxConfigs; i++)
            {
                ThetaByInitialConfig[i] = -1;
                SuccOffsets[i] = 0;
                SuccCounts[i] = 0;
            }
        }

        public unsafe void Dispose()
        {
            if (SuccessorsFlat.IsCreated) SuccessorsFlat.Dispose();
            if (SuccOffsets.IsCreated) SuccOffsets.Dispose();
            if (SuccCounts.IsCreated) SuccCounts.Dispose();
            if (InitialConfigByTheta.IsCreated) InitialConfigByTheta.Dispose();
            if (ThetaByInitialConfig.IsCreated) ThetaByInitialConfig.Dispose();
        }
    }


    public struct ExtendedCell : IEquatable<ExtendedCell>
    {
        public int X;
        public int Y;
        public int ConfigId;

        public ExtendedCell(int x, int y, int configId)
        {
            X = x;
            Y = y;
            ConfigId = configId;
        }

        public bool Equals(ExtendedCell other)
        {
            return X == other.X && Y == other.Y && ConfigId == other.ConfigId;
        }

        public override int GetHashCode()
        {
            return X * 73856093 ^ Y * 19349663 ^ ConfigId;
        }
    }


    public struct MeshSearchNode
    {
        public ExtendedCell Cell;
        public float GCost;
        public float FCost;
        public int ParentIndex;
        public bool IsInitial;
        public bool KeepAfterClosed;
    }
}