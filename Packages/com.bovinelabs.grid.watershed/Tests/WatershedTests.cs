using BovineLabs.Grid;
using BovineLabs.Grid.Watershed;
using NUnit.Framework;
using Unity.Collections;

public class WatershedTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(WatershedApi.TryCreate(5, 5, Allocator.Temp, out var s));
        Assert.AreEqual(25, s.Grid.Length);
        WatershedApi.Dispose(ref s);
    }

    [Test]
    public unsafe void FindMinima_TwoMinima()
    {
        Assert.IsTrue(WatershedApi.TryCreate(5, 5, Allocator.Temp, out var s));
        var height = new NativeArray<float>(25, Allocator.Temp);
        height.Fill(10f);
        height[s.Grid.ToIndex(1, 1)] = 0f;
        height[s.Grid.ToIndex(3, 3)] = 0f;

        for (var i = 0; i < 25; i++)
            if (i != s.Grid.ToIndex(1, 1) && i != s.Grid.ToIndex(3, 3))
                height[i] = 10f;
        Assert.IsTrue(WatershedApi.TryFindMinima(ref s, in height, out var count));
        Assert.GreaterOrEqual(count, 1);
        WatershedApi.Dispose(ref s);
        height.Dispose();
    }

    [Test]
    public unsafe void FindMinima_SingleMinimum()
    {
        Assert.IsTrue(WatershedApi.TryCreate(3, 3, Allocator.Temp, out var s));
        var height = new NativeArray<float>(9, Allocator.Temp);
        height.Fill(5f);
        height[4] = 0f;
        Assert.IsTrue(WatershedApi.TryFindMinima(ref s, in height, out var count));
        Assert.AreEqual(1, count);
        Assert.AreEqual(0, s.Label[4]);
        WatershedApi.Dispose(ref s);
        height.Dispose();
    }

    [Test]
    public unsafe void Flood_TwoBasins()
    {
        Assert.IsTrue(WatershedApi.TryCreate(5, 5, Allocator.Temp, out var s));
        var height = new NativeArray<float>(25, Allocator.Temp);
        height.Fill(10f);
        height[s.Grid.ToIndex(0, 0)] = 0f;
        height[s.Grid.ToIndex(4, 4)] = 0f;
        Assert.IsTrue(WatershedApi.TryFindMinima(ref s, in height, out _));
        Assert.IsTrue(WatershedApi.TryFlood(ref s, in height));

        Assert.GreaterOrEqual(s.Label[0], 0);
        Assert.GreaterOrEqual(s.Label[24], 0);
        WatershedApi.Dispose(ref s);
        height.Dispose();
    }

    [Test]
    public unsafe void ExtractBoundaries()
    {
        Assert.IsTrue(WatershedApi.TryCreate(5, 5, Allocator.Temp, out var s));
        var height = new NativeArray<float>(25, Allocator.Temp);
        var boundary = new NativeArray<byte>(25, Allocator.Temp);
        height.Fill(10f);
        height[0] = 0f;
        height[24] = 0f;
        Assert.IsTrue(WatershedApi.TryFindMinima(ref s, in height, out _));
        Assert.IsTrue(WatershedApi.TryFlood(ref s, in height));
        Assert.IsTrue(WatershedApi.TryExtractBoundaries(ref s, ref boundary));

        var labeled = 0;
        for (var i = 0; i < 25; i++)
            if (s.Label[i] >= 0)
                labeled++;
        Assert.Greater(labeled, 0);
        WatershedApi.Dispose(ref s);
        height.Dispose();
        boundary.Dispose();
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(WatershedApi.TryCreate(3, 3, Allocator.Temp, out var s));
        WatershedApi.Dispose(ref s);
        WatershedApi.Dispose(ref s);
    }
}