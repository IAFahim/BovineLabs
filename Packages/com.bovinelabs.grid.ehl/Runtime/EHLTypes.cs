using System;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid.EHL
{
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
            var cmp = HubVertexId.CompareTo(other.HubVertexId);
            if (cmp != 0) return cmp;
            return Distance.CompareTo(other.Distance);
        }

        public bool Equals(VisibilityLabel other)
        {
            return HubVertexId == other.HubVertexId;
        }

        public override bool Equals(object obj)
        {
            return obj is VisibilityLabel other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HubVertexId;
        }
    }


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
            return HubVertexId.CompareTo(other.HubVertexId);
        }

        public bool Equals(ViaLabel other)
        {
            return HubVertexId == other.HubVertexId;
        }

        public override bool Equals(object obj)
        {
            return obj is ViaLabel other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HubVertexId;
        }
    }


    public struct GridCell
    {
        public float2 Min;
        public float2 Max;

        public int LabelStart;

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


    public struct EHLIndex : IDisposable
    {
        public float2 MapMin;
        public float2 MapMax;


        public int2 GridDims;


        public float2 CellSize;


        public NativeArray<GridCell> Cells;


        public NativeArray<ViaLabel> ViaLabels;


        public NativeArray<ConvexVertex> ConvexVertices;


        public NativeArray<ObstacleEdge> ObstacleEdges;


        public NativeArray<int> AdjOffsets;
        public NativeArray<int> AdjCounts;
        public NativeArray<AdjEdge> AdjEdges;


        public NativeArray<int> HubOffsets;
        public NativeArray<int> HubCounts;
        public NativeArray<VisibilityLabel> HubLabels;


        public NativeHashMap<long, int> SuccessorMap;

        public bool IsCreated => Cells.IsCreated;

        public unsafe void Dispose()
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


        public int CellIndex(float2 p)
        {
            var cx = (int)math.floor((p.x - MapMin.x) / CellSize.x);
            var cy = (int)math.floor((p.y - MapMin.y) / CellSize.y);
            cx = math.clamp(cx, 0, GridDims.x - 1);
            cy = math.clamp(cy, 0, GridDims.y - 1);
            return cy * GridDims.x + cx;
        }


        public NativeSlice<ViaLabel> GetCellLabels(int cellIndex)
        {
            var cell = Cells[cellIndex];
            return new NativeSlice<ViaLabel>(ViaLabels, cell.LabelStart, cell.LabelCount);
        }
    }


    public struct AdjEdge : IComparable<AdjEdge>
    {
        public int TargetVertexId;
        public float Distance;

        public AdjEdge(int target, float distance)
        {
            TargetVertexId = target;
            Distance = distance;
        }

        public int CompareTo(AdjEdge other)
        {
            return TargetVertexId.CompareTo(other.TargetVertexId);
        }
    }


    public struct EHLQueryResult
    {
        public float Distance;

        public NativeList<float2> Waypoints;

        public bool PathFound;

        public EHLQueryResult(Allocator allocator)
        {
            Distance = float.MaxValue;
            Waypoints = new NativeList<float2>(allocator);
            PathFound = false;
        }

        public unsafe void Dispose()
        {
            if (Waypoints.IsCreated) Waypoints.Dispose();
        }
    }
}