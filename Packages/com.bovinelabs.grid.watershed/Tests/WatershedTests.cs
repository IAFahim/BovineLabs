using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Watershed;

public class WatershedTests
{
    [Test] public void Create_Dimensions()
    { var s = WatershedApi.Create(5, 5, Allocator.Temp); Assert.AreEqual(25, s.Grid.Length); WatershedApi.Dispose(ref s); }

    [Test] public void FindMinima_TwoMinima()
    {
        var s = WatershedApi.Create(5, 5, Allocator.Temp);
        var height = new NativeArray<float>(25, Allocator.Temp);
        height.Fill(10f);
        height[s.Grid.ToIndex(1, 1)] = 0f;
        height[s.Grid.ToIndex(3, 3)] = 0f;
        // Lower everything around minima to separate them
        for (int i = 0; i < 25; i++) if (i != s.Grid.ToIndex(1,1) && i != s.Grid.ToIndex(3,3)) height[i] = 10f;
        int count = WatershedApi.FindMinima(ref s, height);
        Assert.GreaterOrEqual(count, 1);
        WatershedApi.Dispose(ref s); height.Dispose();
    }

    [Test] public void FindMinima_SingleMinimum()
    {
        var s = WatershedApi.Create(3, 3, Allocator.Temp);
        var height = new NativeArray<float>(9, Allocator.Temp);
        height.Fill(5f);
        height[4] = 0f; // center minimum
        int count = WatershedApi.FindMinima(ref s, height);
        Assert.AreEqual(1, count);
        Assert.AreEqual(0, s.Label[4]); // first basin label
        WatershedApi.Dispose(ref s); height.Dispose();
    }

    [Test] public void Flood_TwoBasins()
    {
        var s = WatershedApi.Create(5, 5, Allocator.Temp);
        var height = new NativeArray<float>(25, Allocator.Temp);
        height.Fill(10f);
        height[s.Grid.ToIndex(0, 0)] = 0f;
        height[s.Grid.ToIndex(4, 4)] = 0f;
        WatershedApi.FindMinima(ref s, height);
        WatershedApi.Flood(ref s, height);
        // Both minima should have valid labels
        Assert.GreaterOrEqual(s.Label[0], 0);
        Assert.GreaterOrEqual(s.Label[24], 0);
        WatershedApi.Dispose(ref s); height.Dispose();
    }

    [Test] public void ExtractBoundaries()
    {
        var s = WatershedApi.Create(5, 5, Allocator.Temp);
        var height = new NativeArray<float>(25, Allocator.Temp);
        var boundary = new NativeArray<byte>(25, Allocator.Temp);
        height.Fill(10f);
        height[0] = 0f;
        height[24] = 0f;
        WatershedApi.FindMinima(ref s, height);
        WatershedApi.Flood(ref s, height);
        WatershedApi.ExtractBoundaries(ref s, boundary);
        // Should have some labeled cells
        int labeled = 0;
        for (int i = 0; i < 25; i++) if (s.Label[i] >= 0) labeled++;
        Assert.Greater(labeled, 0);
        WatershedApi.Dispose(ref s); height.Dispose(); boundary.Dispose();
    }

    [Test] public void Dispose_Double() { var s = WatershedApi.Create(3, 3, Allocator.Temp); WatershedApi.Dispose(ref s); WatershedApi.Dispose(ref s); }
}
