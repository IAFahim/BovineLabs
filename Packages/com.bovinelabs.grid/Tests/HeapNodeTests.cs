using BovineLabs.Grid;
using NUnit.Framework;

public class HeapNodeTests
{
    [Test]
    public unsafe void Constructor_Full()
    {
        var n = new HeapNode(3, 1.5f, 2.0f);
        Assert.AreEqual(3, n.Id);
        Assert.AreEqual(1.5f, n.Key0, 0.001f);
        Assert.AreEqual(2.0f, n.Key1, 0.001f);
    }

    [Test]
    public unsafe void Constructor_DefaultKey1()
    {
        var n = new HeapNode(5, 3f);
        Assert.AreEqual(0f, n.Key1, 0.001f);
    }

    [Test]
    public unsafe void ZeroKey()
    {
        var n = new HeapNode(0, 0f);
        Assert.AreEqual(0, n.Id);
        Assert.AreEqual(0f, n.Key0);
    }
}