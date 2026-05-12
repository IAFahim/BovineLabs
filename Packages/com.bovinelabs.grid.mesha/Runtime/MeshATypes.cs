using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid.MeshA
{
    /// <summary>
    /// A motion primitive: a precomputed kinodynamically feasible trajectory segment.
    /// Defined in local coordinates starting at (0,0) with a specific heading.
    /// </summary>
    public struct MotionPrimitive
    {
        public int Id;
        public int StartTheta;              // Discrete start heading
        public int2 GoalOffset;             // Relative goal position (di, dj)
        public int GoalTheta;               // End heading
        public float ArcLength;             // Physical length of trajectory
        public float HeadingChange;         // Accumulated heading change (radians)

        /// <summary>
        /// Grid cells swept by the agent body during this primitive.
        /// Stored as flat arrays: swept_i[k], swept_j[k] give the k-th cell.
        /// </summary>
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

        /// <summary>
        /// Check if this primitive is collision-free when starting at (baseX, baseY).
        /// startIdx allows skipping prefix cells (optimization for partial checks).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsCollisionFree(in NativeGrid2D grid, int baseX, int baseY, int startIdx)
        {
            for (int k = startIdx; k < SweptCellCount; k++)
            {
                int cx = baseX + SweptCellsI[k];
                int cy = baseY + SweptCellsJ[k];
                if (!grid.InBounds(new int2(cx, cy)) || !grid.IsFree(new int2(cx, cy)))
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Stores a set of motion primitives indexed by ID.
    /// </summary>
    public struct PrimitiveSet : IDisposable
    {
        public NativeList<MotionPrimitive> Primitives;
        public NativeParallelMultiHashMap<int, int> PrimsByHeading; // heading -> list of prim indices

        public PrimitiveSet(int capacity, Allocator allocator)
        {
            Primitives = new NativeList<MotionPrimitive>(capacity, allocator);
            PrimsByHeading = new NativeParallelMultiHashMap<int, int>(capacity, allocator);
        }

        public void Add(MotionPrimitive prim)
        {
            int idx = Primitives.Length;
            Primitives.Add(prim);
            PrimsByHeading.Add(prim.StartTheta, idx);
        }

        public void Dispose()
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

    /// <summary>
    /// A successor transition in the mesh graph.
    /// From extended cell (i,j,configId) → (i+di, j+dj, nextConfig).
    /// connectingPrimId >= 0 means this transition completes a specific primitive.
    /// </summary>
    public struct SuccessorTransition
    {
        public int Di;
        public int Dj;
        public int NextConfigId;
        public int ConnectingPrimId;  // -1 if ambiguous (multiple prims share this transition)

        public SuccessorTransition(int di, int dj, int nextConfig, int primId)
        {
            Di = di;
            Dj = dj;
            NextConfigId = nextConfig;
            ConnectingPrimId = primId;
        }
    }

    /// <summary>
    /// A primitive endpoint within a configuration (used for Finals/pruning).
    /// (finalTheta, di, dj, kInTrace, primId)
    /// </summary>
    public struct PrimEndpoint
    {
        public int FinalTheta;
        public int Di;
        public int Dj;
        public int KInTrace;
        public int PrimId;
    }

    /// <summary>
    /// Precomputed mesh graph topology.
    /// Stores successor transitions and configuration metadata for MeshA*.
    /// </summary>
    public struct MeshGraphData : IDisposable
    {
        /// <summary>Flat array of all successor transitions, indexed by SuccOffsets[config]..+SuccCounts[config].</summary>
        public NativeArray<SuccessorTransition> SuccessorsFlat;
        public NativeArray<int> SuccOffsets;
        public NativeArray<int> SuccCounts;

        /// <summary>Maps heading theta → initial config ID.</summary>
        public NativeArray<int> InitialConfigByTheta;

        /// <summary>Maps config ID → heading theta (-1 if not initial).</summary>
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
            // Initialize to -1/0
            for (int i = 0; i < numHeadings; i++) InitialConfigByTheta[i] = -1;
            for (int i = 0; i < maxConfigs; i++) { ThetaByInitialConfig[i] = -1; SuccOffsets[i] = 0; SuccCounts[i] = 0; }
        }

        public void Dispose()
        {
            if (SuccessorsFlat.IsCreated) SuccessorsFlat.Dispose();
            if (SuccOffsets.IsCreated) SuccOffsets.Dispose();
            if (SuccCounts.IsCreated) SuccCounts.Dispose();
            if (InitialConfigByTheta.IsCreated) InitialConfigByTheta.Dispose();
            if (ThetaByInitialConfig.IsCreated) ThetaByInitialConfig.Dispose();
        }
    }

    /// <summary>
    /// An extended cell in the mesh graph: (x, y, configId).
    /// ConfigId is either an initial config (corresponding to a heading)
    /// or an intermediate config (trajectory mid-point, zero-cost transition).
    /// </summary>
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

        public bool Equals(ExtendedCell other) => X == other.X && Y == other.Y && ConfigId == other.ConfigId;
        public override int GetHashCode() => X * 73856093 ^ Y * 19349663 ^ ConfigId;
    }

    /// <summary>
    /// Search node for MeshA*. Tracks parent (always an Initial Extended Cell)
    /// and whether this node should be kept after closing.
    /// </summary>
    public struct MeshSearchNode
    {
        public ExtendedCell Cell;
        public float GCost;
        public float FCost;
        public int ParentIndex;  // Index in expanded nodes array (-1 = start)
        public bool IsInitial;   // True if this is an Initial Extended Cell
        public bool KeepAfterClosed; // False for intermediate nodes (free after expansion)
    }
}
