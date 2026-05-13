using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Cbs
{
    public struct AgentTask { public int Start; public int Goal; }

    public struct CbsConstraint { public int Agent; public int Cell; public int Time; }

    public struct CbsNode
    {
        public int ConstraintOffset;
        public int ConstraintCount;
        public int PathOffset;   // Offset in FlatPaths
        public int LengthOffset; // Offset in PathLengths
        public float Cost;
        public int Parent;
    }

    public struct CbsState
    {
        public Grid2D Grid;
        public UnsafeList<CbsNode> Nodes;
        public UnsafeList<CbsConstraint> Constraints;
        public UnsafeList<int> FlatPaths;      // All paths for all nodes concatenated
        public UnsafeList<int> PathLengths;    // Lengths of each agent's path for each node
        public MinHeap Heap;
    }

    [BurstCompile]
    public unsafe static class CbsApi
    {
        public static CbsState Create(int width, int height, int maxNodes, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new CbsState
            {
                Grid = g,
                Nodes = new UnsafeList<CbsNode>(maxNodes, a),
                Constraints = new UnsafeList<CbsConstraint>(maxNodes * 10, a),
                FlatPaths = new UnsafeList<int>(maxNodes * 100, a),
                PathLengths = new UnsafeList<int>(maxNodes * 10, a),
                Heap = MinHeap.Create(maxNodes, a),
            };
        }

        [BurstCompile]
        public static bool Solve(
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

            // Root node
            var root = new CbsNode
            {
                ConstraintOffset = 0,
                ConstraintCount = 0,
                Parent = -1
            };

            if (!PlanNode(ref s, ref root, blockedPtr, agentsPtr, agentCount)) return false;

            int rootIdx = s.Nodes.Length;
            s.Nodes.Add(root);
            s.Heap.InsertOrDecrease(new HeapNode(rootIdx, root.Cost));

            while (!s.Heap.IsEmpty)
            {
                int PIdx = s.Heap.Pop().Id;
                CbsNode P = s.Nodes[PIdx];

                if (!FindConflict(in s, in P, agentCount, out int a1, out int a2, out int cell, out int t))
                {
                    ExtractResult(in s, in P, agentCount, ref resultFlatPaths, ref resultPathLengths);
                    return true;
                }

                ExpandChild(ref s, PIdx, a1, cell, t, blockedPtr, agentsPtr, agentCount);
                ExpandChild(ref s, PIdx, a2, cell, t, blockedPtr, agentsPtr, agentCount);
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
                if (!AStar(ref s, blocked, a, agents[a].Start, agents[a].Goal, in nodeConstraints, ref path))
                {
                    path.Dispose();
                    nodeConstraints.Dispose();
                    return false;
                }

                for (int i = 0; i < path.Length; i++) s.FlatPaths.Add(path[i]);
                s.PathLengths.Add(path.Length);
                node.Cost += (path.Length - 1);
                path.Dispose();
            }

            nodeConstraints.Dispose();
            return true;
        }

        [BurstCompile]
        private static bool FindConflict(in CbsState s, in CbsNode node, int agentCount, out int a1, out int a2, out int cell, out int t)
        {
            a1 = a2 = cell = t = -1;
            int* pathLengthsPtr = s.PathLengths.Ptr + node.LengthOffset;

            for (int i = 0; i < agentCount; i++)
            {
                for (int j = i + 1; j < agentCount; j++)
                {
                    int lenI = pathLengthsPtr[i];
                    int lenJ = pathLengthsPtr[j];
                    int maxT = math.min(lenI, lenJ);
                    for (int time = 0; time < maxT; time++)
                    {
                        int cellI = GetCellAtTime(in s, in node, i, time);
                        int cellJ = GetCellAtTime(in s, in node, j, time);
                        if (cellI == cellJ)
                        {
                            a1 = i; a2 = j; cell = cellI; t = time;
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
            int* pathLengthsPtr = s.PathLengths.Ptr + node.LengthOffset;
            int pathStart = node.PathOffset;
            for (int a = 0; a < agentIdx; a++) pathStart += pathLengthsPtr[a];
            return s.FlatPaths[pathStart + time];
        }

        [BurstCompile]
        private static void ExpandChild(ref CbsState s, int parentIdx, int agent, int cell, int time, byte* blocked, AgentTask* agents, int agentCount)
        {
            var child = new CbsNode
            {
                ConstraintOffset = s.Constraints.Length,
                ConstraintCount = 1,
                Parent = parentIdx
            };
            s.Constraints.Add(new CbsConstraint { Agent = agent, Cell = cell, Time = time });

            if (PlanNode(ref s, ref child, blocked, agents, agentCount))
            {
                int childIdx = s.Nodes.Length;
                s.Nodes.Add(child);
                s.Heap.InsertOrDecrease(new HeapNode(childIdx, child.Cost));
            }
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
        public static bool AStar(ref CbsState s, byte* blocked, int agentId, int start, int goal,
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
            var heap = MinHeap.Create(gridLen * timeHorizon, Allocator.Temp);
            heap.InsertOrDecrease(new HeapNode(start, Grid2D.HeuristicManhattan(s.Grid.ToCoord(start), s.Grid.ToCoord(goal))));

            while (!heap.IsEmpty)
            {
                int stateIdx = heap.Pop().Id;
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
                        if (cons.Agent == agentId && cons.Cell == ni && cons.Time == nextTime)
                        { constrained = true; break; }
                    }
                    if (constrained) continue;

                    int nextStateIdx = nextTime * gridLen + ni;
                    float newG = gPtr[stateIdx] + 1f;
                    if (newG < gPtr[nextStateIdx])
                    {
                        gPtr[nextStateIdx] = newG;
                        parentPtr[nextStateIdx] = stateIdx;
                        float f = newG + Grid2D.HeuristicManhattan(np, s.Grid.ToCoord(goal));
                        heap.InsertOrDecrease(new HeapNode(nextStateIdx, f));
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
            s.Heap.Dispose();
        }
    }
}
