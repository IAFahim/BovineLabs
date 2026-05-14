using BovineLabs.Grid;
using BovineLabs.Grid.Jps;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

public class JpsTests
{
    private JpsState state;

    [SetUp]
    public void SetUp()
    {
        Assert.IsTrue(JpsApi.TryCreate(10, 10, Allocator.Temp, out var s));
        state = s;
    }

    [TearDown]
    public void TearDown()
    {
        JpsApi.Dispose(ref state);
    }

    [Test]
    public void StartEqualsGoal()
    {
        var b = new NativeArray<byte>(100, Allocator.Temp);
        var p = new NativeList<int>(Allocator.Temp);
        b.Fill((byte)0);
        Assert.IsTrue(JpsApi.TrySearch(ref state, in b, 0, 0, ref p));
        Assert.AreEqual(1, p.Length);
        b.Dispose();
        p.Dispose();
    }

    [Test]
    public void Adjacent()
    {
        var b = new NativeArray<byte>(100, Allocator.Temp);
        var p = new NativeList<int>(Allocator.Temp);
        b.Fill((byte)0);
        Assert.IsTrue(JpsApi.TrySearch(ref state, in b, state.Grid.ToIndex(0, 0), state.Grid.ToIndex(1, 0), ref p));
        Assert.AreEqual(state.Grid.ToIndex(1, 0), p[p.Length - 1]);
        b.Dispose();
        p.Dispose();
    }

    [Test]
    public void OpenGrid_Distant()
    {
        var b = new NativeArray<byte>(100, Allocator.Temp);
        var p = new NativeList<int>(Allocator.Temp);
        b.Fill((byte)0);
        Assert.IsTrue(JpsApi.TrySearch(ref state, in b, 0, 99, ref p));
        Assert.AreEqual(0, p[0]);
        Assert.AreEqual(99, p[p.Length - 1]);
        b.Dispose();
        p.Dispose();
    }

    [Test]
    public void BlockedGoal()
    {
        var b = new NativeArray<byte>(100, Allocator.Temp);
        var p = new NativeList<int>(Allocator.Temp);
        b.Fill((byte)0);
        b[55] = 1;
        Assert.IsFalse(JpsApi.TrySearch(ref state, in b, 0, 55, ref p));
        b.Dispose();
        p.Dispose();
    }

    [Test]
    public void BlockedStart()
    {
        var b = new NativeArray<byte>(100, Allocator.Temp);
        var p = new NativeList<int>(Allocator.Temp);
        b.Fill((byte)0);
        b[0] = 1;
        Assert.IsFalse(JpsApi.TrySearch(ref state, in b, 0, 99, ref p));
        b.Dispose();
        p.Dispose();
    }

    [Test]
    public void WallAround()
    {
        var b = new NativeArray<byte>(100, Allocator.Temp);
        var p = new NativeList<int>(Allocator.Temp);
        b.Fill((byte)0);
        for (var y = 0; y < 8; y++) b[state.Grid.ToIndex(5, y)] = 1;
        Assert.IsTrue(JpsApi.TrySearch(ref state, in b, state.Grid.ToIndex(2, 4), state.Grid.ToIndex(8, 4), ref p));
        for (var i = 0; i < p.Length; i++) Assert.AreEqual(0, b[p[i]]);
        b.Dispose();
        p.Dispose();
    }

    [Test]
    public void CompletelyWalled()
    {
        var b = new NativeArray<byte>(100, Allocator.Temp);
        var p = new NativeList<int>(Allocator.Temp);
        b.Fill((byte)0);
        for (var x = 3; x <= 7; x++)
        {
            b[state.Grid.ToIndex(x, 3)] = 1;
            b[state.Grid.ToIndex(x, 7)] = 1;
        }

        for (var y = 3; y <= 7; y++)
        {
            b[state.Grid.ToIndex(3, y)] = 1;
            b[state.Grid.ToIndex(7, y)] = 1;
        }

        Assert.IsFalse(JpsApi.TrySearch(ref state, in b, state.Grid.ToIndex(0, 0), state.Grid.ToIndex(5, 5), ref p));
        b.Dispose();
        p.Dispose();
    }

    [Test]
    public void NoBacktracking()
    {
        var b = new NativeArray<byte>(100, Allocator.Temp);
        var p = new NativeList<int>(Allocator.Temp);
        var seen = new NativeArray<byte>(100, Allocator.Temp);
        b.Fill((byte)0);
        Assert.IsTrue(JpsApi.TrySearch(ref state, in b, state.Grid.ToIndex(0, 0), state.Grid.ToIndex(9, 9), ref p));
        for (var i = 0; i < p.Length; i++)
        {
            Assert.AreEqual(0, seen[p[i]]);
            seen[p[i]] = 1;
        }

        b.Dispose();
        p.Dispose();
        seen.Dispose();
    }

    [Test]
    public void Jump_ToGoal()
    {
        var b = new NativeArray<byte>(100, Allocator.Temp);
        b.Fill((byte)0);
        Assert.IsTrue(JpsApi.TryJump(in state, in b, new int2(0, 0), new int2(1, 0), state.Grid.ToIndex(5, 0),
            out var j));
        Assert.AreEqual(state.Grid.ToIndex(5, 0), j);
        b.Dispose();
    }

