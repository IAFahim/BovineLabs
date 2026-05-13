using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Cbs
{
    public struct AgentTask { public int Start; public int Goal; }

    /// <summary>
    /// Supports both vertex constraints (CellFrom == -1: agent cannot occupy Cell at Time)
    /// and edge constraints (CellFrom >= 0: agent cannot traverse edge CellFrom→Cell at Time).
    /// </summary>
    public struct CbsConstraint { public int Agent; public int Cell; public int CellFrom; public int Time; }

    public struct CbsNode
    {
        public int ConstraintOffset;
        public int ConstraintCount;
        public int PathOffset;
        public int LengthOffset;
        public float Cost;
        public int Parent;
    }

    public struct CbsState
    {
        public Grid2D Grid;
        public UnsafeList<CbsNode> Nodes;
        public UnsafeList<CbsConstraint> Constraints;
        public UnsafeList<int> FlatPaths;
        public UnsafeList<int> PathLengths;
        public MinHeap Heap;
    }

    [BurstCompile]
    public unsafe static class CbsApi
    {
        public static bool TryCreate(int width, int height, int maxNodes, Allocator a, out CbsState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g) || !MinHeap.TryCreate(maxNodes, a, out var heap))
            {
                result = default;
                return false;
            }

            result = new CbsState
            {
                Grid = g,
                Nodes = new UnsafeList<CbsNode>(maxNodes, a),
                Constraints = new UnsafeList<CbsConstraint>(maxNodes * 10, a),
                FlatPaths = new UnsafeList<int>(maxNodes * 100, a),
                PathLengths = new UnsafeList<int>(maxNodes * 10, a),
                Heap = heap,
            };
            return true;
        }

        [BurstCompile]
        public static bool TrySolve(
            ref CbsState s,
            in NativeArray<byte> blocked,
            in NativeArray<AgentTask> agents,
            ref NativeList<int> resultFlatPaths,
            ref NativeList<int> resultPathLengths)
        {
            s.Nodes.Clear();
            s.Constraints.Clear();
            s.FlatPaths.Clear();
            s.PathLengths.Clear();
            s.Heap.Clear();
            resultFlatPaths.Clear();
            resultPathLengths.Clear();

            int agentCount = agents.Length;
            if (agentCount == 0) return true;

            byte* blockedPtr = (byte*)blocked.GetUnsafeReadOnlyPtr();
            AgentTask* agentsPtr = (AgentTask*)agents.GetUnsafeReadOnlyPtr();

            var root = new CbsNode
            {
                ConstraintOffset = 0,
                ConstraintCount = 0,
                Parent = -1
            };

            if (!PlanNode(ref s, ref root, blockedPtr, agentsPtr, agentCount)) return false;

            int rootIdx = s.Nodes.Length;
            s.Nodes.Add(root);
            if (!s.Heap.TryInsertOrDecrease(new HeapNode(rootIdx, root.Cost))) return false;

            while (!s.Heap.IsEmpty)
            {
                if (!s.Heap.TryPop(out var heapNode)) return false;
                int PIdx = heapNode.Id;
                CbsNode P = s.Nodes[PIdx];

                if (!FindConflict(in s, in P, agentCount, out int a1, out int a2, out int conflictType, out int cell, out int cellFrom, out int cellTo, out int t))
                {
                    ExtractResult(in s, in P, agentCount, ref resultFlatPaths, ref resultPathLengths);
                    return true;
                }

                if (conflictType == 0)
                {
                    // Vertex conflict: constrain each agent from occupying cell at time t
                    if (!TryExpandChild(ref s, PIdx, a1, new CbsConstraint { Agent = a1, Cell = cell, CellFrom = -1, Time = t }, blockedPtr, agentsPtr, agentCount)) return false;
                    if (!TryExpandChild(ref s, PIdx, a2, new CbsConstraint { Agent = a2, Cell = cell, CellFrom = -1, Time = t }, blockedPtr, agentsPtr, agentCount)) return false;
                }
                else
                {
                    // Edge/swap conflict: constrain each agent from traversing the conflicting edge
                    // Agent a1 moved cellFrom→cellTo at time t; Agent a2 moved cellTo→cellFrom at time t
                    if (!TryExpandChild(ref s, PIdx, a1, new CbsConstraint { Agent = a1, Cell = cellTo, CellFrom = cellFrom, Time = t }, blockedPtr, agentsPtr, agentCount)) return false;
                    if (!TryExpandChild(ref s, PIdx, a2, new CbsConstraint { Agent = a2, Cell = cellFrom, CellFrom = cellTo, Time = t }, blockedPtr, agentsPtr, agentCount)) return false;
                }
            }

            return false;
        }

        [BurstCompile]
        private static bool PlanNode(ref CbsState s, ref CbsNode node, byte* blocked, AgentTask* agents, int agentCount)
        {
            node.PathOffset = s.FlatPaths.Length;
            node.LengthOffset = s.PathLengths.Length;
            node.Cost = 0;

            var nodeConstraints = new UnsafeList<CbsConstraint>(32, Allocator.Temp);
            int p = node.Parent;
            while (p >= 0)
            {
                var pn = s.Nodes[p];
                for (int i = 0; i < pn.ConstraintCount; i++)
                    nodeConstraints.Add(s.Constraints[pn.ConstraintOffset + i]);
                p = pn.Parent;
            }
            for (int i = 0; i < node.ConstraintCount; i++)
                nodeConstraints.Add(s.Constraints[node.ConstraintOffset + i]);

            for (int a = 0; a < agentCount; a++)
            {
                var path = new NativeList<int>(Allocator.Temp);
                if (!TryAStar(ref s, blocked, a, agents[a].Start, agents[a].Goal, in nodeConstraints, ref path))
                {
                    path.Dispose();
                    nodeConstraints.Dispose();
                    return false;
                }

                if (Hint.Unlikely(s.FlatPaths.Length + path.Length > s.FlatPaths.Capacity)) { path.Dispose(); nodeConstraints.Dispose(); return false; }
                for (int i = 0; i < path.Length; i++) s.FlatPaths.Add(path[i]);

                if (Hint.Unlikely(s.PathLengths.Length >= s.PathLengths.Capacity)) { path.Dispose(); nodeConstraints.Dispose(); return false; }
                s.PathLengths.Add(path.Length);

                node.Cost += (path.Length - 1);
                path.Dispose();
            }

            nodeConstraints.Dispose();
            return true;
        }

        /// <summary>
        /// Detects vertex and edge conflicts between all agent pairs.
        /// conflictType: 0 = vertex (two agents at same cell at same time), 1 = edge (swap conflict).
        /// </summary>
        [BurstCompile]
        private static bool FindConflict(in CbsState s, in CbsNode node, int agentCount,
            out int a1, out int a2, out int conflictType, out int cell, out int cellFrom, out int cellTo, out int t)
        {
            a1 = a2 = conflictType = cell = cellFrom = cellTo = t = -1;
            int* pathLengthsPtr = s.PathLengths.Ptr + node.LengthOffset;

            for (int i = 0; i < agentCount; i++)
            {
                for (int j = i + 1; j < agentCount; j++)
                {
                    int lenI = pathLengthsPtr[i];
                    int lenJ = pathLengthsPtr[j];
                    int maxT = math.max(lenI, lenJ);
                    for (int time = 0; time < maxT; time++)
                    {
                        int cellI_t = GetCellAtTime(in s, in node, i, time);
                        int cellJ_t = GetCellAtTime(in s, in node, j, time);

                        // Vertex conflict: both agents at same cell at same time
                        if (cellI_t == cellJ_t)
                        {
                            a1 = i; a2 = j;
                            conflictType = 0;
                            cell = cellI_t;
                            cellFrom = cellTo = -1;
                            t = time;
                            return true;
                        }

                        // Edge/swap conflict: agents swap positions
                        if (time + 1 < maxT)
                        {
                            int cellI_next = GetCellAtTime(in s, in node, i, time + 1);
                            int cellJ_next = GetCellAtTime(in s, in node, j, time + 1);
                            if (cellI_t == cellJ_next && cellI_next == cellJ_t)
                            {
                                a1 = i; a2 = j;
                                conflictType = 1;
                                cell = -1;
                                cellFrom = cellI_t;
                                cellTo = cellI_next;
                                t = time + 1;
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        [BurstCompile]
        private static int GetCellAtTime(in CbsState s, in CbsNode node, int agentIdx, int time)
        {
            int* pathLengthsPtr = s.PathLengths.Ptr + node.LengthOffset;
            int pathStart = node.PathOffset;
            for (int a = 0; a < agentIdx; a++) pathStart += pathLengthsPtr[a];
            int pathLength = pathLengthsPtr[agentIdx];
            int clampedTime = math.clamp(time, 0, pathLength - 1);
            return s.FlatPaths[pathStart + clampedTime];
        }

        [BurstCompile]
        private static bool TryExpandChild(ref CbsState s, int parentIdx, int agent, CbsConstraint constraint, byte* blocked, AgentTask* agents, int agentCount)
        {
            if (Hint.Unlikely(s.Constraints.Length >= s.Constraints.Capacity)) return false;

            var child = new CbsNode
            {
                ConstraintOffset = s.Constraints.Length,
                ConstraintCount = 1,
                Parent = parentIdx
            };
            s.Constraints.Add(constraint);

            if (PlanNode(ref s, ref child, blocked, agents, agentCount))
            {
                if (Hint.Unlikely(s.Nodes.Length >= s.Nodes.Capacity)) return false;
                int childIdx = s.Nodes.Length;
                s.Nodes.Add(child);
                return s.Heap.TryInsertOrDecrease(new HeapNode(childIdx, child.Cost));
            }
            return true;
        }

        [BurstCompile]
        private static void ExtractResult(in CbsState s, in CbsNode node, int agentCount, ref NativeList<int> flatPaths, ref NativeList<int> pathLengths)
        {
            int* pathLengthsPtr = s.PathLengths.Ptr + node.LengthOffset;
            int* flatPathsPtr = s.FlatPaths.Ptr + node.PathOffset;

            for (int a = 0; a < agentCount; a++)
            {
                int len = pathLengthsPtr[a];
                pathLengths.Add(len);
                for (int i = 0; i < len; i++) flatPaths.Add(flatPathsPtr[i]);
                flatPathsPtr += len;
            }
        }

        [BurstCompile]
        public static bool TryAStar(ref CbsState s, byte* blocked, int agentId, int start, int goal,
            in UnsafeList<CbsConstraint> constraints, ref NativeList<int> path)
        {
            path.Clear();
            int gridLen = s.Grid.Length;
            int width = s.Grid.Width;
            int timeHorizon = math.max(100, gridLen);

            var g = new NativeArray<float>(gridLen * timeHorizon, Allocator.Temp);
            var parent = new NativeArray<int>(gridLen * timeHorizon, Allocator.Temp);
            g.Fill(float.PositiveInfinity);
            parent.Fill(-1);

            float* gPtr = (float*)g.GetUnsafePtr();
            int* parentPtr = (int*)parent.GetUnsafePtr();

            gPtr[start] = 0f;
            if (!MinHeap.TryCreate(gridLen * timeHorizon, Allocator.Temp, out var heap))
            {
                g.Dispose(); parent.Dispose();
                return false;
            }
            if (!heap.TryInsertOrDecrease(new HeapNode(start, Grid2D.HeuristicManhattan(s.Grid.ToCoord(start), s.Grid.ToCoord(goal)))))
            {
                g.Dispose(); parent.Dispose(); heap.Dispose();
                return false;
            }

            while (!heap.IsEmpty)
            {
                if (!heap.TryPop(out var heapNode)) break;
                int stateIdx = heapNode.Id;
                int u = stateIdx % gridLen;
                int time = stateIdx / gridLen;

                if (u == goal)
                {
                    int cur = stateIdx;
                    while (cur >= 0) { path.Add(cur % gridLen); cur = parentPtr[cur]; }
                    for (int i = 0, j = path.Length - 1; i < j; i++, j--)
                    { int tmp = path[i]; path[i] = path[j]; path[j] = tmp; }
                    break;
                }

                int2 up = s.Grid.ToCoord(u);
                int nextTime = time + 1;
                if (Hint.Unlikely(nextTime >= timeHorizon)) continue;

                for (int d = -1; d < 4; d++)
                {
                    int2 np = (d == -1) ? up : up + Grid2D.Dir4(d);
                    if (Hint.Unlikely(np.x < 0 || np.y < 0 || np.x >= width || np.y >= s.Grid.Height)) continue;

                    int ni = np.y * width + np.x;
                    if (Hint.Unlikely(blocked[ni] != 0)) continue;

                    bool constrained = false;
                    for (int c = 0; c < constraints.Length; c++)
                    {
                        var cons = constraints[c];
                        if (cons.Agent != agentId) continue;

                        if (cons.CellFrom < 0)
                        {
                            // Vertex constraint: agent cannot occupy cons.Cell at cons.Time
                            if (cons.Cell == ni && cons.Time == nextTime)
                            { constrained = true; break; }
                        }
                        else
                        {
                            // Edge constraint: agent cannot traverse cons.CellFrom → cons.Cell at cons.Time
                            if (cons.Cell == ni && cons.CellFrom == u && cons.Time == nextTime)
                            { constrained = true; break; }
                        }
                    }
                    if (constrained) continue;

                    int nextStateIdx = nextTime * gridLen + ni;
                    float newG = gPtr[stateIdx] + 1f;
                    if (newG < gPtr[nextStateIdx])
                    {
                        gPtr[nextStateIdx] = newG;
                        parentPtr[nextStateIdx] = stateIdx;
                        float f = newG + Grid2D.HeuristicManhattan(np, s.Grid.ToCoord(goal));
                        if (!heap.TryInsertOrDecrease(new HeapNode(nextStateIdx, f))) break;
                    }
                }
            }

            g.Dispose(); parent.Dispose(); heap.Dispose();
            return path.Length > 0;
        }

        public static void Dispose(ref CbsState s)
        {
            if (s.Nodes.IsCreated) s.Nodes.Dispose();
            if (s.Constraints.IsCreated) s.Constraints.Dispose();
            if (s.FlatPaths.IsCreated) s.FlatPaths.Dispose();
            if (s.PathLengths.IsCreated) s.PathLengths.Dispose();
            if (s.Heap.IsCreated) s.Heap.Dispose();
        }
    }
}
