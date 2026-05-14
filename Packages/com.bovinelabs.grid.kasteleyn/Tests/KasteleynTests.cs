using BovineLabs.Grid;
using BovineLabs.Grid.Kasteleyn;
using NUnit.Framework;
using Unity.Collections;

public class KasteleynTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(KasteleynApi.TryCreate(4, 4, 100, Allocator.Temp, out var s));
        Assert.AreEqual(16, s.Grid.Length);
        KasteleynApi.Dispose(ref s);
    }

    [Test]
    public unsafe void BuildPlanarGraph_2x2()
    {
        Assert.IsTrue(KasteleynApi.TryCreate(2, 2, 10, Allocator.Temp, out var s));
        var region = new NativeArray<byte>(4, Allocator.Temp);
        region.Fill((byte)1);
        KasteleynApi.SetRegion(ref s, region);
        Assert.IsTrue(KasteleynApi.TryBuildPlanarGraph(ref s));
        Assert.AreEqual(4, s.VertexCount);
        Assert.Greater(s.Edges.Length, 0);
        KasteleynApi.Dispose(ref s);
        region.Dispose();
    }

    [Test]
    public unsafe void OrientKasteleyn()
    {
        Assert.IsTrue(KasteleynApi.TryCreate(2, 2, 10, Allocator.Temp, out var s));
        var region = new NativeArray<byte>(4, Allocator.Temp);
        region.Fill((byte)1);
        KasteleynApi.SetRegion(ref s, region);
        Assert.IsTrue(KasteleynApi.TryBuildPlanarGraph(ref s));
        Assert.IsTrue(KasteleynApi.TryOrientKasteleyn(ref s));

        for (var i = 0; i < s.VertexCount; i++)
        {
            Assert.AreEqual(0.0, s.Matrix[i * s.VertexCount + i], 0.001);
            for (var j = 0; j < s.VertexCount; j++)
                Assert.AreEqual(-s.Matrix[i * s.VertexCount + j], s.Matrix[j * s.VertexCount + i], 0.001);
        }

        KasteleynApi.Dispose(ref s);
        region.Dispose();
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(KasteleynApi.TryCreate(3, 3, 10, Allocator.Temp, out var s));
        KasteleynApi.Dispose(ref s);
        KasteleynApi.Dispose(ref s);
    }
}