    [Test]
    public void Jump_HitWall()
    {
        var b = new NativeArray<byte>(100, Allocator.Temp);
        b.Fill((byte)0);
        b[state.Grid.ToIndex(3, 0)] = 1;
        Assert.IsFalse(JpsApi.TryJump(in state, in b, new int2(0, 0), new int2(1, 0), 99, out _));
        b.Dispose();
    }

    [Test]
    public void Jump_Diagonal()
    {
        var b = new NativeArray<byte>(100, Allocator.Temp);
        b.Fill((byte)0);
        Assert.IsTrue(JpsApi.TryJump(in state, in b, new int2(0, 0), new int2(1, 1), state.Grid.ToIndex(3, 3),
            out var j));
        Assert.AreEqual(state.Grid.ToIndex(3, 3), j);
        b.Dispose();
    }

    [Test]
    public void Jump_OutOfBounds()
    {
        var b = new NativeArray<byte>(100, Allocator.Temp);
        b.Fill((byte)0);
        Assert.IsFalse(JpsApi.TryJump(in state, in b, new int2(0, 0), new int2(-1, 0), 99, out _));
        b.Dispose();
    }

    [Test]
    public void ExtractPath_TwoSteps()
    {
        var par = new NativeArray<int>(3, Allocator.Temp);
        var p = new NativeList<int>(Allocator.Temp);
        par[0] = -1;
        par[1] = 0;
        par[2] = 1;
        Assert.IsTrue(JpsApi.TryExtractPath(in par, 2, 0, ref p));
        Assert.AreEqual(3, p.Length);
        Assert.AreEqual(0, p[0]);
        Assert.AreEqual(2, p[2]);
        par.Dispose();
        p.Dispose();
    }

    [Test]
    public void ExtractPath_Single()
    {
        var par = new NativeArray<int>(1, Allocator.Temp);
        var p = new NativeList<int>(Allocator.Temp);
        par[0] = -1;
        Assert.IsTrue(JpsApi.TryExtractPath(in par, 0, 0, ref p));
        Assert.AreEqual(1, p.Length);
        par.Dispose();
        p.Dispose();
    }

    [Test]
    public void ExtractPath_Broken()
    {
        var par = new NativeArray<int>(5, Allocator.Temp);
        var p = new NativeList<int>(Allocator.Temp);
        par.Fill(-1);
        par[3] = 4;
        Assert.IsTrue(JpsApi.TryExtractPath(in par, 3, 0, ref p));
        Assert.LessOrEqual(p.Length, 5);
        par.Dispose();
        p.Dispose();
    }

    [Test]
    public void Create_Dimensions()
    {
        Assert.IsTrue(JpsApi.TryCreate(10, 10, Allocator.Temp, out var s));
        Assert.AreEqual(100, s.Grid.Length);
        JpsApi.Dispose(ref s);
    }

    [Test]
    public void Dispose_Double()
    {
        Assert.IsTrue(JpsApi.TryCreate(5, 5, Allocator.Temp, out var s));
        JpsApi.Dispose(ref s);
        JpsApi.Dispose(ref s);
    }

    [Test]
    public void FullyBlocked_NoPath()
    {
        Assert.IsTrue(JpsApi.TryCreate(5, 5, Allocator.Temp, out var s));
        var b = new NativeArray<byte>(25, Allocator.Temp);
        var p = new NativeList<int>(Allocator.Temp);
        b.Fill((byte)1);
        Assert.IsFalse(JpsApi.TrySearch(ref s, in b, 0, 24, ref p));
        JpsApi.Dispose(ref s);
        b.Dispose();
        p.Dispose();
    }

    [Test]
    public void _2x2_Diagonal()
    {
        Assert.IsTrue(JpsApi.TryCreate(2, 2, Allocator.Temp, out var s));
        var b = new NativeArray<byte>(4, Allocator.Temp);
        var p = new NativeList<int>(Allocator.Temp);
        b.Fill((byte)0);
        Assert.IsTrue(JpsApi.TrySearch(ref s, in b, s.Grid.ToIndex(0, 0), s.Grid.ToIndex(1, 1), ref p));
        Assert.AreEqual(s.Grid.ToIndex(1, 1), p[p.Length - 1]);
        JpsApi.Dispose(ref s);
        b.Dispose();
        p.Dispose();
    }

    [Test]
    public void _1x5_Linear()
    {
        Assert.IsTrue(JpsApi.TryCreate(5, 1, Allocator.Temp, out var s));
        var b = new NativeArray<byte>(5, Allocator.Temp);
        var p = new NativeList<int>(Allocator.Temp);
        b.Fill((byte)0);
        Assert.IsTrue(JpsApi.TrySearch(ref s, in b, 0, 4, ref p));
        Assert.AreEqual(4, p[p.Length - 1]);
        JpsApi.Dispose(ref s);
        b.Dispose();
        p.Dispose();
    }
}