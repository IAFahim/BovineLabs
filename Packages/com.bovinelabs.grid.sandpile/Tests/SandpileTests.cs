using BovineLabs.Grid.Sandpile;
using NUnit.Framework;
using Unity.Collections;

public class SandpileTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(SandpileApi.TryCreate(5, 3, Allocator.Temp, out var s));
        Assert.AreEqual(15, s.Grid.Length);
        SandpileApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Clear_Zeros()
    {
        Assert.IsTrue(SandpileApi.TryCreate(3, 3, Allocator.Temp, out var s));
        SandpileApi.AddGrains(ref s, 4, 10);
        SandpileApi.Clear(ref s);
        for (var i = 0; i < s.Grid.Length; i++) Assert.AreEqual(0, s.Grains[i]);
        SandpileApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Add_SetsValue()
    {
        Assert.IsTrue(SandpileApi.TryCreate(3, 3, Allocator.Temp, out var s));
        SandpileApi.AddGrains(ref s, 4, 3);
        Assert.AreEqual(3, s.Grains[4]);
        SandpileApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Add_Stable_NoEnqueue()
    {
        Assert.IsTrue(SandpileApi.TryCreate(3, 3, Allocator.Temp, out var s));
        SandpileApi.AddGrains(ref s, 4, 3);
        Assert.AreEqual(0, s.Queue.Count);
        SandpileApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Add_Unstable_Enqueues()
    {
        Assert.IsTrue(SandpileApi.TryCreate(3, 3, Allocator.Temp, out var s));
        SandpileApi.AddGrains(ref s, 4, 4);
        Assert.IsTrue(s.Queue.Count > 0);
        SandpileApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Relax_SingleTopple()
    {
        Assert.IsTrue(SandpileApi.TryCreate(3, 3, Allocator.Temp, out var s));
        SandpileApi.AddGrains(ref s, 4, 4);
        SandpileApi.RelaxAll(ref s);
        Assert.AreEqual(0, s.Grains[4]);
        Assert.AreEqual(1, s.Grains[1]);
        Assert.AreEqual(1, s.Grains[3]);
        Assert.AreEqual(1, s.Grains[5]);
        Assert.AreEqual(1, s.Grains[7]);
        Assert.IsTrue(SandpileApi.IsStable(ref s));
        SandpileApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Relax_Chain()
    {
        Assert.IsTrue(SandpileApi.TryCreate(3, 3, Allocator.Temp, out var s));
        SandpileApi.AddGrains(ref s, 4, 3);
        SandpileApi.AddGrains(ref s, 4, 4);
        SandpileApi.RelaxAll(ref s);
        Assert.IsTrue(SandpileApi.IsStable(ref s));
        SandpileApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Relax_Conservation()
    {
        Assert.IsTrue(SandpileApi.TryCreate(5, 5, Allocator.Temp, out var s));
        SandpileApi.AddGrains(ref s, 12, 16);
        SandpileApi.RelaxAll(ref s);
        var total = 0;
        for (var i = 0; i < s.Grid.Length; i++) total += s.Grains[i];
        Assert.AreEqual(16, total);
        Assert.IsTrue(SandpileApi.IsStable(ref s));
        SandpileApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Relax_Edge()
    {
        Assert.IsTrue(SandpileApi.TryCreate(3, 3, Allocator.Temp, out var s));
        SandpileApi.AddGrains(ref s, 0, 4);
        SandpileApi.RelaxAll(ref s);
        Assert.AreEqual(0, s.Grains[0]);
        Assert.AreEqual(1, s.Grains[1]);
        Assert.AreEqual(1, s.Grains[3]);
        SandpileApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Relax_MultipleSources()
    {
        Assert.IsTrue(SandpileApi.TryCreate(5, 5, Allocator.Temp, out var s));
        SandpileApi.AddGrains(ref s, 6, 8);
        SandpileApi.AddGrains(ref s, 12, 8);
        SandpileApi.AddGrains(ref s, 18, 8);
        SandpileApi.RelaxAll(ref s);
        Assert.IsTrue(SandpileApi.IsStable(ref s));
        SandpileApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Stable_UnderThreshold()
    {
        Assert.IsTrue(SandpileApi.TryCreate(3, 3, Allocator.Temp, out var s));
        SandpileApi.AddGrains(ref s, 4, 3);
        SandpileApi.RelaxAll(ref s);
        Assert.AreEqual(3, s.Grains[4]);
        SandpileApi.Dispose(ref s);
    }

    [Test]
    public unsafe void IsStable_Empty()
    {
        Assert.IsTrue(SandpileApi.TryCreate(3, 3, Allocator.Temp, out var s));
        Assert.IsTrue(SandpileApi.IsStable(ref s));
        SandpileApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(SandpileApi.TryCreate(3, 3, Allocator.Temp, out var s));
        SandpileApi.Dispose(ref s);
        SandpileApi.Dispose(ref s);
    }
}