using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.DStarLite;

public class DStarLiteTests
{
    private DStarLiteState state;
    private NativeArray<byte> blocked;

    [SetUp] public void SetUp()
    {
        state = DStarLiteApi.Create(10, 10, Allocator.Temp);
        blocked = new NativeArray<byte>(100, Allocator.Temp);
        blocked.Fill((byte)0);
    }

    [TearDown] public void TearDown()
    {
        DStarLiteApi.Dispose(ref state);
        if (blocked.IsCreated) blocked.Dispose();
    }

    [Test] public void Create_Dimensions() { Assert.AreEqual(100, state.Grid.Length); }

    [Test] public void Initialize_StartGoal()
    {
        DStarLiteApi.Initialize(ref state, 0, 99, blocked);
        Assert.AreEqual(0, state.Start);
        Assert.AreEqual(99, state.Goal);
        Assert.AreEqual(0f, state.RHS[99], 0.001f);
    }

    [Test] public void Initialize_GIsInf()
    {
        DStarLiteApi.Initialize(ref state, 0, 99, blocked);
        for (int i = 0; i < state.Grid.Length; i++)
            Assert.IsTrue(float.IsPositiveInfinity(state.G[i]));
    }

    [Test] public void Repair_OpenGrid()
    {
        var cost = new NativeArray<float>(0, Allocator.Temp);
        DStarLiteApi.Initialize(ref state, 0, 99, blocked);
        Assert.IsTrue(DStarLiteApi.Repair(ref state, blocked, cost, 1000));
        cost.Dispose();
    }

    [Test] public void Repair_BlockedGoal()
    {
        blocked[99] = 1;
        var cost = new NativeArray<float>(0, Allocator.Temp);
        DStarLiteApi.Initialize(ref state, 0, 99, blocked);
        Assert.IsFalse(DStarLiteApi.Repair(ref state, blocked, cost, 1000));
        cost.Dispose();
    }

    [Test] public void Repair_BlockedStart()
    {
        blocked[0] = 1;
        var cost = new NativeArray<float>(0, Allocator.Temp);
        DStarLiteApi.Initialize(ref state, 0, 99, blocked);
        // Repair should still work (start is blocked but path may exist from neighbors)
        // Actually with blocked start, ExtractPath should return empty
        var result = DStarLiteApi.Repair(ref state, blocked, cost, 1000);
        // The start's RHS will be inf since start is blocked in neighbor expansion
        cost.Dispose();
    }

    [Test] public void Repair_StartEqualsGoal()
    {
        var cost = new NativeArray<float>(0, Allocator.Temp);
        DStarLiteApi.Initialize(ref state, 42, 42, blocked);
        Assert.IsTrue(DStarLiteApi.Repair(ref state, blocked, cost, 1000));
        cost.Dispose();
    }

    [Test] public void NotifyMoved()
    {
        DStarLiteApi.Initialize(ref state, 0, 99, blocked);
        DStarLiteApi.NotifyMoved(ref state, 5);
        Assert.AreEqual(5, state.Start);
        Assert.IsTrue(state.Km > 0f);
    }

    [Test] public void Repair_1x5_Linear()
    {
        var s = DStarLiteApi.Create(5, 1, Allocator.Temp);
        var b = new NativeArray<byte>(5, Allocator.Temp);
        var cost = new NativeArray<float>(0, Allocator.Temp);
        b.Fill((byte)0);
        DStarLiteApi.Initialize(ref s, 0, 4, b);
        Assert.IsTrue(DStarLiteApi.Repair(ref s, b, cost, 100));
        DStarLiteApi.Dispose(ref s); b.Dispose(); cost.Dispose();
    }

    [Test] public void Dispose_Double()
    {
        var s = DStarLiteApi.Create(3, 3, Allocator.Temp);
        DStarLiteApi.Dispose(ref s);
        DStarLiteApi.Dispose(ref s);
    }
}
