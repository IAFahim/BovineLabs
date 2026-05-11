using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Cbs
{
    public struct AgentTask { public int Start; public int Goal; }

    public struct CbsConstraint { public int Agent; public int Cell; public int Time; }

    public struct CbsNode { public int ConstraintOffset; public int ConstraintCount; public float Cost; }

    public struct CbsState
    {
        public Grid2D Grid;
        public NativeList<CbsNode> Nodes;
        public NativeList<CbsConstraint> Constraints;
        public NativeList<int> FlatPaths;      // all paths concatenated
        public NativeList<int> PathLengths;    // length of each agent's path
        public MinHeap Heap;
    }

    public static class CbsApi
    {
        public static CbsState Create(int width, int height, int maxNodes, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new CbsState
            {
                Grid = g,
                Nodes = new NativeList<CbsNode>(maxNodes, a),
                Constraints = new NativeList<CbsConstraint>(maxNodes * 10, a),
                FlatPaths = new NativeList<int>(maxNodes * 20, a),
                PathLengths = new NativeList<int>(maxNodes, a),
                Heap = MinHeap.Create(maxNodes, a),
            };
        }

        public static bool Solve(
            ref CbsState s,
            NativeArray<byte> blocked,
            NativeArray<AgentTask> agents,
            NativeList<int> flatPaths,
            NativeList<int> pathLengths)
        {
            s.Nodes.Clear();
            s.Constraints.Clear();
            s.FlatPaths.Clear();
            s.PathLengths.Clear();
            s.Heap.Clear();
            flatPaths.Clear();
            pathLengths.Clear();

            int agentCount = agents.Length;
            if (agentCount == 0) return true;

            // Plan each agent independently
            float totalCost = 0;
            bool allFound = true;
            var agentPaths = new NativeList<int>[agentCount];

            for (int a = 0; a < agentCount; a++)
            {
                var path = new NativeList<int>(Allocator.Temp);
                var constraints = new NativeList<CbsConstraint>(Allocator.Temp);
                if (!AStar(ref s, blocked, agents[a].Start, agents[a].Goal, constraints, path))
                {
                    allFound = false;
                    path.Dispose();
                    constraints.Dispose();
                    // Clean up already allocated
                    for (int j = 0; j < a; j++) agentPaths[j].Dispose();
                    constraints.Dispose();
                    return false;
                }
                constraints.Dispose();
                agentPaths[a] = path;
                totalCost += path.Length - 1;
            }

            // Check for vertex conflicts
            int conflictAgentA = -1, conflictAgentB = -1, conflictCell = -1, conflictTime = -1;
            for (int a = 0; a < agentCount && conflictAgentA < 0; a++)
            {
                for (int b = a + 1; b < agentCount && conflictAgentA < 0; b++)
                {
                    int maxT = math.min(agentPaths[a].Length, agentPaths[b].Length);
                    for (int t = 0; t < maxT; t++)
                    {
                        if (agentPaths[a][t] == agentPaths[b][t])
                        {
                            conflictAgentA = a;
                            conflictAgentB = b;
                            conflictCell = agentPaths[a][t];
                            conflictTime = t;
                            break;
                        }
                    }
                }
            }

            if (conflictAgentA < 0)
            {
                // No conflicts — copy paths to output
                for (int a = 0; a < agentCount; a++)
                {
                    for (int i = 0; i < agentPaths[a].Length; i++)
                        flatPaths.Add(agentPaths[a][i]);
                    pathLengths.Add(agentPaths[a].Length);
                    agentPaths[a].Dispose();
                }
                return true;
            }

            // Simple conflict resolution: replan one conflicting agent with constraint
            // Replan agent B avoiding the conflict cell at the conflict time
            for (int a = 0; a < agentCount; a++)
            {
                var path = agentPaths[a];
                if (a == conflictAgentB)
                {
                    // Replan with constraint
                    var newPath = new NativeList<int>(Allocator.Temp);
                    var constraints = new NativeList<CbsConstraint>(Allocator.Temp);
                    constraints.Add(new CbsConstraint { Agent = a, Cell = conflictCell, Time = conflictTime });
                    if (!AStar(ref s, blocked, agents[a].Start, agents[a].Goal, constraints, newPath))
                    {
                        // Can't resolve — use original
                        for (int i = 0; i < path.Length; i++) flatPaths.Add(path[i]);
                        pathLengths.Add(path.Length);
                    }
                    else
                    {
                        for (int i = 0; i < newPath.Length; i++) flatPaths.Add(newPath[i]);
                        pathLengths.Add(newPath.Length);
                        newPath.Dispose();
                    }
                    constraints.Dispose();
                }
                else
                {
                    for (int i = 0; i < path.Length; i++) flatPaths.Add(path[i]);
                    pathLengths.Add(path.Length);
                }
                path.Dispose();
            }

            return true;
        }

        public static bool AStar(ref CbsState s, NativeArray<byte> blocked, int start, int goal,
            NativeList<CbsConstraint> constraints, NativeList<int> path)
        {
            path.Clear();
            if (blocked[start] != 0 || blocked[goal] != 0) return false;

            var g = new NativeArray<float>(s.Grid.Length, Allocator.Temp);
            var parent = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
            var closed = new NativeArray<byte>(s.Grid.Length, Allocator.Temp);
            g.Fill(float.PositiveInfinity);
            parent.Fill(-1);
            closed.Fill((byte)0);

            g[start] = 0f;
            var heap = MinHeap.Create(s.Grid.Length, Allocator.Temp);
            heap.InsertOrDecrease(new HeapNode(start, Grid2D.HeuristicManhattan(s.Grid.ToCoord(start), s.Grid.ToCoord(goal))));

            while (!heap.IsEmpty)
            {
                int u = heap.Pop().Id;
                if (u == goal)
                {
                    int cur = goal;
                    while (cur >= 0) { path.Add(cur); cur = parent[cur]; }
                    for (int i = 0; i < path.Length / 2; i++)
                    { int tmp = path[i]; path[i] = path[path.Length - 1 - i]; path[path.Length - 1 - i] = tmp; }
                    break;
                }

                closed[u] = 1;
                int2 up = s.Grid.ToCoord(u);

                for (int d = 0; d < 4; d++)
                {
                    int2 np = up + Grid2D.Directions4[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    if (blocked[ni] != 0 || closed[ni] != 0) continue;

                    bool constrained = false;
                    for (int c = 0; c < constraints.Length; c++)
                    {
                        if (constraints[c].Cell == ni && constraints[c].Time == (int)g[u] + 1)
                        { constrained = true; break; }
                    }
                    if (constrained) continue;

                    float newG = g[u] + 1f;
                    if (newG < g[ni])
                    {
                        g[ni] = newG;
                        parent[ni] = u;
                        float f = newG + Grid2D.HeuristicManhattan(s.Grid.ToCoord(ni), s.Grid.ToCoord(goal));
                        heap.InsertOrDecrease(new HeapNode(ni, f));
                    }
                }
            }

            g.Dispose(); parent.Dispose(); closed.Dispose(); heap.Dispose();
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
