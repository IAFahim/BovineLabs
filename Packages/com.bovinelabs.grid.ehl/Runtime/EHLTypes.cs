using System;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid.EHL
{
    /// <summary>
    /// A convex vertex of an obstacle polygon, identified by position and a unique ID.
    /// </summary>
    public struct ConvexVertex
    {
        public float2 Position;
        public int Id;

        public ConvexVertex(float2 position, int id)
        {
            Position = position;
            Id = id;
        }
    }

    /// <summary>
    /// An edge of an obstacle polygon defined by its two endpoints.
    /// </summary>
    public struct ObstacleEdge
    {
        public float2 A;
        public float2 B;

        public ObstacleEdge(float2 a, float2 b)
        {
            A = a;
            B = b;
        }
    }

    /// <summary>
    /// A hub label entry: for a vertex v, records that hub `HubVertexId` is on a shortest
    /// path from v, with distance `Distance`. `ViaVertexId` is the first successor from v
    /// toward the hub (used for path reconstruction). If ViaVertexId == HubVertexId, the
    /// hub is directly adjacent or is v itself.
    /// </summary>
    public struct VisibilityLabel : IComparable<VisibilityLabel>, IEquatable<VisibilityLabel>
    {
        public int HubVertexId;
        public float Distance;
        public int ViaVertexId;

        public VisibilityLabel(int hubVertexId, float distance, int viaVertexId)
        {
            HubVertexId = hubVertexId;
            Distance = distance;
            ViaVertexId = viaVertexId;
        }

        public int CompareTo(VisibilityLabel other)
        {
            // Sort by hub ID for efficient intersection scans
            int cmp = HubVertexId.CompareTo(other.HubVertexId);
            if (cmp != 0) return cmp;
            return Distance.CompareTo(other.Distance);
        }

        public bool Equals(VisibilityLabel other)
        {
            return HubVertexId == other.HubVertexId;
        }

        public override bool Equals(object obj) => obj is VisibilityLabel other && Equals(other);
        public override int GetHashCode() => HubVertexId;
    }

    /// <summary>
    /// A via-label for a grid cell: the cell can see convex vertex `VisibleVertexId`,
    /// which has hub `HubVertexId` at distance `HubDistance` via `ViaVertexId`.
    /// For query: vdist(s, hub) = |s - visibleVertex| + HubDistance.
    /// </summary>
    public struct ViaLabel : IComparable<ViaLabel>, IEquatable<ViaLabel>
    {
        public int HubVertexId;
        public float HubDistance;
        public int ViaVertexId;
        public int VisibleVertexId;

        public ViaLabel(int hubVertexId, float hubDistance, int viaVertexId, int visibleVertexId)
        {
            HubVertexId = hubVertexId;
            HubDistance = hubDistance;
            ViaVertexId = viaVertexId;
            VisibleVertexId = visibleVertexId;
        }

        public int CompareTo(ViaLabel other)
        {
            // Sort by hub ID for merge-style intersection
            return HubVertexId.CompareTo(other.HubVertexId);
        }

        public bool Equals(ViaLabel other) => HubVertexId == other.HubVertexId;
        public override bool Equals(object obj) => obj is ViaLabel other && Equals(other);
        public override int GetHashCode() => HubVertexId;
    }

    /// <summary>
    /// Represents a grid cell in the EHL overlay. Stores the axis-aligned bounds and
    /// a reference (offset + count) into the global via-label array.
    /// </summary>
    public struct GridCell
    {
        public float2 Min;
        public float2 Max;
        /// <summary>Start index in the global ViaLabel array.</summary>
        public int LabelStart;
        /// <summary>Number of via-labels for this cell.</summary>
        public int LabelCount;

        public GridCell(float2 min, float2 max, int labelStart, int labelCount)
        {
            Min = min;
            Max = max;
            LabelStart = labelStart;
            LabelCount = labelCount;
        }

        public bool Contains(float2 p)
        {
            return p.x >= Min.x && p.x < Max.x && p.y >= Min.y && p.y < Max.y;
        }

        public float2 Center => (Min + Max) * 0.5f;
    }

    /// <summary>
    /// The full preprocessed EHL* index. Contains the grid cells, via-labels, convex vertices,
    /// and obstacle edges. All data is stored in NativeArrays for Burst compatibility.
    /// </summary>
    public struct EHLIndex : IDisposable
    {
        /// <summary>AABB of the entire map.</summary>
        public float2 MapMin;
        public float2 MapMax;

        /// <summary>Grid dimensions (cellsX x cellsY).</summary>
        public int2 GridDims;

        /// <summary>Size of each grid cell.</summary>
        public float2 CellSize;

        /// <summary>Grid cells arranged row-major [y * GridDims.x + x].</summary>
        public NativeArray<GridCell> Cells;

        /// <summary>All via-labels concatenated; each cell references a slice via LabelStart/LabelCount.</summary>
        public NativeArray<ViaLabel> ViaLabels;

        /// <summary>Convex vertices of all obstacles.</summary>
        public NativeArray<ConvexVertex> ConvexVertices;

        /// <summary>Obstacle edges.</summary>
        public NativeArray<ObstacleEdge> ObstacleEdges;

        /// <summary>
        /// Adjacency list for the visibility graph: for vertex v, edges start at AdjOffsets[v]
        /// and there are AdjCounts[v] entries in AdjEdges.
        /// </summary>
        public NativeArray<int> AdjOffsets;
        public NativeArray<int> AdjCounts;
        public NativeArray<AdjEdge> AdjEdges;

        /// <summary>Hub labels for each convex vertex: for vertex v, labels start at HubOffsets[v] with HubCounts[v] entries.</summary>
        public NativeArray<int> HubOffsets;
        public NativeArray<int> HubCounts;
        public NativeArray<VisibilityLabel> HubLabels;

        /// <summary>Successor map for path reconstruction: key = (vertexId << 32) | hubId, value = next vertex toward hub.</summary>
        public NativeHashMap<long, int> SuccessorMap;

        public bool IsCreated => Cells.IsCreated;

        public void Dispose()
        {
            if (Cells.IsCreated) Cells.Dispose();
            if (ViaLabels.IsCreated) ViaLabels.Dispose();
            if (ConvexVertices.IsCreated) ConvexVertices.Dispose();
            if (ObstacleEdges.IsCreated) ObstacleEdges.Dispose();
            if (AdjOffsets.IsCreated) AdjOffsets.Dispose();
            if (AdjCounts.IsCreated) AdjCounts.Dispose();
            if (AdjEdges.IsCreated) AdjEdges.Dispose();
            if (HubOffsets.IsCreated) HubOffsets.Dispose();
            if (HubCounts.IsCreated) HubCounts.Dispose();
            if (HubLabels.IsCreated) HubLabels.Dispose();
            if (SuccessorMap.IsCreated) SuccessorMap.Dispose();
        }

        /// <summary>
        /// Look up the grid cell index for a world-space point.
        /// </summary>
        public int CellIndex(float2 p)
        {
            int cx = (int)math.floor((p.x - MapMin.x) / CellSize.x);
            int cy = (int)math.floor((p.y - MapMin.y) / CellSize.y);
            cx = math.clamp(cx, 0, GridDims.x - 1);
            cy = math.clamp(cy, 0, GridDims.y - 1);
            return cy * GridDims.x + cx;
        }

        /// <summary>
        /// Get a slice of via-labels for a given cell index.
        /// </summary>
        public NativeSlice<ViaLabel> GetCellLabels(int cellIndex)
        {
            var cell = Cells[cellIndex];
            return new NativeSlice<ViaLabel>(ViaLabels, cell.LabelStart, cell.LabelCount);
        }
    }

    /// <summary>
    /// An edge in the visibility graph adjacency list.
    /// </summary>
    public struct AdjEdge : IComparable<AdjEdge>
    {
        public int TargetVertexId;
        public float Distance;

        public AdjEdge(int target, float distance)
        {
            TargetVertexId = target;
            Distance = distance;
        }

        public int CompareTo(AdjEdge other) => TargetVertexId.CompareTo(other.TargetVertexId);
    }

    /// <summary>
    /// Result of an EHL* shortest path query.
    /// </summary>
    public struct EHLQueryResult
    {
        /// <summary>Shortest distance found, or float.MaxValue if no path.</summary>
        public float Distance;
        /// <summary>Path waypoints from source to target (inclusive). Empty if no path.</summary>
        public NativeList<float2> Waypoints;
        /// <summary>Whether a path was found.</summary>
        public bool PathFound;

        public EHLQueryResult(Allocator allocator)
        {
            Distance = float.MaxValue;
            Waypoints = new NativeList<float2>(allocator);
            PathFound = false;
        }

        public void Dispose()
        {
            if (Waypoints.IsCreated) Waypoints.Dispose();
        }
    }
}
