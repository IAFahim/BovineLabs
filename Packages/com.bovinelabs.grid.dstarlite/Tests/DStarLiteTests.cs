using BovineLabs.Grid;
using BovineLabs.Grid.DStarLite;
using NUnit.Framework;
using Unity.Collections;

public class DStarLiteTests
{
    private NativeArray<byte> blocked;
    private DStarLiteState state;

    [SetUp]
    public unsafe void SetUp()
    {
        Assert.IsTrue(DStarLiteApi.TryCreate(10, 10, Allocator.Temp, out var s));
        state = s;
        blocked = new NativeArray<byte>(100, Allocator.Temp);
        blocked.Fill((byte)0);
    }

    [TearDown]
    public unsafe void TearDown()
    {
        DStarLiteApi.Dispose(ref state);
        if (blocked.IsCreated) blocked.Dispose();
    }

    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.AreEqual(100, state.Grid.Length);
    }

    [Test]
    public unsafe void Initialize_StartGoal()
    {
        Assert.IsTrue(DStarLiteApi.TryInitialize(ref state, 0, 99, blocked));
        Assert.AreEqual(0, state.Start);
        Assert.AreEqual(99, state.Goal);
        Assert.AreEqual(0f, state.RHS[99], 0.001f);
    }

    [Test]
    public unsafe void Initialize_GIsInf()
    {
        Assert.IsTrue(DStarLiteApi.TryInitialize(ref state, 0, 99, blocked));
        for (var i = 0; i < state.Grid.Length; i++)
            Assert.IsTrue(float.IsPositiveInfinity(state.G[i]));
    }

    [Test]
    public unsafe void Repair_OpenGrid()
    {
        var cost = new NativeArray<float>(0, Allocator.Temp);
        Assert.IsTrue(DStarLiteApi.TryInitialize(ref state, 0, 99, blocked));
        Assert.IsTrue(DStarLiteApi.TryRepair(ref state, blocked, cost, 1000));
        cost.Dispose();
    }

    [Test]
    public unsafe void Repair_BlockedGoal()
    {
        blocked[99] = 1;
        var cost = new NativeArray<float>(0, Allocator.Temp);
        Assert.IsTrue(DStarLiteApi.TryInitialize(ref state, 0, 99, blocked));
        Assert.IsFalse(DStarLiteApi.TryRepair(ref state, blocked, cost, 1000));
        cost.Dispose();
    }

    [Test]
    public unsafe void Repair_BlockedStart()
    {
        blocked[0] = 1;
        var cost = new NativeArray<float>(0, Allocator.Temp);
        Assert.IsTrue(DStarLiteApi.TryInitialize(ref state, 0, 99, blocked));
        Assert.IsFalse(DStarLiteApi.TryRepair(ref state, blocked, cost, 1000));
        cost.Dispose();
    }

    [Test]
    public unsafe void Repair_StartEqualsGoal()
    {
        var cost = new NativeArray<float>(0, Allocator.Temp);
        Assert.IsTrue(DStarLiteApi.TryInitialize(ref state, 42, 42, blocked));
        Assert.IsTrue(DStarLiteApi.TryRepair(ref state, blocked, cost, 1000));
        cost.Dispose();
    }

    [Test]
    public unsafe void NotifyMoved()
    {
        Assert.IsTrue(DStarLiteApi.TryInitialize(ref state, 0, 99, blocked));
        DStarLiteApi.NotifyMoved(ref state, 5);
        Assert.AreEqual(5, state.Start);
        Assert.IsTrue(state.Km > 0f);
    }

    [Test]
    public unsafe void Repair_1x5_Linear()
    {
        Assert.IsTrue(DStarLiteApi.TryCreate(5, 1, Allocator.Temp, out var s));
        var b = new NativeArray<byte>(5, Allocator.Temp);
        var cost = new NativeArray<float>(0, Allocator.Temp);
        b.Fill((byte)0);
        Assert.IsTrue(DStarLiteApi.TryInitialize(ref s, 0, 4, b));
        Assert.IsTrue(DStarLiteApi.TryRepair(ref s, b, cost, 100));
        DStarLiteApi.Dispose(ref s);
        b.Dispose();
        cost.Dispose();
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(DStarLiteApi.TryCreate(3, 3, Allocator.Temp, out var s));
        DStarLiteApi.Dispose(ref s);
        DStarLiteApi.Dispose(ref s);
    }
}