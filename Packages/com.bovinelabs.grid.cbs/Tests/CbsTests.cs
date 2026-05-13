using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Cbs;

public class CbsTests
{
    [Test] public void Create_Dimensions()
    { Assert.IsTrue(CbsApi.TryCreate(10, 10, 100, Allocator.Temp, out var s)); Assert.AreEqual(100, s.Grid.Length); CbsApi.Dispose(ref s); }

    [Test] public void Solve_TwoAgents_NoConflict()
    {
        Assert.IsTrue(CbsApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        for (int i = 0; i < 25; i++) blocked[i] = 0;
        var agents = new NativeArray<AgentTask>(2, Allocator.Temp);
        agents[0] = new AgentTask { Start = s.Grid.ToIndex(0, 0), Goal = s.Grid.ToIndex(0, 4) };
        agents[1] = new AgentTask { Start = s.Grid.ToIndex(4, 0), Goal = s.Grid.ToIndex(4, 4) };
        var paths = new NativeList<int>(Allocator.Temp);
        var lengths = new NativeList<int>(Allocator.Temp);
        Assert.IsTrue( CbsApi.TrySolve(ref s, blocked, agents, ref paths, ref lengths));
        Assert.AreEqual(2, lengths.Length);
        Assert.Greater(lengths[0], 0);
        Assert.Greater(lengths[1], 0);
        CbsApi.Dispose(ref s); blocked.Dispose(); agents.Dispose(); paths.Dispose(); lengths.Dispose();
    }

    [Test] public unsafe void AStar_SimplePath()
    {
        Assert.IsTrue(CbsApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        for (int i = 0; i < 25; i++) blocked[i] = 0;
        var path = new NativeList<int>(Allocator.Temp);
        var constraints = new UnsafeList<CbsConstraint>(0, Allocator.Temp);
        byte* blockedPtr = (byte*)blocked.GetUnsafePtr();
        Assert.IsTrue(CbsApi.TryAStar(ref s, blockedPtr, 0, 0, 24, in constraints, ref path));
        Assert.Greater(path.Length, 0);
        Assert.AreEqual(0, path[0]);
        Assert.AreEqual(24, path[path.Length - 1]);
        CbsApi.Dispose(ref s); blocked.Dispose(); path.Dispose(); constraints.Dispose();
    }

    [Test] public void Dispose_Double() { Assert.IsTrue(CbsApi.TryCreate(5, 5, 10, Allocator.Temp, out var s)); CbsApi.Dispose(ref s); CbsApi.Dispose(ref s); }

    [Test] public void Solve_EdgeSwapConflict()
    {
        Assert.IsTrue(CbsApi.TryCreate(5, 5, 5000, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        blocked.Fill((byte)0);
        var agents = new NativeArray<AgentTask>(2, Allocator.Temp);
        agents[0] = new AgentTask { Start = s.Grid.ToIndex(0, 0), Goal = s.Grid.ToIndex(4, 4) };
        agents[1] = new AgentTask { Start = s.Grid.ToIndex(4, 0), Goal = s.Grid.ToIndex(0, 4) };
        var paths = new NativeList<int>(Allocator.Temp);
        var lengths = new NativeList<int>(Allocator.Temp);
        Assert.IsTrue(CbsApi.TrySolve(ref s, blocked, agents, ref paths, ref lengths));
        Assert.AreEqual(2, lengths.Length);
        Assert.Greater(lengths[0], 0);
        Assert.Greater(lengths[1], 0);
        CbsApi.Dispose(ref s); blocked.Dispose(); agents.Dispose(); paths.Dispose(); lengths.Dispose();
    }

    [Test] public void Solve_GoalWaitConflict()
    {
        Assert.IsTrue(CbsApi.TryCreate(5, 5, 5000, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        blocked.Fill((byte)0);
        var agents = new NativeArray<AgentTask>(2, Allocator.Temp);
        agents[0] = new AgentTask { Start = s.Grid.ToIndex(0, 0), Goal = s.Grid.ToIndex(2, 2) };
        agents[1] = new AgentTask { Start = s.Grid.ToIndex(4, 0), Goal = s.Grid.ToIndex(4, 4) };
        var paths = new NativeList<int>(Allocator.Temp);
        var lengths = new NativeList<int>(Allocator.Temp);
        Assert.IsTrue(CbsApi.TrySolve(ref s, blocked, agents, ref paths, ref lengths));
        Assert.AreEqual(2, lengths.Length);
        CbsApi.Dispose(ref s); blocked.Dispose(); agents.Dispose(); paths.Dispose(); lengths.Dispose();
    }

    [Test] public void Solve_MultiAgentBottleneck()
    {
        Assert.IsTrue(CbsApi.TryCreate(7, 5, 10000, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(35, Allocator.Temp);
        blocked.Fill((byte)0);
        for (int y = 0; y < 5; y++)
            if (y != 2) blocked[s.Grid.ToIndex(3, y)] = 1;
        var agents = new NativeArray<AgentTask>(2, Allocator.Temp);
        agents[0] = new AgentTask { Start = s.Grid.ToIndex(0, 1), Goal = s.Grid.ToIndex(5, 1) };
        agents[1] = new AgentTask { Start = s.Grid.ToIndex(0, 3), Goal = s.Grid.ToIndex(5, 3) };
        var paths = new NativeList<int>(Allocator.Temp);
        var lengths = new NativeList<int>(Allocator.Temp);
        Assert.IsTrue(CbsApi.TrySolve(ref s, blocked, agents, ref paths, ref lengths));
        Assert.AreEqual(2, lengths.Length);
        for (int a = 0; a < 2; a++)
        {
            Assert.Greater(lengths[a], 0, $"Agent {a} must have a path");
            int off = 0;
            for (int aa = 0; aa < a; aa++) off += lengths[aa];
            int startCell = paths[off];
            int goalCell = paths[off + lengths[a] - 1];
            Assert.AreEqual(agents[a].Start, startCell, $"Agent {a} path start");
            Assert.AreEqual(agents[a].Goal, goalCell, $"Agent {a} path goal");
        }
        CbsApi.Dispose(ref s); blocked.Dispose(); agents.Dispose(); paths.Dispose(); lengths.Dispose();
    }

    [Test] public unsafe void AStar_EdgeConstraint()
    {
        Assert.IsTrue(CbsApi.TryCreate(5, 5, 500, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        blocked.Fill((byte)0);
        var path = new NativeList<int>(Allocator.Temp);
        var constraints = new UnsafeList<CbsConstraint>(2, Allocator.Temp);
        // Constrain agent 0 from traversing cell 1→cell 6 at time 2 (edge constraint)
        constraints.Add(new CbsConstraint { Agent = 0, Cell = 6, CellFrom = 1, Time = 2 });
        byte* blockedPtr = (byte*)blocked.GetUnsafePtr();
        Assert.IsTrue(CbsApi.TryAStar(ref s, blockedPtr, 0, 0, 24, in constraints, ref path));
        Assert.Greater(path.Length, 0);
        Assert.AreEqual(0, path[0]);
        Assert.AreEqual(24, path[path.Length - 1]);
        // Verify the constrained edge is not used
        for (int i = 0; i < path.Length - 1; i++)
        {
            if (i + 1 == 2) // time=2 step
            {
                Assert.IsFalse(path[i] == 1 && path[i + 1] == 6,
                    "Path must not use constrained edge 1→6 at time 2");
            }
        }
        CbsApi.Dispose(ref s); blocked.Dispose(); path.Dispose(); constraints.Dispose();
    }
}
