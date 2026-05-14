using BovineLabs.Grid;
using NUnit.Framework;
using Unity.Mathematics;

public class Grid2DTests
{
    [Test]
    public unsafe void Setup_SetsDimensions()
    {
        Assert.IsTrue(Grid2D.TryCreate(5, 3, out var g));
        Assert.AreEqual(5, g.Width);
        Assert.AreEqual(3, g.Height);
        Assert.AreEqual(15, g.Length);
    }

    [Test]
    public unsafe void ToIndex_Origin()
    {
        Assert.IsTrue(Grid2D.TryCreate(5, 5, out var g));
        Assert.AreEqual(0, g.ToIndex(new int2(0, 0)));
    }

    [Test]
    public unsafe void ToIndex_RowMajor()
    {
        Assert.IsTrue(Grid2D.TryCreate(4, 3, out var g));
        Assert.AreEqual(0, g.ToIndex(0, 0));
        Assert.AreEqual(3, g.ToIndex(3, 0));
        Assert.AreEqual(4, g.ToIndex(0, 1));
        Assert.AreEqual(11, g.ToIndex(3, 2));
    }

    [Test]
    public unsafe void ToCoord_RoundTrip()
    {
        Assert.IsTrue(Grid2D.TryCreate(6, 4, out var g));
        for (var i = 0; i < g.Length; i++)
        {
            var c = g.ToCoord(i);
            Assert.AreEqual(i, g.ToIndex(c));
        }
    }

    [Test]
    public unsafe void InBounds_Inside_True()
    {
        Assert.IsTrue(Grid2D.TryCreate(5, 5, out var g));
        Assert.IsTrue(g.InBounds(new int2(0, 0)));
        Assert.IsTrue(g.InBounds(new int2(4, 4)));
    }

    [Test]
    public unsafe void InBounds_Outside_False()
    {
        Assert.IsTrue(Grid2D.TryCreate(5, 5, out var g));
        Assert.IsFalse(g.InBounds(new int2(-1, 0)));
        Assert.IsFalse(g.InBounds(new int2(5, 0)));
        Assert.IsFalse(g.InBounds(new int2(0, 5)));
    }

    [Test]
    public unsafe void InBounds_Index()
    {
        Assert.IsTrue(Grid2D.TryCreate(5, 5, out var g));
        Assert.IsTrue(g.InBounds(0));
        Assert.IsTrue(g.InBounds(24));
        Assert.IsFalse(g.InBounds(25));
        Assert.IsFalse(g.InBounds(-1));
    }

    [Test]
    public unsafe void TryIndex_Valid()
    {
        Assert.IsTrue(Grid2D.TryCreate(5, 5, out var g));
        Assert.IsTrue(g.TryIndex(new int2(2, 3), out var i));
        Assert.AreEqual(17, i);
    }

    [Test]
    public unsafe void TryIndex_Invalid()
    {
        Assert.IsTrue(Grid2D.TryCreate(5, 5, out var g));
        Assert.IsFalse(g.TryIndex(new int2(-1, 0), out _));
    }

    [Test]
    public unsafe void HeuristicManhattan()
    {
        Assert.AreEqual(7f, Grid2D.HeuristicManhattan(new int2(1, 2), new int2(4, 6)), 0.001f);
    }

    [Test]
    public unsafe void HeuristicEuclidean()
    {
        Assert.AreEqual(5f, Grid2D.HeuristicEuclidean(new int2(0, 0), new int2(3, 4)), 0.001f);
    }

    [Test]
    public unsafe void HeuristicOctile_Straight()
    {
        Assert.AreEqual(5f, Grid2D.HeuristicOctile(new int2(0, 0), new int2(5, 0)), 0.001f);
    }

    [Test]
    public unsafe void HeuristicOctile_Diagonal()
    {
        Assert.AreEqual(3f * 1.4142135f, Grid2D.HeuristicOctile(new int2(0, 0), new int2(3, 3)), 0.001f);
    }

    [Test]
    public unsafe void Directions4_SumZero()
    {
        var s = int2.zero;
        for (var i = 0; i < 4; i++) s += Grid2D.Dir4(i);
        Assert.AreEqual(int2.zero, s);
    }

    [Test]
    public unsafe void Directions8_SumZero()
    {
        var s = int2.zero;
        for (var i = 0; i < 8; i++) s += Grid2D.Dir8(i);
        Assert.AreEqual(int2.zero, s);
    }

    [Test]
    public unsafe void OneByOne()
    {
        Assert.IsTrue(Grid2D.TryCreate(1, 1, out var g));
        Assert.AreEqual(1, g.Length);
        Assert.IsTrue(g.InBounds(new int2(0, 0)));
        Assert.AreEqual(0, g.ToIndex(new int2(0, 0)));
        Assert.AreEqual(new int2(0, 0), g.ToCoord(0));
    }

    [Test]
    public unsafe void SingleRow_RoundTrip()
    {
        Assert.IsTrue(Grid2D.TryCreate(10, 1, out var g));
        for (var x = 0; x < 10; x++)
        {
            Assert.AreEqual(x, g.ToIndex(new int2(x, 0)));
            Assert.AreEqual(new int2(x, 0), g.ToCoord(x));
        }
    }

    [Test]
    public unsafe void SingleColumn_RoundTrip()
    {
        Assert.IsTrue(Grid2D.TryCreate(1, 10, out var g));
        for (var y = 0; y < 10; y++)
        {
            Assert.AreEqual(y, g.ToIndex(new int2(0, y)));
            Assert.AreEqual(new int2(0, y), g.ToCoord(y));
        }
    }
}