using BovineLabs.Grid.FieldDStar;
using NUnit.Framework;
using Unity.Collections;

public class FieldDStarTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(FieldDStarApi.TryCreate(5, 5, Allocator.Temp, out var s));
        Assert.AreEqual(25, s.Grid.Length);
        FieldDStarApi.Dispose(ref s);
    }

    [Test]
    public unsafe void SetGoal()
    {
        Assert.IsTrue(FieldDStarApi.TryCreate(5, 5, Allocator.Temp, out var s));
        Assert.IsTrue(FieldDStarApi.TrySetGoal(ref s, 24));
        Assert.AreEqual(0f, s.RHS[24], 0.001f);
        FieldDStarApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Step_OpenGrid()
    {
        Assert.IsTrue(FieldDStarApi.TryCreate(5, 5, Allocator.Temp, out var s));
        var cost = new NativeArray<float>(25, Allocator.Temp);
        for (var i = 0; i < 25; i++) cost[i] = 1f;
        Assert.IsTrue(FieldDStarApi.TrySetGoal(ref s, 24));
        Assert.IsTrue(FieldDStarApi.TryStep(ref s, cost));
        FieldDStarApi.Dispose(ref s);
        cost.Dispose();
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(FieldDStarApi.TryCreate(5, 5, Allocator.Temp, out var s));
        FieldDStarApi.Dispose(ref s);
        FieldDStarApi.Dispose(ref s);
    }
}