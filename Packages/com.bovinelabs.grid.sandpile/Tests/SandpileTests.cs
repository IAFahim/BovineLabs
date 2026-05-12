using NUnit.Framework;
using Unity.Collections;
using BovineLabs.Grid.Sandpile;

public class SandpileTests
{
    [Test] public void Create_Dimensions()
    { var s = SandpileApi.Create(5, 3, Allocator.Temp); Assert.AreEqual(15, s.Grid.Length); SandpileApi.Dispose(ref s); }

    [Test] public void Clear_Zeros()
    {
        var s = SandpileApi.Create(3, 3, Allocator.Temp);
        SandpileApi.AddGrains(ref s, 4, 10); SandpileApi.Clear(ref s);
        for (int i = 0; i < s.Grid.Length; i++) Assert.AreEqual(0, s.Grains[i]);
        SandpileApi.Dispose(ref s);
    }

    [Test] public void Add_SetsValue()
    { var s = SandpileApi.Create(3, 3, Allocator.Temp); SandpileApi.AddGrains(ref s, 4, 3); Assert.AreEqual(3, s.Grains[4]); SandpileApi.Dispose(ref s); }

    [Test] public void Add_Stable_NoEnqueue()
    { var s = SandpileApi.Create(3, 3, Allocator.Temp); SandpileApi.AddGrains(ref s, 4, 3); Assert.AreEqual(0, s.Queue.Count); SandpileApi.Dispose(ref s); }

    [Test] public void Add_Unstable_Enqueues()
    { var s = SandpileApi.Create(3, 3, Allocator.Temp); SandpileApi.AddGrains(ref s, 4, 4); Assert.IsTrue(s.Queue.Count > 0); SandpileApi.Dispose(ref s); }

    [Test] public void Relax_SingleTopple()
    {
        var s = SandpileApi.Create(3, 3, Allocator.Temp);
        SandpileApi.AddGrains(ref s, 4, 4); SandpileApi.RelaxAll(ref s);
        Assert.AreEqual(0, s.Grains[4]); Assert.AreEqual(1, s.Grains[1]); Assert.AreEqual(1, s.Grains[3]);
        Assert.AreEqual(1, s.Grains[5]); Assert.AreEqual(1, s.Grains[7]);
        Assert.IsTrue(SandpileApi.IsStable(ref s));
        SandpileApi.Dispose(ref s);
    }

    [Test] public void Relax_Chain()
    {
        var s = SandpileApi.Create(3, 3, Allocator.Temp);
        SandpileApi.AddGrains(ref s, 4, 3); SandpileApi.AddGrains(ref s, 4, 4);
        SandpileApi.RelaxAll(ref s);
        Assert.IsTrue(SandpileApi.IsStable(ref s));
        SandpileApi.Dispose(ref s);
    }

    [Test] public void Relax_Conservation()
    {
        var s = SandpileApi.Create(5, 5, Allocator.Temp);
        SandpileApi.AddGrains(ref s, 12, 16); SandpileApi.RelaxAll(ref s);
        int total = 0; for (int i = 0; i < s.Grid.Length; i++) total += s.Grains[i];
        Assert.AreEqual(16, total);
        Assert.IsTrue(SandpileApi.IsStable(ref s));
        SandpileApi.Dispose(ref s);
    }

    [Test] public void Relax_Edge()
    {
        var s = SandpileApi.Create(3, 3, Allocator.Temp);
        SandpileApi.AddGrains(ref s, 0, 4); SandpileApi.RelaxAll(ref s);
        Assert.AreEqual(0, s.Grains[0]); Assert.AreEqual(1, s.Grains[1]); Assert.AreEqual(1, s.Grains[3]);
        SandpileApi.Dispose(ref s);
    }

    [Test] public void Relax_MultipleSources()
    {
        var s = SandpileApi.Create(5, 5, Allocator.Temp);
        SandpileApi.AddGrains(ref s, 6, 8); SandpileApi.AddGrains(ref s, 12, 8); SandpileApi.AddGrains(ref s, 18, 8);
        SandpileApi.RelaxAll(ref s);
        Assert.IsTrue(SandpileApi.IsStable(ref s));
        SandpileApi.Dispose(ref s);
    }

    [Test] public void Stable_UnderThreshold()
    { var s = SandpileApi.Create(3, 3, Allocator.Temp); SandpileApi.AddGrains(ref s, 4, 3); SandpileApi.RelaxAll(ref s); Assert.AreEqual(3, s.Grains[4]); SandpileApi.Dispose(ref s); }

    [Test] public void IsStable_Empty()
    { var s = SandpileApi.Create(3, 3, Allocator.Temp); Assert.IsTrue(SandpileApi.IsStable(ref s)); SandpileApi.Dispose(ref s); }

    [Test] public void Dispose_Double() { var s = SandpileApi.Create(3, 3, Allocator.Temp); SandpileApi.Dispose(ref s); SandpileApi.Dispose(ref s); }
}
