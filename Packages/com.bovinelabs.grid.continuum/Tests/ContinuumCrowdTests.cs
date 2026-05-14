using BovineLabs.Grid;
using BovineLabs.Grid.Continuum;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

public class ContinuumCrowdTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(ContinuumCrowdApi.TryCreate(10, 10, Allocator.Temp, out var s));
        Assert.AreEqual(100, s.Grid.Length);
        ContinuumCrowdApi.Dispose(ref s);
    }

    [Test]
    public unsafe void ClearDensity()
    {
        Assert.IsTrue(ContinuumCrowdApi.TryCreate(5, 5, Allocator.Temp, out var s));
        s.Density[0] = 5f;
        ContinuumCrowdApi.ClearDensity(ref s);
        for (var i = 0; i < s.Grid.Length; i++) Assert.AreEqual(0f, s.Density[i], 0.001f);
        ContinuumCrowdApi.Dispose(ref s);
    }

    [Test]
    public unsafe void SplatAgents()
    {
        Assert.IsTrue(ContinuumCrowdApi.TryCreate(10, 10, Allocator.Temp, out var s));
        ContinuumCrowdApi.ClearDensity(ref s);
        var positions = new NativeArray<float2>(new[] { new float2(2.5f, 3.5f) }, Allocator.Temp);
        ContinuumCrowdApi.SplatAgents(ref s, positions);
        Assert.AreEqual(1f, s.Density[s.Grid.ToIndex(2, 3)], 0.001f);
        ContinuumCrowdApi.Dispose(ref s);
        positions.Dispose();
    }

    [Test]
    public unsafe void SolvePotential()
    {
        Assert.IsTrue(ContinuumCrowdApi.TryCreate(5, 5, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        blocked.Fill((byte)0);
        ContinuumCrowdApi.ClearDensity(ref s);
        Assert.IsTrue(ContinuumCrowdApi.TrySolvePotential(ref s, blocked, s.Grid.ToIndex(0, 0), 50));
        Assert.AreEqual(0f, s.Potential[s.Grid.ToIndex(0, 0)], 0.01f);
        Assert.Less(s.Potential[s.Grid.ToIndex(2, 2)], float.PositiveInfinity);
        ContinuumCrowdApi.Dispose(ref s);
        blocked.Dispose();
    }

    [Test]
    public unsafe void BuildFlow()
    {
        Assert.IsTrue(ContinuumCrowdApi.TryCreate(5, 5, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        blocked.Fill((byte)0);
        ContinuumCrowdApi.ClearDensity(ref s);
        Assert.IsTrue(ContinuumCrowdApi.TrySolvePotential(ref s, blocked, s.Grid.ToIndex(4, 4), 50));
        Assert.IsTrue(ContinuumCrowdApi.TryBuildFlow(ref s));

        var hasFlow = false;
        for (var i = 0; i < s.Grid.Length; i++)
            if (math.length(s.Flow[i]) > 0.01f)
                hasFlow = true;
        Assert.IsTrue(hasFlow);
        ContinuumCrowdApi.Dispose(ref s);
        blocked.Dispose();
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(ContinuumCrowdApi.TryCreate(3, 3, Allocator.Temp, out var s));
        ContinuumCrowdApi.Dispose(ref s);
        ContinuumCrowdApi.Dispose(ref s);
    }
}