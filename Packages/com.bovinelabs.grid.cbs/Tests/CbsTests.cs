using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Cbs;

public class CbsTests
{
    [Test] public void Create_Dimensions()
    { var s = CbsApi.Create(10, 10, 100, Allocator.Temp); Assert.AreEqual(100, s.Grid.Length); CbsApi.Dispose(ref s); }

    [Test] public void Solve_TwoAgents_NoConflict()
    {
        var s = CbsApi.Create(5, 5, 100, Allocator.Temp);
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        for (int i = 0; i < 25; i++) blocked[i] = 0;
        var agents = new NativeArray<AgentTask>(2, Allocator.Temp);
        agents[0] = new AgentTask { Start = s.Grid.ToIndex(0, 0), Goal = s.Grid.ToIndex(0, 4) };
        agents[1] = new AgentTask { Start = s.Grid.ToIndex(4, 0), Goal = s.Grid.ToIndex(4, 4) };
        var paths = new NativeList<int>(Allocator.Temp);
        var lengths = new NativeList<int>(Allocator.Temp);
        Assert.IsTrue(CbsApi.Solve(ref s, blocked, agents, paths, lengths));
        Assert.AreEqual(2, lengths.Length);
        Assert.Greater(lengths[0], 0);
        Assert.Greater(lengths[1], 0);
        CbsApi.Dispose(ref s); blocked.Dispose(); agents.Dispose(); paths.Dispose(); lengths.Dispose();
    }

    [Test] public void AStar_SimplePath()
    {
        var s = CbsApi.Create(5, 5, 100, Allocator.Temp);
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        for (int i = 0; i < 25; i++) blocked[i] = 0;
        var path = new NativeList<int>(Allocator.Temp);
        var constraints = new NativeList<CbsConstraint>(Allocator.Temp);
        Assert.IsTrue(CbsApi.AStar(ref s, blocked, 0, 24, constraints, path));
        Assert.Greater(path.Length, 0);
        Assert.AreEqual(0, path[0]);
        Assert.AreEqual(24, path[path.Length - 1]);
        CbsApi.Dispose(ref s); blocked.Dispose(); path.Dispose(); constraints.Dispose();
    }

    [Test] public void Dispose_Double() { var s = CbsApi.Create(5, 5, 10, Allocator.Temp); CbsApi.Dispose(ref s); CbsApi.Dispose(ref s); }
}
