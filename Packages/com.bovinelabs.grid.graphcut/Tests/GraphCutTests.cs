using NUnit.Framework;
using Unity.Collections;
using BovineLabs.GraphCut;

public class GraphCutTests
{
    static NativeArray<int> MakeIntArray(int len, int val, Allocator a)
    {
        var arr = new NativeArray<int>(len, a);
        for (int i = 0; i < len; i++) arr[i] = val;
        return arr;
    }

    [Test] public void Create_Dimensions()
    { var s = GraphCutApi.Create(5, 5, 100, Allocator.Temp); Assert.AreEqual(25, s.Grid.Length); GraphCutApi.Dispose(ref s); }

    [Test] public void BuildBinaryEnergy()
    {
        var s = GraphCutApi.Create(3, 3, 100, Allocator.Temp);
        var u0 = MakeIntArray(9, 1, Allocator.Temp);
        var u1 = MakeIntArray(9, 1, Allocator.Temp);
        var pw = MakeIntArray(9, 1, Allocator.Temp);
        GraphCutApi.BuildBinaryEnergy(ref s, u0, u1, pw);
        Assert.Greater(s.EdgeTo.Length, 0);
        GraphCutApi.Dispose(ref s); u0.Dispose(); u1.Dispose(); pw.Dispose();
    }

    [Test] public void Dispose_Double() { var s = GraphCutApi.Create(3, 3, 10, Allocator.Temp); GraphCutApi.Dispose(ref s); GraphCutApi.Dispose(ref s); }
}
