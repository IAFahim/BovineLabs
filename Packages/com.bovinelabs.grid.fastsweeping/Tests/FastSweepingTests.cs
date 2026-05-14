using BovineLabs.Grid.FastSweeping;
using NUnit.Framework;
using Unity.Collections;

public class FastSweepingTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(FastSweepingApi.TryCreate(5, 3, Allocator.Temp, out var s));
        Assert.AreEqual(15, s.Grid.Length);
        FastSweepingApi.Dispose(ref s);
    }

    [Test]
    public unsafe void InitSources_SetsZero()
    {
        Assert.IsTrue(FastSweepingApi.TryCreate(5, 1, Allocator.Temp, out var s));
        var src = new NativeArray<int>(new[] { 0 }, Allocator.Temp);
        Assert.IsTrue(FastSweepingApi.TryInitialize(ref s, src));
        Assert.AreEqual(0f, s.T[0], 0.001f);
        Assert.IsTrue(float.IsPositiveInfinity(s.T[1]));
        FastSweepingApi.Dispose(ref s);
        src.Dispose();
    }

    private static NativeArray<float> MakeSpeed(int len, float val, Allocator a)
    {
        var arr = new NativeArray<float>(len, a);
        for (var i = 0; i < len; i++) arr[i] = val;
        return arr;
    }

    [Test]
    public unsafe void Sweep_1D()
    {
        Assert.IsTrue(FastSweepingApi.TryCreate(5, 1, Allocator.Temp, out var s));
        var speed = MakeSpeed(5, 1f, Allocator.Temp);
        var src = new NativeArray<int>(new[] { 0 }, Allocator.Temp);
        Assert.IsTrue(FastSweepingApi.TryInitialize(ref s, src));
        Assert.IsTrue(FastSweepingApi.TrySweepAllDirections(ref s, speed, 3));
        Assert.AreEqual(1f, s.T[1], 0.01f);
        Assert.AreEqual(2f, s.T[2], 0.01f);
        Assert.AreEqual(4f, s.T[4], 0.01f);
        FastSweepingApi.Dispose(ref s);
        speed.Dispose();
        src.Dispose();
    }

    [Test]
    public unsafe void Sweep_2D_Center()
    {
        Assert.IsTrue(FastSweepingApi.TryCreate(3, 3, Allocator.Temp, out var s));
        var speed = MakeSpeed(9, 1f, Allocator.Temp);
        var src = new NativeArray<int>(new[] { 4 }, Allocator.Temp);
        Assert.IsTrue(FastSweepingApi.TryInitialize(ref s, src));
        Assert.IsTrue(FastSweepingApi.TrySweepAllDirections(ref s, speed, 5));
        Assert.AreEqual(0f, s.T[4], 0.001f);
        Assert.AreEqual(1f, s.T[1], 0.1f);
        Assert.AreEqual(1.707f, s.T[0], 0.05f);
        FastSweepingApi.Dispose(ref s);
        speed.Dispose();
        src.Dispose();
    }

    [Test]
    public unsafe void RelaxCell_Updates()
    {
        Assert.IsTrue(FastSweepingApi.TryCreate(5, 1, Allocator.Temp, out var s));
        var speed = MakeSpeed(5, 1f, Allocator.Temp);
        for (var i = 0; i < s.Grid.Length; i++) s.T[i] = float.PositiveInfinity;
        s.T[0] = 0f;
        Assert.IsTrue(FastSweepingApi.TryRelaxCell(s, speed, 1));
        Assert.AreEqual(1f, s.T[1], 0.01f);
        FastSweepingApi.Dispose(ref s);
        speed.Dispose();
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(FastSweepingApi.TryCreate(3, 3, Allocator.Temp, out var s));
        FastSweepingApi.Dispose(ref s);
        FastSweepingApi.Dispose(ref s);
    }
}