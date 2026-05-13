using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.FastMarching;

public class FastMarchingTests
{
    [Test] public void Create_Dimensions()
    { var s = FastMarchingApi.Create(5, 5, Allocator.Temp); Assert.AreEqual(25, s.Grid.Length); FastMarchingApi.Dispose(ref s); }

    [Test] public void InitSources_SetsZero()
    {
        var s = FastMarchingApi.Create(5, 5, Allocator.Temp);
        var src = new NativeArray<int>(new int[] { 12 }, Allocator.Temp);
        FastMarchingApi.InitializeSources(ref s, in src);
        Assert.AreEqual(0f, s.T[12], 0.001f);
        FastMarchingApi.Dispose(ref s); src.Dispose();
    }

    [Test] public void InitSources_OtherInf()
    {
        var s = FastMarchingApi.Create(5, 5, Allocator.Temp);
        var src = new NativeArray<int>(new int[] { 0 }, Allocator.Temp);
        FastMarchingApi.InitializeSources(ref s, in src);
        Assert.IsTrue(float.IsPositiveInfinity(s.T[1]));
        FastMarchingApi.Dispose(ref s); src.Dispose();
    }

    [Test] public void Propagate_Line1D()
    {
        var s = FastMarchingApi.Create(5, 1, Allocator.Temp);
        var speed = new NativeArray<float>(5, Allocator.Temp); speed.Fill(1f);
        var src = new NativeArray<int>(new int[] { 0 }, Allocator.Temp);
        FastMarchingApi.InitializeSources(ref s, in src);
        FastMarchingApi.PropagateAll(ref s, in speed);
        Assert.AreEqual(0f, s.T[0], 0.001f);
        Assert.AreEqual(1f, s.T[1], 0.001f);
        Assert.AreEqual(2f, s.T[2], 0.001f);
        Assert.AreEqual(4f, s.T[4], 0.001f);
        FastMarchingApi.Dispose(ref s); speed.Dispose(); src.Dispose();
    }

    [Test] public void Propagate_2D()
    {
        var s = FastMarchingApi.Create(3, 3, Allocator.Temp);
        var speed = new NativeArray<float>(9, Allocator.Temp); speed.Fill(1f);
        var src = new NativeArray<int>(new int[] { 4 }, Allocator.Temp);
        FastMarchingApi.InitializeSources(ref s, in src);
        FastMarchingApi.PropagateAll(ref s, in speed);
        Assert.AreEqual(0f, s.T[4], 0.001f);
        Assert.AreEqual(1f, s.T[s.Grid.ToIndex(1, 0)], 0.01f);
        Assert.AreEqual(1.707f, s.T[s.Grid.ToIndex(0, 0)], 0.05f);
        FastMarchingApi.Dispose(ref s); speed.Dispose(); src.Dispose();
    }

    [Test] public void Propagate_SlowSpeed()
    {
        var s = FastMarchingApi.Create(5, 1, Allocator.Temp);
        var speed = new NativeArray<float>(5, Allocator.Temp); speed.Fill(0.5f);
        var src = new NativeArray<int>(new int[] { 0 }, Allocator.Temp);
        FastMarchingApi.InitializeSources(ref s, in src);
        FastMarchingApi.PropagateAll(ref s, in speed);
        Assert.AreEqual(2f, s.T[1], 0.01f);
        FastMarchingApi.Dispose(ref s); speed.Dispose(); src.Dispose();
    }

    [Test] public void GradientFlow_NotNull()
    {
        var s = FastMarchingApi.Create(5, 5, Allocator.Temp);
        var speed = new NativeArray<float>(25, Allocator.Temp); speed.Fill(1f);
        var src = new NativeArray<int>(new int[] { 12 }, Allocator.Temp);
        var flow = new NativeArray<float2>(25, Allocator.Temp);
        FastMarchingApi.InitializeSources(ref s, in src);
        FastMarchingApi.PropagateAll(ref s, in speed);
        FastMarchingApi.BuildGradientFlow(ref s, ref flow);
        // Flow at center should be near zero (at source)
        Assert.IsTrue(math.length(flow[12]) < 0.01f);
        FastMarchingApi.Dispose(ref s); speed.Dispose(); src.Dispose(); flow.Dispose();
    }

    [Test] public void Dispose_Double() { var s = FastMarchingApi.Create(3, 3, Allocator.Temp); FastMarchingApi.Dispose(ref s); FastMarchingApi.Dispose(ref s); }
}
