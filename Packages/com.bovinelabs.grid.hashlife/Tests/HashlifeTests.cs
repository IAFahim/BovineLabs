using BovineLabs.Grid.Hashlife;
using NUnit.Framework;
using Unity.Collections;

public class HashlifeTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(HashlifeApi.TryCreate(100, Allocator.Temp, out var s));
        Assert.IsTrue(s.Nodes.IsCreated);
        Assert.IsTrue(s.Intern.Keys != null);
        HashlifeApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(HashlifeApi.TryCreate(100, Allocator.Temp, out var s));
        HashlifeApi.Dispose(ref s);
        HashlifeApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Create_Leaves()
    {
        Assert.IsTrue(HashlifeApi.TryCreate(100, Allocator.Temp, out var s));
        Assert.AreEqual(0, s.Nodes[0].Level);
        Assert.AreEqual(0, s.Nodes[1].Level);
        HashlifeApi.Dispose(ref s);
    }

    [Test]
    public unsafe void MakeNode_Interns()
    {
        Assert.IsTrue(HashlifeApi.TryCreate(100, Allocator.Temp, out var s));
        Assert.IsTrue(HashlifeApi.TryMakeNode(ref s, 0, 0, 0, 0, out var id1));
        Assert.IsTrue(HashlifeApi.TryMakeNode(ref s, 0, 0, 0, 0, out var id2));
        Assert.AreEqual(id1, id2);
        HashlifeApi.Dispose(ref s);
    }
}