using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

public class Grid2DTests
{
    [Test] public void Setup_SetsDimensions()
    { var g = Grid2D.Create(5, 3); Assert.AreEqual(5, g.Width); Assert.AreEqual(3, g.Height); Assert.AreEqual(15, g.Length); }

    [Test] public void ToIndex_Origin() { Assert.AreEqual(0, Grid2D.Create(5, 5).ToIndex(new int2(0, 0))); }
    [Test] public void ToIndex_RowMajor()
    {
        var g = Grid2D.Create(4, 3);
        Assert.AreEqual(0, g.ToIndex(0, 0));
        Assert.AreEqual(3, g.ToIndex(3, 0));
        Assert.AreEqual(4, g.ToIndex(0, 1));
        Assert.AreEqual(11, g.ToIndex(3, 2));
    }

    [Test] public void ToCoord_RoundTrip()
    {
        var g = Grid2D.Create(6, 4);
        for (int i = 0; i < g.Length; i++)
        { int2 c = g.ToCoord(i); Assert.AreEqual(i, g.ToIndex(c)); }
    }

    [Test] public void InBounds_Inside_True()
    {
        var g = Grid2D.Create(5, 5);
        Assert.IsTrue(g.InBounds(new int2(0, 0)));
        Assert.IsTrue(g.InBounds(new int2(4, 4)));
    }

    [Test] public void InBounds_Outside_False()
    {
        var g = Grid2D.Create(5, 5);
        Assert.IsFalse(g.InBounds(new int2(-1, 0)));
        Assert.IsFalse(g.InBounds(new int2(5, 0)));
        Assert.IsFalse(g.InBounds(new int2(0, 5)));
    }

    [Test] public void InBounds_Index()
    {
        var g = Grid2D.Create(5, 5);
        Assert.IsTrue(g.InBounds(0));
        Assert.IsTrue(g.InBounds(24));
        Assert.IsFalse(g.InBounds(25));
        Assert.IsFalse(g.InBounds(-1));
    }

    [Test] public void TryIndex_Valid()
    { var g = Grid2D.Create(5, 5); Assert.IsTrue(g.TryIndex(new int2(2, 3), out int i)); Assert.AreEqual(17, i); }

    [Test] public void TryIndex_Invalid()
    { var g = Grid2D.Create(5, 5); Assert.IsFalse(g.TryIndex(new int2(-1, 0), out _)); }

    [Test] public void HeuristicManhattan() { Assert.AreEqual(7f, Grid2D.HeuristicManhattan(new int2(1, 2), new int2(4, 6)), 0.001f); }
    [Test] public void HeuristicEuclidean() { Assert.AreEqual(5f, Grid2D.HeuristicEuclidean(new int2(0, 0), new int2(3, 4)), 0.001f); }
    [Test] public void HeuristicOctile_Straight() { Assert.AreEqual(5f, Grid2D.HeuristicOctile(new int2(0, 0), new int2(5, 0)), 0.001f); }
    [Test] public void HeuristicOctile_Diagonal() { Assert.AreEqual(3f * 1.4142135f, Grid2D.HeuristicOctile(new int2(0, 0), new int2(3, 3)), 0.001f); }

    [Test] public void Directions4_SumZero()
    { int2 s = int2.zero; for (int i = 0; i < 4; i++) s += Grid2D.Dir4(i); Assert.AreEqual(int2.zero, s); }

    [Test] public void Directions8_SumZero()
    { int2 s = int2.zero; for (int i = 0; i < 8; i++) s += Grid2D.Dir8(i); Assert.AreEqual(int2.zero, s); }

    [Test] public void OneByOne()
    {
        var g = Grid2D.Create(1, 1);
        Assert.AreEqual(1, g.Length);
        Assert.IsTrue(g.InBounds(new int2(0, 0)));
        Assert.AreEqual(0, g.ToIndex(new int2(0, 0)));
        Assert.AreEqual(new int2(0, 0), g.ToCoord(0));
    }

    [Test] public void SingleRow_RoundTrip()
    {
        var g = Grid2D.Create(10, 1);
        for (int x = 0; x < 10; x++) { Assert.AreEqual(x, g.ToIndex(new int2(x, 0))); Assert.AreEqual(new int2(x, 0), g.ToCoord(x)); }
    }

    [Test] public void SingleColumn_RoundTrip()
    {
        var g = Grid2D.Create(1, 10);
        for (int y = 0; y < 10; y++) { Assert.AreEqual(y, g.ToIndex(new int2(0, y))); Assert.AreEqual(new int2(0, y), g.ToCoord(y)); }
    }
}
