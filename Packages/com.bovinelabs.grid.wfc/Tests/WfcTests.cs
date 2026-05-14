using BovineLabs.Grid.Wfc;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

public class WfcTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(WfcApi.TryCreate(3, 3, 4, Allocator.Temp, out var s));
        Assert.AreEqual(9, s.Grid.Length);
        WfcApi.Dispose(ref s);
    }

    [Test]
    public unsafe void InitializeAllPossible()
    {
        Assert.IsTrue(WfcApi.TryCreate(3, 3, 4, Allocator.Temp, out var s));
        Assert.IsTrue(WfcApi.TryInitializeAllPossible(ref s));
        for (var i = 0; i < s.Grid.Length; i++) Assert.AreEqual(4, s.Entropy[i]);
        WfcApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Observe_Collapses()
    {
        Assert.IsTrue(WfcApi.TryCreate(3, 3, 4, Allocator.Temp, out var s));
        Assert.IsTrue(WfcApi.TryInitializeAllPossible(ref s));
        Assert.IsTrue(WfcApi.TryObserve(ref s, 4, 2));
        Assert.AreEqual(1, s.Entropy[4]);
        Assert.AreEqual(1UL << 2, s.PossibleBits[4]);
        WfcApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Run_Simple()
    {
        Assert.IsTrue(WfcApi.TryCreate(3, 3, 2, Allocator.Temp, out var s));
        var output = new NativeArray<int>(9, Allocator.Temp);
        var rng = new Random(42);

        Assert.IsTrue(WfcApi.TryLearnAdjacency(ref s, new NativeArray<int>(0, Allocator.Temp), 0, 0));

        for (var i = 0; i < s.PatternCount * 4; i++)
            s.Compatibility[i] = 0x3;
        Assert.IsTrue(WfcApi.TryRun(ref s, ref output, ref rng));
        for (var i = 0; i < 9; i++) Assert.LessOrEqual(output[i], 1);
        WfcApi.Dispose(ref s);
        output.Dispose();
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(WfcApi.TryCreate(3, 3, 2, Allocator.Temp, out var s));
        WfcApi.Dispose(ref s);
        WfcApi.Dispose(ref s);
    }
}