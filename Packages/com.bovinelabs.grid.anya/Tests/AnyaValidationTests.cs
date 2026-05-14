using BovineLabs.Grid;
using BovineLabs.Grid.Anya;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

public unsafe partial class AnyaValidationTests
{
    private const double CostTolerance = 1e-5;

    private static NativeArray<byte> NewEmptyBlocked(int width, int height, Allocator allocator)
    {
        var blocked = new NativeArray<byte>(width * height, allocator);
        blocked.Fill((byte)0);
        return blocked;
    }

    private static void AssertPathValidAndVisible(
        Grid2D grid,
        NativeArray<byte> blocked,
        NativeList<int2> path,
        int2 start,
        int2 goal)
    {
        Assert.Greater(path.Length, 0, "Path must not be empty.");
        Assert.AreEqual(start, path[0], "Path must start at requested start.");
        Assert.AreEqual(goal, path[path.Length - 1], "Path must end at requested goal.");

        for (var i = 0; i < path.Length; i++)
        {
            var p = path[i];
            Assert.IsTrue(grid.InBounds(p), $"Waypoint {i} is out of bounds: {p}.");
            Assert.AreEqual(0, blocked[p.y * grid.Width + p.x], $"Waypoint {i} is blocked: {p}.");
        }

        var ptr = (byte*)blocked.GetUnsafeReadOnlyPtr();
        for (var i = 0; i < path.Length - 1; i++)
        {
            Assert.IsTrue(
                AnyaApi.LineOfSight(grid.Width, grid.Height, ptr, path[i], path[i + 1]),
                $"Path segment {i} has no line of sight: {path[i]} -> {path[i + 1]}.");
        }
    }

    private static double PathCost(NativeList<int2> path)
    {
        var cost = 0.0;
        for (var i = 0; i < path.Length - 1; i++)
        {
            var a = new double2(path[i].x, path[i].y);
            var b = new double2(path[i + 1].x, path[i + 1].y);
            cost += math.distance(a, b);
        }

        return cost;
    }

    private static double VisibilityGraphOracleCost(
        int width,
        int height,
        NativeArray<byte> blocked,
        int2 start,
        int2 goal)
    {
        var count = width * height;
        var startIndex = start.y * width + start.x;
        var goalIndex = goal.y * width + goal.x;

        if (start.x < 0 || start.y < 0 || start.x >= width || start.y >= height) return double.PositiveInfinity;
        if (goal.x < 0 || goal.y < 0 || goal.x >= width || goal.y >= height) return double.PositiveInfinity;
        if (blocked[startIndex] != 0 || blocked[goalIndex] != 0) return double.PositiveInfinity;

        var dist = new double[count];
        var used = new bool[count];

        for (var i = 0; i < count; i++)
            dist[i] = double.PositiveInfinity;

        dist[startIndex] = 0.0;

        var ptr = (byte*)blocked.GetUnsafeReadOnlyPtr();

        for (var iter = 0; iter < count; iter++)
        {
            var u = -1;
            var best = double.PositiveInfinity;

            for (var i = 0; i < count; i++)
            {
                if (!used[i] && blocked[i] == 0 && dist[i] < best)
                {
                    best = dist[i];
                    u = i;
                }
            }

            if (u < 0) break;
            if (u == goalIndex) return dist[u];

            used[u] = true;
            var ux = u % width;
            var uy = u / width;
            var up = new int2(ux, uy);

            for (var v = 0; v < count; v++)
            {
                if (used[v] || blocked[v] != 0) continue;

                var vx = v % width;
                var vy = v / width;
                var vp = new int2(vx, vy);

                if (!AnyaApi.LineOfSight(width, height, ptr, up, vp)) continue;

                var edge = math.distance(new double2(ux, uy), new double2(vx, vy));
                var candidate = dist[u] + edge;

                if (candidate < dist[v])
                    dist[v] = candidate;
            }
        }

        return dist[goalIndex];
    }
}
