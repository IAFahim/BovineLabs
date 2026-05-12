using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Kasteleyn;

public class KasteleynTests
{
    [Test] public void Create_Dimensions()
    { var s = KasteleynApi.Create(4, 4, 100, Allocator.Temp); Assert.AreEqual(16, s.Grid.Length); KasteleynApi.Dispose(ref s); }

    [Test] public void BuildPlanarGraph_2x2()
    {
        var s = KasteleynApi.Create(2, 2, 10, Allocator.Temp);
        var region = new NativeArray<byte>(4, Allocator.Temp); region.Fill((byte)1);
        KasteleynApi.SetRegion(ref s, region);
        KasteleynApi.BuildPlanarGraph(ref s);
        Assert.AreEqual(4, s.VertexCount);
        Assert.Greater(s.Edges.Length, 0);
        KasteleynApi.Dispose(ref s); region.Dispose();
    }

    [Test] public void OrientKasteleyn()
    {
        var s = KasteleynApi.Create(2, 2, 10, Allocator.Temp);
        var region = new NativeArray<byte>(4, Allocator.Temp); region.Fill((byte)1);
        KasteleynApi.SetRegion(ref s, region);
        KasteleynApi.BuildPlanarGraph(ref s);
        KasteleynApi.OrientKasteleyn(ref s);
        // Matrix should be skew-symmetric
        for (int i = 0; i < s.VertexCount; i++)
        {
            Assert.AreEqual(0.0, s.Matrix[i * s.VertexCount + i], 0.001);
            for (int j = 0; j < s.VertexCount; j++)
                Assert.AreEqual(-s.Matrix[i * s.VertexCount + j], s.Matrix[j * s.VertexCount + i], 0.001);
        }
        KasteleynApi.Dispose(ref s); region.Dispose();
    }

    [Test] public void Dispose_Double() { var s = KasteleynApi.Create(3, 3, 10, Allocator.Temp); KasteleynApi.Dispose(ref s); KasteleynApi.Dispose(ref s); }
}
