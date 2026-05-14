using BovineLabs.Grid;
using BovineLabs.Grid.Domino;
using NUnit.Framework;
using Unity.Collections;

public class DominoTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(DominoApi.TryCreate(4, 4, Allocator.Temp, out var s));
        Assert.AreEqual(16, s.Grid.Length);
        DominoApi.Dispose(ref s);
    }

    [Test]
    public unsafe void CheckTileable_Even()
    {
        Assert.IsTrue(DominoApi.TryCreate(2, 2, Allocator.Temp, out var s));
        var region = new NativeArray<byte>(4, Allocator.Temp);
        region.Fill((byte)1);
        DominoApi.SetRegion(ref s, region);
        Assert.IsTrue(DominoApi.CheckTileableByParity(ref s));
        DominoApi.Dispose(ref s);
        region.Dispose();
    }

    [Test]
    public unsafe void CheckTileable_Odd()
    {
        Assert.IsTrue(DominoApi.TryCreate(3, 3, Allocator.Temp, out var s));
        var region = new NativeArray<byte>(9, Allocator.Temp);
        region.Fill((byte)1);
        DominoApi.SetRegion(ref s, region);
        Assert.IsFalse(DominoApi.CheckTileableByParity(ref s));
        DominoApi.Dispose(ref s);
        region.Dispose();
    }

    [Test]
    public unsafe void BuildTiling_2x2()
    {
        Assert.IsTrue(DominoApi.TryCreate(2, 2, Allocator.Temp, out var s));
        var region = new NativeArray<byte>(4, Allocator.Temp);
        region.Fill((byte)1);
        DominoApi.SetRegion(ref s, region);
        Assert.IsTrue(DominoApi.TryBuildTilingByMatching(ref s));
        DominoApi.Dispose(ref s);
        region.Dispose();
    }

    [Test]
    public unsafe void BuildTiling_4x4()
    {
        Assert.IsTrue(DominoApi.TryCreate(4, 4, Allocator.Temp, out var s));
        var region = new NativeArray<byte>(16, Allocator.Temp);
        region.Fill((byte)1);
        DominoApi.SetRegion(ref s, region);

        Assert.IsTrue(DominoApi.CheckTileableByParity(ref s));
        DominoApi.Dispose(ref s);
        region.Dispose();
    }

    [Test]
    public unsafe void MutilatedChessboard_Untileable()
    {
        Assert.IsTrue(DominoApi.TryCreate(8, 8, Allocator.Temp, out var s));
        var region = new NativeArray<byte>(64, Allocator.Temp);
        region.Fill((byte)1);
        region[s.Grid.ToIndex(0, 0)] = 0;
        region[s.Grid.ToIndex(7, 7)] = 0;
        DominoApi.SetRegion(ref s, region);
        Assert.IsFalse(DominoApi.CheckTileableByParity(ref s), "Removing two same-color cells breaks parity");
        Assert.IsFalse(DominoApi.TryBuildTilingByMatching(ref s), "Mutilated chessboard cannot be tiled");
        DominoApi.Dispose(ref s);
        region.Dispose();
    }

    [Test]
    public unsafe void Flip_HorizontalToVertical()
    {
        Assert.IsTrue(DominoApi.TryCreate(2, 2, Allocator.Temp, out var s));
        var region = new NativeArray<byte>(4, Allocator.Temp);
        region.Fill((byte)1);
        DominoApi.SetRegion(ref s, region);
        Assert.IsTrue(DominoApi.TryBuildTilingByMatching(ref s));
        var roots = 0;
        for (var i = 0; i < 4; i++)
            if (s.MatchingDir[i] != 0)
                roots++;
        Assert.AreEqual(2, roots, "Must have exactly 2 domino roots");
        var anyFlipped = false;
        for (var i = 0; i < 4; i++)
            if (DominoApi.TryFlipAt(ref s, i))
            {
                anyFlipped = true;
                break;
            }

        Assert.IsTrue(anyFlipped, "At least one cell must allow a flip in 2x2");
        var newRoots = 0;
        for (var i = 0; i < 4; i++)
            if (s.MatchingDir[i] != 0)
                newRoots++;
        Assert.AreEqual(2, newRoots, "Must still have exactly 2 domino roots after flip");
        DominoApi.Dispose(ref s);
        region.Dispose();
    }

    [Test]
    public unsafe void Flip_OutOfBounds_ReturnsFalse()
    {
        Assert.IsTrue(DominoApi.TryCreate(2, 2, Allocator.Temp, out var s));
        var region = new NativeArray<byte>(4, Allocator.Temp);
        region.Fill((byte)1);
        DominoApi.SetRegion(ref s, region);
        Assert.IsFalse(DominoApi.TryFlipAt(ref s, -1));
        Assert.IsFalse(DominoApi.TryFlipAt(ref s, 99));
        DominoApi.Dispose(ref s);
        region.Dispose();
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(DominoApi.TryCreate(3, 3, Allocator.Temp, out var s));
        DominoApi.Dispose(ref s);
        DominoApi.Dispose(ref s);
    }
}