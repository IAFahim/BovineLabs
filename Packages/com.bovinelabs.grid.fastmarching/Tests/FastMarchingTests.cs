using BovineLabs.Grid;
using BovineLabs.Grid.FastMarching;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

public class FastMarchingTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(FastMarchingApi.TryCreate(5, 5, Allocator.Temp, out var s));
        Assert.AreEqual(25, s.Grid.Length);
        FastMarchingApi.Dispose(ref s);
    }

    [Test]
    public unsafe void InitSources_SetsZero()
    {
        Assert.IsTrue(FastMarchingApi.TryCreate(5, 5, Allocator.Temp, out var s));
        var src = new NativeArray<int>(new[] { 12 }, Allocator.Temp);
        FastMarchingApi.InitializeSources(ref s, in src);
        Assert.AreEqual(0f, s.T[12], 0.001f);
        FastMarchingApi.Dispose(ref s);
        src.Dispose();
    }

    [Test]
    public unsafe void InitSources_OtherInf()
    {
        Assert.IsTrue(FastMarchingApi.TryCreate(5, 5, Allocator.Temp, out var s));
        var src = new NativeArray<int>(new[] { 0 }, Allocator.Temp);
        FastMarchingApi.InitializeSources(ref s, in src);
        Assert.IsTrue(float.IsPositiveInfinity(s.T[1]));
        FastMarchingApi.Dispose(ref s);
        src.Dispose();
    }

    [Test]
    public unsafe void Propagate_Line1D()
    {
        Assert.IsTrue(FastMarchingApi.TryCreate(5, 1, Allocator.Temp, out var s));
        var speed = new NativeArray<float>(5, Allocator.Temp);
        speed.Fill(1f);
        var src = new NativeArray<int>(new[] { 0 }, Allocator.Temp);
        FastMarchingApi.InitializeSources(ref s, in src);
        Assert.IsTrue(FastMarchingApi.TryPropagateAll(ref s, in speed));
        Assert.AreEqual(0f, s.T[0], 0.001f);
        Assert.AreEqual(1f, s.T[1], 0.001f);
        Assert.AreEqual(2f, s.T[2], 0.001f);
        Assert.AreEqual(4f, s.T[4], 0.001f);
        FastMarchingApi.Dispose(ref s);
        speed.Dispose();
        src.Dispose();
    }

    [Test]
    public unsafe void Propagate_2D()
    {
        Assert.IsTrue(FastMarchingApi.TryCreate(3, 3, Allocator.Temp, out var s));
        var speed = new NativeArray<float>(9, Allocator.Temp);
        speed.Fill(1f);
        var src = new NativeArray<int>(new[] { 4 }, Allocator.Temp);
        FastMarchingApi.InitializeSources(ref s, in src);
        Assert.IsTrue(FastMarchingApi.TryPropagateAll(ref s, in speed));
        Assert.AreEqual(0f, s.T[4], 0.001f);
        Assert.AreEqual(1f, s.T[s.Grid.ToIndex(1, 0)], 0.01f);
        Assert.AreEqual(1.707f, s.T[s.Grid.ToIndex(0, 0)], 0.05f);
        FastMarchingApi.Dispose(ref s);
        speed.Dispose();
        src.Dispose();
    }

    [Test]
    public unsafe void Propagate_SlowSpeed()
    {
        Assert.IsTrue(FastMarchingApi.TryCreate(5, 1, Allocator.Temp, out var s));
        var speed = new NativeArray<float>(5, Allocator.Temp);
        speed.Fill(0.5f);
        var src = new NativeArray<int>(new[] { 0 }, Allocator.Temp);
        FastMarchingApi.InitializeSources(ref s, in src);
        Assert.IsTrue(FastMarchingApi.TryPropagateAll(ref s, in speed));
        Assert.AreEqual(2f, s.T[1], 0.01f);
        FastMarchingApi.Dispose(ref s);
        speed.Dispose();
        src.Dispose();
    }

    [Test]
    public unsafe void GradientFlow_NotNull()
    {
        Assert.IsTrue(FastMarchingApi.TryCreate(5, 5, Allocator.Temp, out var s));
        var speed = new NativeArray<float>(25, Allocator.Temp);
        speed.Fill(1f);
        var src = new NativeArray<int>(new[] { 12 }, Allocator.Temp);
        var flow = new NativeArray<float2>(25, Allocator.Temp);
        FastMarchingApi.InitializeSources(ref s, in src);
        Assert.IsTrue(FastMarchingApi.TryPropagateAll(ref s, in speed));
        FastMarchingApi.BuildGradientFlow(ref s, ref flow);

        Assert.IsTrue(math.length(flow[12]) < 0.01f);
        FastMarchingApi.Dispose(ref s);
        speed.Dispose();
        src.Dispose();
        flow.Dispose();
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(FastMarchingApi.TryCreate(3, 3, Allocator.Temp, out var s));
        FastMarchingApi.Dispose(ref s);
        FastMarchingApi.Dispose(ref s);
    }
}