using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Thinning;

public class ThinningTests
{
    [Test] public void Create_Dimensions()
    { var s = ThinningApi.Create(5, 5, Allocator.Temp); Assert.AreEqual(25, s.Grid.Length); ThinningApi.Dispose(ref s); }

    [Test] public void Iterate_RemovesBorder()
    {
        var s = ThinningApi.Create(5, 5, Allocator.Temp);
        var solid = new NativeArray<byte>(25, Allocator.Temp);
        solid.Fill((byte)1);
        int removed = ThinningApi.Iterate(ref s, ref solid);
        Assert.Greater(removed, 0);
        ThinningApi.Dispose(ref s); solid.Dispose();
    }

    [Test] public void Dispose_Double() { var s = ThinningApi.Create(3, 3, Allocator.Temp); ThinningApi.Dispose(ref s); ThinningApi.Dispose(ref s); }
}
