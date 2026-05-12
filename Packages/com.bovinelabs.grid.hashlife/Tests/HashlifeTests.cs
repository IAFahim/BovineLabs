using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Hashlife;

public class HashlifeTests
{
    [Test] public void Create_NotNull()
    {
        var s = HashlifeApi.Create(1000, Allocator.Temp);
        Assert.IsTrue(s.Nodes.IsCreated);
        // Pre-creates dead (0) and alive (1) leaf nodes
        Assert.GreaterOrEqual(s.Nodes.Length, 2);
        Assert.AreEqual(0, s.Nodes[0].Level);
        Assert.AreEqual(1, s.Nodes[1].Level);
        HashlifeApi.Dispose(ref s);
    }

    [Test] public void MakeNode_Deduplicates()
    {
        var s = HashlifeApi.Create(1000, Allocator.Temp);
        int id1 = HashlifeApi.MakeNode(ref s, 0, 0, 0, 0);
        int id2 = HashlifeApi.MakeNode(ref s, 0, 0, 0, 0);
        Assert.AreEqual(id1, id2);
        HashlifeApi.Dispose(ref s);
    }

    [Test] public void MakeNode_DistinctNodes()
    {
        var s = HashlifeApi.Create(1000, Allocator.Temp);
        int allDead = HashlifeApi.MakeNode(ref s, 0, 0, 0, 0);
        int allAlive = HashlifeApi.MakeNode(ref s, 1, 1, 1, 1);
        Assert.AreNotEqual(allDead, allAlive);
        HashlifeApi.Dispose(ref s);
    }

    [Test] public void Decode_AllDead()
    {
        var s = HashlifeApi.Create(1000, Allocator.Temp);
        var grid = Grid2D.Create(4, 4);
        var cells = new NativeArray<byte>(16, Allocator.Temp);
        int root = HashlifeApi.MakeNode(ref s, 0, 0, 0, 0);
        HashlifeApi.Decode(ref s, root, cells, grid);
        for (int i = 0; i < 16; i++) Assert.AreEqual(0, cells[i]);
        HashlifeApi.Dispose(ref s); cells.Dispose();
    }

    [Test] public void Dispose_Double()
    {
        var s = HashlifeApi.Create(100, Allocator.Temp);
        HashlifeApi.Dispose(ref s);
        HashlifeApi.Dispose(ref s);
    }
}
