using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Watershed
{
    public struct WatershedState
    {
        public Grid2D Grid;
        public NativeArray<int> Label;
        public NativeArray<byte> State; // 0 unvisited, 1 in queue, 2 done
        public MinHeap Heap;
    }

    public static class WatershedApi
    {
        public static WatershedState Create(int width, int height, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new WatershedState
            {
                Grid = g,
                Label = new NativeArray<int>(g.Length, a),
                State = new NativeArray<byte>(g.Length, a),
                Heap = MinHeap.Create(g.Length, a),
            };
        }

        public static int FindMinima(ref WatershedState s, NativeArray<float> height)
        {
            s.Label.Fill(-1);
            s.State.Fill((byte)0);
            s.Heap.Clear();

            int labelCount = 0;
            var stack = new NativeList<int>(s.Grid.Length, Allocator.Temp);

            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (s.State[i] != 0) continue;
                if (height[i] <= 0f) continue; // skip blocked/zero

                // Check if local minimum
                int2 p = s.Grid.ToCoord(i);
                bool isMin = true;
                for (int d = 0; d < 4; d++)
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (s.Grid.InBounds(np) && height[s.Grid.ToIndex(np)] < height[i])
                    { isMin = false; break; }
                }

                if (!isMin) continue;

                // Flood-fill plateau
                stack.Clear();
                stack.Add(i);
                s.State[i] = 2;
                float h = height[i];

                for (int si = 0; si < stack.Length; si++)
                {
                    int ci = stack[si];
                    int2 cp = s.Grid.ToCoord(ci);
                    for (int d = 0; d < 4; d++)
                    {
                        int2 np = cp + Grid2D.Directions4[d];
                        if (!s.Grid.InBounds(np)) continue;
                        int ni = s.Grid.ToIndex(np);
                        if (s.State[ni] != 0) continue;
                        if (math.abs(height[ni] - h) < 0.0001f)
                        {
                            s.State[ni] = 2;
                            stack.Add(ni);
                        }
                    }
                }

                // Assign label
                int label = labelCount++;
                for (int si = 0; si < stack.Length; si++)
                    s.Label[stack[si]] = label;

                // Push boundary neighbors into heap
                for (int si = 0; si < stack.Length; si++)
                {
                    int2 cp = s.Grid.ToCoord(stack[si]);
                    for (int d = 0; d < 4; d++)
                    {
                        int2 np = cp + Grid2D.Directions4[d];
                        if (!s.Grid.InBounds(np)) continue;
                        int ni = s.Grid.ToIndex(np);
                        if (s.State[ni] == 0 && s.Label[ni] == -1)
                        {
                            s.State[ni] = 1;
                            s.Label[ni] = label;
                            s.Heap.InsertOrDecrease(new HeapNode(ni, height[ni]));
                        }
                    }
                }
            }

            stack.Dispose();
            return labelCount;
        }

        public static void Flood(ref WatershedState s, NativeArray<float> height)
        {
            while (!s.Heap.IsEmpty)
            {
                int u = s.Heap.Pop().Id;
                s.State[u] = 2;

                int2 up = s.Grid.ToCoord(u);
                for (int d = 0; d < 4; d++)
                {
                    int2 np = up + Grid2D.Directions4[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    if (s.State[ni] == 2) continue;

                    if (s.Label[ni] == -1)
                        s.Label[ni] = s.Label[u];
                    else if (s.Label[ni] != s.Label[u])
                        s.Label[ni] = -2; // watershed line

                    if (s.State[ni] == 0)
                    {
                        s.State[ni] = 1;
                        s.Heap.InsertOrDecrease(new HeapNode(ni, height[ni]));
                    }
                }
            }
        }

        public static void ExtractBoundaries(ref WatershedState s, NativeArray<byte> boundary)
        {
            boundary.Fill((byte)0);
            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (s.Label[i] == -2) { boundary[i] = 1; continue; }
                if (s.Label[i] < 0) continue;

                int2 p = s.Grid.ToCoord(i);
                for (int d = 0; d < 4; d++)
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    if (s.Label[ni] >= 0 && s.Label[ni] != s.Label[i])
                    { boundary[i] = 1; break; }
                }
            }
        }

        public static void Dispose(ref WatershedState s)
        {
            if (s.Label.IsCreated) s.Label.Dispose();
            if (s.State.IsCreated) s.State.Dispose();
            if (s.Heap.IsCreated) s.Heap.Dispose();
        }
    }
}
