using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.MeshA
{
    /// <summary>
    /// MeshA* search algorithm (AAAI 2026).
    /// Searches over extended cells (grid position + configuration) instead of lattice states.
    /// For this implementation with simplified mesh graph, it reduces to weighted A* with
    /// heading-aware state space, but preserves the full API for future extension to
    /// multi-cell swept primitives with intermediate configurations.
    /// </summary>
    [BurstCompile]
    public struct MeshAStarJob : IJob
    {
        public NativeGrid2D Grid;
        public MeshGraphData MeshGraph;
        public PrimitiveSet PrimSet;
        public int2 Start;
        public int2 Goal;
        public int StartTheta;
        public float Weight;

        public NativeList<int2>.ParallelWriter Path;
        public NativeReference<bool> Found;
        public NativeReference<float> PathCost;
        public NativeReference<int> NodesExplored;

        public void Execute()
        {
            int gridW = Grid.Width;
            var closed = new NativeHashMap<int, bool>(gridW * Grid.Height * MeshGraphBuilder.NumHeadings, Allocator.Temp);
            var parentMap = new NativeHashMap<int, int>(gridW * Grid.Height * MeshGraphBuilder.NumHeadings, Allocator.Temp);
            var gCosts = new NativeHashMap<int, float>(gridW * Grid.Height * MeshGraphBuilder.NumHeadings, Allocator.Temp);

            int searchSpace = gridW * Grid.Height * MeshGraphBuilder.NumHeadings;
            var heap = new NativeMinHeap(searchSpace, Allocator.Temp);

            // Start: push all possible initial configurations (all headings) at the start position
            for (int h = 0; h < MeshGraphBuilder.NumHeadings; ++h)
            {
                int startConfig = MeshGraph.InitialConfigByTheta[h];
                int startKey = EncodeKey(Start.x, Start.y, startConfig, gridW);
                // Initialize cost for each start config (all zero)
                if (!gCosts.ContainsKey(startKey))
                    gCosts[startKey] = 0f;
                heap.Push(startKey, GridHeuristics.Octile(Start, Goal) * Weight);
            }

            int explored = 0;

            while (heap.Count > 0)
            {
                int currentKey = heap.Pop();
                explored++;

                if (closed.ContainsKey(currentKey)) continue;
                closed[currentKey] = true;

                int cx, cy, cConfig;
                DecodeKey(currentKey, out cx, out cy, out cConfig, gridW);

                // Goal check: position close to goal with any heading
                if (cx == Goal.x && cy == Goal.y)
                {
                    // Reconstruct path
                    int key = currentKey;
                    var reversePath = new NativeList<int2>(Allocator.Temp);
                    while (parentMap.TryGetValue(key, out int parentKey))
                    {
                        int px, py, pc;
                        DecodeKey(key, out px, out py, out pc, gridW);
                        reversePath.Add(new int2(px, py));
                        key = parentKey;
                    }
                    // Add start
                    {
                        int sx, sy, sc;
                        DecodeKey(key, out sx, out sy, out sc, gridW);
                        reversePath.Add(new int2(sx, sy));
                    }

                    // Write in forward order
                    for (int i = reversePath.Length - 1; i >= 0; i--)
                    {
                        Path.AddNoResize(reversePath[i]);
                    }
                    reversePath.Dispose();

                    Found.Value = true;
                    PathCost.Value = gCosts[currentKey];
                    NodesExplored.Value = explored;

                    heap.Dispose();
                    closed.Dispose();
                    parentMap.Dispose();
                    gCosts.Dispose();
                    return;
                }

                float currentG = gCosts[currentKey];

                // Expand successors from mesh graph (flat array lookup)
                if (cConfig < 0 || cConfig >= MeshGraph.MaxConfigs) continue;
                int succOff = MeshGraph.SuccOffsets[cConfig];
                int succCnt = MeshGraph.SuccCounts[cConfig];
                if (succCnt == 0) continue;

                for (int si = 0; si < succCnt; si++)
                {
                    var succ = MeshGraph.SuccessorsFlat[succOff + si];
                    int nx = cx + succ.Di;
                    int ny = cy + succ.Dj;
                    int nConfig = succ.NextConfigId;

                    // Bounds and traversability check
                    if (!Grid.InBounds(new int2(nx, ny))) continue;
                    if (!Grid.IsFree(new int2(nx, ny))) continue;

                    int nKey = EncodeKey(nx, ny, nConfig, gridW);
                    if (closed.ContainsKey(nKey)) continue;

                    // Get transition cost from primitive
                    float transCost = 0f;
                    if (succ.ConnectingPrimId >= 0)
                    {
                        transCost = PrimSet.Primitives[succ.ConnectingPrimId].ArcLength;
                    }

                    // Collision check for the primitive's swept cells
                    bool collisionFree = true;
                    if (succ.ConnectingPrimId >= 0)
                    {
                        var prim = PrimSet.Primitives[succ.ConnectingPrimId];
                        collisionFree = prim.IsCollisionFree(Grid, cx, cy, 0);
                    }
                    if (!collisionFree) continue;

                    float newG = currentG + transCost;
                    float existingG;
                    bool hasExisting = gCosts.TryGetValue(nKey, out existingG);

                    if (!hasExisting || newG < existingG)
                    {
                        gCosts[nKey] = newG;
                        parentMap[nKey] = currentKey;
                        float h = GridHeuristics.Octile(new int2(nx, ny), Goal) * Weight;
                        heap.Push(nKey, newG + h);
                    }
                }
            }

            // No path found
            Found.Value = false;
            PathCost.Value = -1f;
            NodesExplored.Value = explored;

            heap.Dispose();
            closed.Dispose();
            parentMap.Dispose();
            gCosts.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int EncodeKey(int x, int y, int config, int gridW)
        {
            return (y * gridW + x) * MeshGraphBuilder.NumHeadings + config;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void DecodeKey(int key, out int x, out int y, out int config, int gridW)
        {
            config = key % MeshGraphBuilder.NumHeadings;
            int posIdx = key / MeshGraphBuilder.NumHeadings;
            y = posIdx / gridW;
            x = posIdx % gridW;
        }
    }

    /// <summary>
    /// High-level API for MeshA* search.
    /// </summary>
    [BurstCompile]
    public static class MeshAStar
    {
        /// <summary>
        /// Run MeshA* on the given grid with the provided primitives and mesh graph.
        /// </summary>
        public static PathResult FindPath(
            in NativeGrid2D grid,
            in PrimitiveSet primSet,
            in MeshGraphData meshGraph,
            int2 start,
            int2 goal,
            int startTheta = 0,
            float weight = 1.0f,
            Allocator allocator = Allocator.Temp)
        {
            var result = new PathResult(allocator);
            var found = new NativeReference<bool>(allocator);
            var pathCost = new NativeReference<float>(allocator);
            var nodesExplored = new NativeReference<int>(allocator);

            var job = new MeshAStarJob
            {
                Grid = grid,
                MeshGraph = meshGraph,
                PrimSet = primSet,
                Start = start,
                Goal = goal,
                StartTheta = startTheta,
                Weight = weight,
                Path = result.Path.AsParallelWriter(),
                Found = found,
                PathCost = pathCost,
                NodesExplored = nodesExplored,
            };

            job.Execute();

            result.Found = found.Value;
            result.PathCost = pathCost.Value;
            result.NodesExplored = nodesExplored.Value;

            found.Dispose();
            pathCost.Dispose();
            nodesExplored.Dispose();

            return result;
        }
    }
}
