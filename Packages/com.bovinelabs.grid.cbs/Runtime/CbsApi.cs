using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.Cbs
{
    public struct AgentTask
    {
        public int Start;
        public int Goal;
    }

    public struct CbsConstraint
    {
        public int Agent;
        public int Cell;
        public int CellFrom;
        public int Time;
    }

    public struct CbsNode
    {
        public int ConstraintOffset;
        public int ConstraintCount;
        public int PathOffset;
        public int LengthOffset;
        public float Cost;
        public int Parent;
    }

    public unsafe struct CbsState
    {
        public Grid2D Grid;
        public UnsafeList<CbsNode> Nodes;
        public UnsafeList<CbsConstraint> Constraints;
        public UnsafeList<int> FlatPaths;
        public UnsafeList<int> PathLengths;
        public MinHeap Heap;

        public byte SolveComplete;
        public int AgentCount;
        public int SolutionNode;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class CbsApi
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
                Allocator = a,
                Grid = g,
                Nodes = new UnsafeList<CbsNode>(maxNodes, a),
                Constraints = new UnsafeList<CbsConstraint>(maxNodes * 10, a),
                FlatPaths = new UnsafeList<int>(maxNodes * 100, a),
                PathLengths = new UnsafeList<int>(maxNodes * 10, a),
                Heap = heap
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
            var blockedPtr = (byte*)blocked.GetUnsafeReadOnlyPtr();
            var agentsPtr = (AgentTask*)agents.GetUnsafeReadOnlyPtr();

            if (!TryInitSolve(ref s, blockedPtr, agentsPtr, agents.Length))
                return false;

            while (s.SolveComplete == 0)
                if (!TryStepSolve(ref s, blockedPtr, agentsPtr))
                    break;

            return TryExtractSolution(ref s, ref resultFlatPaths, ref resultPathLengths);
        }

        [BurstCompile]
        public static bool TryInitSolve(
            ref CbsState s,
            byte* blocked,
            AgentTask* agents,
            int agentCount)
        {
            s.Nodes.Clear();
            s.Constraints.Clear();
            s.FlatPaths.Clear();
            s.PathLengths.Clear();
            s.Heap.Clear();
            s.SolveComplete = 0;
            s.AgentCount = agentCount;
            s.SolutionNode = -1;

            if (agentCount == 0)
            {
                s.SolveComplete = 1;
                return true;
            }

            var root = new CbsNode
            {
                ConstraintOffset = 0,
                ConstraintCount = 0,
                Parent = -1
            };

            if (!PlanNode(ref s, ref root, blocked, agents, agentCount)) return false;

            var rootIdx = s.Nodes.Length;
            s.Nodes.Add(root);
            if (!s.Heap.TryInsertOrDecrease(new HeapNode(rootIdx, root.Cost))) return false;
            return true;
        }

        [BurstCompile]
        public static bool TryStepSolve(ref CbsState s, byte* blocked, AgentTask* agents)
        {
            if (s.SolveComplete != 0) return false;

            if (s.Heap.IsEmpty)
            {
                s.SolveComplete = 2;
                return false;
            }

            if (!s.Heap.TryPop(out var heapNode))
            {
                s.SolveComplete = 2;
                return false;
            }

            var PIdx = heapNode.Id;
            var P = s.Nodes[PIdx];

            if (!FindConflict(in s, in P, s.AgentCount, out var a1, out var a2, out var conflictType, out var cell,
                    out var cellFrom, out var cellTo, out var t))
            {
                s.SolveComplete = 1;
                s.SolutionNode = PIdx;
                return false;
            }

            if (conflictType == 0)
            {
                if (!TryExpandChild(ref s, PIdx, a1,
                        new CbsConstraint { Agent = a1, Cell = cell, CellFrom = -1, Time = t }, blocked, agents,
                        s.AgentCount)) return false;
                if (!TryExpandChild(ref s, PIdx, a2,
                        new CbsConstraint { Agent = a2, Cell = cell, CellFrom = -1, Time = t }, blocked, agents,
                        s.AgentCount)) return false;
            }
            else
            {
                if (!TryExpandChild(ref s, PIdx, a1,
                        new CbsConstraint { Agent = a1, Cell = cellTo, CellFrom = cellFrom, Time = t }, blocked, agents,
                        s.AgentCount)) return false;
                if (!TryExpandChild(ref s, PIdx, a2,
                        new CbsConstraint { Agent = a2, Cell = cellFrom, CellFrom = cellTo, Time = t }, blocked, agents,
                        s.AgentCount)) return false;
            }

            return true;
        }

        [BurstCompile]
        public static bool TryExtractSolution(
            ref CbsState s,
            ref NativeList<int> resultFlatPaths,
            ref NativeList<int> resultPathLengths)
        {
            resultFlatPaths.Clear();
            resultPathLengths.Clear();

            if (s.SolveComplete != 1 || s.SolutionNode < 0) return false;

            var node = s.Nodes[s.SolutionNode];
            ExtractResult(in s, in node, s.AgentCount, ref resultFlatPaths, ref resultPathLengths);
            return true;
        }

        [BurstCompile]
        private static bool PlanNode(ref CbsState s, ref CbsNode node, byte* blocked, AgentTask* agents, int agentCount)
        {
            node.PathOffset = s.FlatPaths.Length;
            node.LengthOffset = s.PathLengths.Length;
            node.Cost = 0;

            var nodeConstraints = new UnsafeList<CbsConstraint>(32, Allocator.Temp);
            var p = node.Parent;
            while (p >= 0)
            {
                var pn = s.Nodes[p];
                for (var i = 0; i < pn.ConstraintCount; i++)
                    nodeConstraints.Add(s.Constraints[pn.ConstraintOffset + i]);
                p = pn.Parent;
            }

            for (var i = 0; i < node.ConstraintCount; i++)
                nodeConstraints.Add(s.Constraints[node.ConstraintOffset + i]);

            for (var a = 0; a < agentCount; a++)
            {
                var path = new NativeList<int>(Allocator.Temp);
                if (!TryAStar(ref s, blocked, a, agents[a].Start, agents[a].Goal, in nodeConstraints, ref path))
                {
                    path.Dispose();
                    nodeConstraints.Dispose();
                    return false;
                }

                if (Hint.Unlikely(s.FlatPaths.Length + path.Length > s.FlatPaths.Capacity))
                {
                    path.Dispose();
                    nodeConstraints.Dispose();
                    return false;
                }

                for (var i = 0; i < path.Length; i++) s.FlatPaths.Add(path[i]);

                if (Hint.Unlikely(s.PathLengths.Length >= s.PathLengths.Capacity))
                {
                    path.Dispose();
                    nodeConstraints.Dispose();
                    return false;
                }

                s.PathLengths.Add(path.Length);

                node.Cost += path.Length - 1;
                path.Dispose();
            }

            nodeConstraints.Dispose();
            return true;
        }

        [BurstCompile]
        private static bool FindConflict(in CbsState s, in CbsNode node, int agentCount,
            out int a1, out int a2, out int conflictType, out int cell, out int cellFrom, out int cellTo, out int t)
        {
            a1 = a2 = conflictType = cell = cellFrom = cellTo = t = -1;
            var pathLengthsPtr = s.PathLengths.Ptr + node.LengthOffset;

            for (var i = 0; i < agentCount; i++)
            for (var j = i + 1; j < agentCount; j++)
            {
                var lenI = pathLengthsPtr[i];
                var lenJ = pathLengthsPtr[j];
                var maxT = math.max(lenI, lenJ);
                for (var time = 0; time < maxT; time++)
                {
                    var cellI_t = GetCellAtTime(in s, in node, i, time);
                    var cellJ_t = GetCellAtTime(in s, in node, j, time);

                    if (cellI_t == cellJ_t)
                    {
                        a1 = i;
                        a2 = j;
                        conflictType = 0;
                        cell = cellI_t;
                        cellFrom = cellTo = -1;
                        t = time;
                        return true;
                    }

                    if (time + 1 < maxT)
                    {
                        var cellI_next = GetCellAtTime(in s, in node, i, time + 1);
                        var cellJ_next = GetCellAtTime(in s, in node, j, time + 1);
                        if (cellI_t == cellJ_next && cellI_next == cellJ_t)
                        {
                            a1 = i;
                            a2 = j;
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

            return false;
        }

        [BurstCompile]
        private static int GetCellAtTime(in CbsState s, in CbsNode node, int agentIdx, int time)
        {
            var pathLengthsPtr = s.PathLengths.Ptr + node.LengthOffset;
            var pathStart = node.PathOffset;
            for (var a = 0; a < agentIdx; a++) pathStart += pathLengthsPtr[a];
            var pathLength = pathLengthsPtr[agentIdx];
            var clampedTime = math.clamp(time, 0, pathLength - 1);
            return s.FlatPaths[pathStart + clampedTime];
        }

        [BurstCompile]
        private static bool TryExpandChild(ref CbsState s, int parentIdx, int agent, CbsConstraint constraint,
            byte* blocked, AgentTask* agents, int agentCount)
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
                var childIdx = s.Nodes.Length;
                s.Nodes.Add(child);
                return s.Heap.TryInsertOrDecrease(new HeapNode(childIdx, child.Cost));
            }

            return true;
        }

        [BurstCompile]
        private static void ExtractResult(in CbsState s, in CbsNode node, int agentCount, ref NativeList<int> flatPaths,
            ref NativeList<int> pathLengths)
        {
            var pathLengthsPtr = s.PathLengths.Ptr + node.LengthOffset;
            var flatPathsPtr = s.FlatPaths.Ptr + node.PathOffset;

            for (var a = 0; a < agentCount; a++)
            {
                var len = pathLengthsPtr[a];
                pathLengths.Add(len);
                for (var i = 0; i < len; i++) flatPaths.Add(flatPathsPtr[i]);
                flatPathsPtr += len;
            }
        }

        [BurstCompile]
        public static bool TryAStar(ref CbsState s, byte* blocked, int agentId, int start, int goal,
            in UnsafeList<CbsConstraint> constraints, ref NativeList<int> path)
        {
            path.Clear();
            var gridLen = s.Grid.Length;
            var width = s.Grid.Width;
            var timeHorizon = math.max(100, gridLen);

            var g = new Unity.Collections.NativeArray<float>(gridLen * timeHorizon, Allocator.Temp);
            var parent = new Unity.Collections.NativeArray<int>(gridLen * timeHorizon, Allocator.Temp);
            g.Fill(float.PositiveInfinity);
            parent.Fill(-1);

            var gPtr = (float*)g.GetUnsafePtr();
            var parentPtr = (int*)parent.GetUnsafePtr();

            gPtr[start] = 0f;
            if (!MinHeap.TryCreate(gridLen * timeHorizon, Allocator.Temp, out var heap))
            {
                g.Dispose();
                parent.Dispose();
                return false;
            }

            if (!heap.TryInsertOrDecrease(new HeapNode(start,
                    Grid2D.HeuristicManhattan(s.Grid.ToCoord(start), s.Grid.ToCoord(goal)))))
            {
                g.Dispose();
                parent.Dispose();
                heap.Dispose();
                return false;
            }

            while (!heap.IsEmpty)
            {
                if (!heap.TryPop(out var heapNode)) break;
                var stateIdx = heapNode.Id;
                var u = stateIdx % gridLen;
                var time = stateIdx / gridLen;

                if (u == goal)
                {
                    var cur = stateIdx;
                    while (cur >= 0)
                    {
                        path.Add(cur % gridLen);
                        cur = parentPtr[cur];
                    }

                    for (int i = 0, j = path.Length - 1; i < j; i++, j--)
                    {
                        var tmp = path[i];
                        path[i] = path[j];
                        path[j] = tmp;
                    }

                    break;
                }

                var up = s.Grid.ToCoord(u);
                var nextTime = time + 1;
                if (Hint.Unlikely(nextTime >= timeHorizon)) continue;

                for (var d = -1; d < 4; d++)
                {
                    var np = d == -1 ? up : up + Grid2D.Dir4(d);
                    if (Hint.Unlikely(np.x < 0 || np.y < 0 || np.x >= width || np.y >= s.Grid.Height)) continue;

                    var ni = np.y * width + np.x;
                    if (Hint.Unlikely(blocked[ni] != 0)) continue;

                    var constrained = false;
                    for (var c = 0; c < constraints.Length; c++)
                    {
                        var cons = constraints[c];
                        if (cons.Agent != agentId) continue;

                        if (cons.CellFrom < 0)
                        {
                            if (cons.Cell == ni && cons.Time == nextTime)
                            {
                                constrained = true;
                                break;
                            }
                        }
                        else
                        {
                            if (cons.Cell == ni && cons.CellFrom == u && cons.Time == nextTime)
                            {
                                constrained = true;
                                break;
                            }
                        }
                    }

                    if (constrained) continue;

                    var nextStateIdx = nextTime * gridLen + ni;
                    var newG = gPtr[stateIdx] + 1f;
                    if (newG < gPtr[nextStateIdx])
                    {
                        gPtr[nextStateIdx] = newG;
                        parentPtr[nextStateIdx] = stateIdx;
                        var f = newG + Grid2D.HeuristicManhattan(np, s.Grid.ToCoord(goal));
                        if (!heap.TryInsertOrDecrease(new HeapNode(nextStateIdx, f))) break;
                    }
                }
            }

            g.Dispose();
            parent.Dispose();
            heap.Dispose();
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