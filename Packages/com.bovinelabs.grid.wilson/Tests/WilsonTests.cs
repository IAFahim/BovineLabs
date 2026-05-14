using BovineLabs.Grid.Wilson;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

public class WilsonTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(WilsonApi.TryCreate(5, 5, Allocator.Temp, out var s));
        Assert.AreEqual(25, s.Grid.Length);
        WilsonApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Initialize_Root()
    {
        Assert.IsTrue(WilsonApi.TryCreate(5, 5, Allocator.Temp, out var s));
        Assert.IsTrue(WilsonApi.TryInitialize(ref s, 0));
        Assert.AreEqual(1, s.InTree[0]);
        Assert.AreEqual(0, s.InTree[1]);
        WilsonApi.Dispose(ref s);
    }

    [Test]
    public unsafe void BuildTree_Small()
    {
        Assert.IsTrue(WilsonApi.TryCreate(3, 3, Allocator.Temp, out var s));
        var rng = new Random(42);
        Assert.IsTrue(WilsonApi.TryInitialize(ref s, 0));
        Assert.IsTrue(WilsonApi.TryBuildTree(ref s, ref rng));

        for (var i = 0; i < s.Grid.Length; i++)
            Assert.AreEqual(1, s.InTree[i]);

        Assert.AreEqual(-1, s.Parent[0]);

        for (var i = 1; i < s.Grid.Length; i++)
            Assert.AreNotEqual(-1, s.Parent[i]);
        WilsonApi.Dispose(ref s);
    }

    [Test]
    public unsafe void BuildTree_1x5()
    {
        Assert.IsTrue(WilsonApi.TryCreate(5, 1, Allocator.Temp, out var s));
        var rng = new Random(123);
        Assert.IsTrue(WilsonApi.TryInitialize(ref s, 0));
        Assert.IsTrue(WilsonApi.TryBuildTree(ref s, ref rng));
        for (var i = 0; i < 5; i++) Assert.AreEqual(1, s.InTree[i]);
        WilsonApi.Dispose(ref s);
    }

    [Test]
    public unsafe void BuildTree_Large()
    {
        Assert.IsTrue(WilsonApi.TryCreate(10, 10, Allocator.Temp, out var s));
        var rng = new Random(999);
        Assert.IsTrue(WilsonApi.TryInitialize(ref s, 0));
        Assert.IsTrue(WilsonApi.TryBuildTree(ref s, ref rng));
        for (var i = 0; i < 100; i++) Assert.AreEqual(1, s.InTree[i]);
        WilsonApi.Dispose(ref s);
    }

    [Test]
    public unsafe void ExtractMazeWalls()
    {
        Assert.IsTrue(WilsonApi.TryCreate(3, 3, Allocator.Temp, out var s));
        var rng = new Random(42);
        Assert.IsTrue(WilsonApi.TryInitialize(ref s, 0));
        Assert.IsTrue(WilsonApi.TryBuildTree(ref s, ref rng));
        var walls = new NativeArray<byte>(9, Allocator.Temp);
        Assert.IsTrue(WilsonApi.TryExtractMazeWalls(ref s, ref walls));

        var hasPassage = false;
        for (var i = 0; i < walls.Length; i++)
            if (walls[i] == 0)
                hasPassage = true;
        Assert.IsTrue(hasPassage);
        WilsonApi.Dispose(ref s);
        walls.Dispose();
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(WilsonApi.TryCreate(3, 3, Allocator.Temp, out var s));
        WilsonApi.Dispose(ref s);
        WilsonApi.Dispose(ref s);
    }
}