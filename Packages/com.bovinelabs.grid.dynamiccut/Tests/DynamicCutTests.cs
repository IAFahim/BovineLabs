using NUnit.Framework;
using Unity.Collections;
using BovineLabs.Grid.DynamicCut;

public class DynamicCutTests
{
    [Test] public void Create_Dimensions()
    { var s = DynamicCutApi.Create(5, 5, 100, Allocator.Temp); DynamicCutApi.Dispose(ref s); }

    [Test] public void EditUnary()
    {
        var s = DynamicCutApi.Create(5, 5, 100, Allocator.Temp);
        DynamicCutApi.EditUnary(ref s, 12, 1, 0);
        Assert.AreEqual(1, s.DirtyNodes.Length);
        DynamicCutApi.Dispose(ref s);
    }

    [Test] public void Dispose_Double() { var s = DynamicCutApi.Create(3, 3, 10, Allocator.Temp); DynamicCutApi.Dispose(ref s); DynamicCutApi.Dispose(ref s); }
}
