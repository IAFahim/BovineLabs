using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Wilson;

public class WilsonTests
{
    [Test] public void Create_Dimensions()
    { var s = WilsonApi.Create(5, 5, Allocator.Temp); Assert.AreEqual(25, s.Grid.Length); WilsonApi.Dispose(ref s); }

    [Test] public void Initialize_Root()
    {
        var s = WilsonApi.Create(5, 5, Allocator.Temp);
        WilsonApi.Initialize(ref s, 0);
        Assert.AreEqual(1, s.InTree[0]);
        Assert.AreEqual(0, s.InTree[1]);
        WilsonApi.Dispose(ref s);
    }

    [Test] public void BuildTree_Small()
    {
        var s = WilsonApi.Create(3, 3, Allocator.Temp);
        var rng = new Unity.Mathematics.Random(42);
        WilsonApi.Initialize(ref s, 0);
        WilsonApi.BuildTree(ref s, ref rng);
        // All cells should be in tree
        for (int i = 0; i < s.Grid.Length; i++)
            Assert.AreEqual(1, s.InTree[i]);
        // Parent[0] should be -1 (root)
        Assert.AreEqual(-1, s.Parent[0]);
        // All others should have parent
        for (int i = 1; i < s.Grid.Length; i++)
            Assert.AreNotEqual(-1, s.Parent[i]);
        WilsonApi.Dispose(ref s);
    }

    [Test] public void BuildTree_1x5()
    {
        var s = WilsonApi.Create(5, 1, Allocator.Temp);
        var rng = new Unity.Mathematics.Random(123);
        WilsonApi.Initialize(ref s, 0);
        WilsonApi.BuildTree(ref s, ref rng);
        for (int i = 0; i < 5; i++) Assert.AreEqual(1, s.InTree[i]);
        WilsonApi.Dispose(ref s);
    }

    [Test] public void BuildTree_Large()
    {
        var s = WilsonApi.Create(10, 10, Allocator.Temp);
        var rng = new Unity.Mathematics.Random(999);
        WilsonApi.Initialize(ref s, 0);
        WilsonApi.BuildTree(ref s, ref rng);
        for (int i = 0; i < 100; i++) Assert.AreEqual(1, s.InTree[i]);
        WilsonApi.Dispose(ref s);
    }

    [Test] public void ExtractMazeWalls()
    {
        var s = WilsonApi.Create(3, 3, Allocator.Temp);
        var rng = new Unity.Mathematics.Random(42);
        WilsonApi.Initialize(ref s, 0);
        WilsonApi.BuildTree(ref s, ref rng);
        var walls = new NativeArray<byte>(9, Allocator.Temp);
        WilsonApi.ExtractMazeWalls(ref s, ref walls);
        // At least some cells should be marked
        bool hasPassage = false;
        for (int i = 0; i < walls.Length; i++) if (walls[i] == 0) hasPassage = true;
        Assert.IsTrue(hasPassage);
        WilsonApi.Dispose(ref s); walls.Dispose();
    }

    [Test] public void Dispose_Double() { var s = WilsonApi.Create(3, 3, Allocator.Temp); WilsonApi.Dispose(ref s); WilsonApi.Dispose(ref s); }
}
