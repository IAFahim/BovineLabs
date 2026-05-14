using BovineLabs.Grid.DynamicCut;
using NUnit.Framework;
using Unity.Collections;

public class DynamicCutTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(DynamicCutApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        DynamicCutApi.Dispose(ref s);
    }

    [Test]
    public unsafe void EditUnary()
    {
        Assert.IsTrue(DynamicCutApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        DynamicCutApi.EditUnary(ref s, 12, 1, 0);
        Assert.AreEqual(1, s.DirtyNodes.Length);
        DynamicCutApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(DynamicCutApi.TryCreate(3, 3, 10, Allocator.Temp, out var s));
        DynamicCutApi.Dispose(ref s);
        DynamicCutApi.Dispose(ref s);
    }
}