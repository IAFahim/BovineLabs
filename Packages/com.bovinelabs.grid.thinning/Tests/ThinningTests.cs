using BovineLabs.Grid;
using BovineLabs.Grid.Thinning;
using NUnit.Framework;
using Unity.Collections;

public class ThinningTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(ThinningApi.TryCreate(5, 5, Allocator.Temp, out var s));
        Assert.AreEqual(25, s.Grid.Length);
        ThinningApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Iterate_RemovesBorder()
    {
        Assert.IsTrue(ThinningApi.TryCreate(5, 5, Allocator.Temp, out var s));
        var solid = new NativeArray<byte>(25, Allocator.Temp);
        solid.Fill((byte)1);
        Assert.IsTrue(ThinningApi.TryIterate(ref s, ref solid, out var removed));
        Assert.Greater(removed, 0);
        ThinningApi.Dispose(ref s);
        solid.Dispose();
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(ThinningApi.TryCreate(3, 3, Allocator.Temp, out var s));
        ThinningApi.Dispose(ref s);
        ThinningApi.Dispose(ref s);
    }
}