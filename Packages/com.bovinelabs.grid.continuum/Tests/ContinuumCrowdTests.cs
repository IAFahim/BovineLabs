using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Continuum;

public class ContinuumCrowdTests
{
    [Test] public void Create_Dimensions()
    { var s = ContinuumCrowdApi.Create(10, 10, Allocator.Temp); Assert.AreEqual(100, s.Grid.Length); ContinuumCrowdApi.Dispose(ref s); }

    [Test] public void ClearDensity()
    {
        var s = ContinuumCrowdApi.Create(5, 5, Allocator.Temp);
        s.Density[0] = 5f;
        ContinuumCrowdApi.ClearDensity(ref s);
        for (int i = 0; i < s.Grid.Length; i++) Assert.AreEqual(0f, s.Density[i], 0.001f);
        ContinuumCrowdApi.Dispose(ref s);
    }

    [Test] public void SplatAgents()
    {
        var s = ContinuumCrowdApi.Create(10, 10, Allocator.Temp);
        ContinuumCrowdApi.ClearDensity(ref s);
        var positions = new NativeArray<float2>(new float2[] { new float2(2.5f, 3.5f) }, Allocator.Temp);
        ContinuumCrowdApi.SplatAgents(ref s, positions);
        Assert.AreEqual(1f, s.Density[s.Grid.ToIndex(2, 3)], 0.001f);
        ContinuumCrowdApi.Dispose(ref s); positions.Dispose();
    }

    [Test] public void SolvePotential()
    {
        var s = ContinuumCrowdApi.Create(5, 5, Allocator.Temp);
        var blocked = new NativeArray<byte>(25, Allocator.Temp); blocked.Fill((byte)0);
        ContinuumCrowdApi.ClearDensity(ref s);
        ContinuumCrowdApi.SolvePotential(ref s, blocked, s.Grid.ToIndex(0, 0), 50);
        Assert.AreEqual(0f, s.Potential[s.Grid.ToIndex(0, 0)], 0.01f);
        Assert.Less(s.Potential[s.Grid.ToIndex(2, 2)], float.PositiveInfinity);
        ContinuumCrowdApi.Dispose(ref s); blocked.Dispose();
    }

    [Test] public void BuildFlow()
    {
        var s = ContinuumCrowdApi.Create(5, 5, Allocator.Temp);
        var blocked = new NativeArray<byte>(25, Allocator.Temp); blocked.Fill((byte)0);
        ContinuumCrowdApi.ClearDensity(ref s);
        ContinuumCrowdApi.SolvePotential(ref s, blocked, s.Grid.ToIndex(4, 4), 50);
        ContinuumCrowdApi.BuildFlow(ref s);
        // Flow should exist somewhere
        bool hasFlow = false;
        for (int i = 0; i < s.Grid.Length; i++) if (math.length(s.Flow[i]) > 0.01f) hasFlow = true;
        Assert.IsTrue(hasFlow);
        ContinuumCrowdApi.Dispose(ref s); blocked.Dispose();
    }

    [Test] public void Dispose_Double() { var s = ContinuumCrowdApi.Create(3, 3, Allocator.Temp); ContinuumCrowdApi.Dispose(ref s); ContinuumCrowdApi.Dispose(ref s); }
}
