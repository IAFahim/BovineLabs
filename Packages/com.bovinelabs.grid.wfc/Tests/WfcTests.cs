using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Wfc;

public class WfcTests
{
    [Test] public void Create_Dimensions()
    { var s = WfcApi.Create(3, 3, 4, Allocator.Temp); Assert.AreEqual(9, s.Grid.Length); WfcApi.Dispose(ref s); }

    [Test] public void InitializeAllPossible()
    {
        var s = WfcApi.Create(3, 3, 4, Allocator.Temp);
        WfcApi.InitializeAllPossible(ref s);
        for (int i = 0; i < s.Grid.Length; i++) Assert.AreEqual(4, s.Entropy[i]);
        WfcApi.Dispose(ref s);
    }

    [Test] public void Observe_Collapses()
    {
        var s = WfcApi.Create(3, 3, 4, Allocator.Temp);
        WfcApi.InitializeAllPossible(ref s);
        WfcApi.Observe(ref s, 4, 2);
        Assert.AreEqual(1, s.Entropy[4]);
        Assert.AreEqual(1UL << 2, s.PossibleBits[4]);
        WfcApi.Dispose(ref s);
    }

    [Test] public void Run_Simple()
    {
        var s = WfcApi.Create(3, 3, 2, Allocator.Temp);
        var output = new NativeArray<int>(9, Allocator.Temp);
        var rng = new Unity.Mathematics.Random(42);
        // 2 patterns, no adjacency constraints (everything compatible)
        WfcApi.LearnAdjacency(ref s, new NativeArray<int>(0, Allocator.Temp), 0, 0);
        // Set all adjacency as compatible
        for (int i = 0; i < s.Compatibility.Length; i++)
            s.Compatibility[i] = 0x3; // both patterns
        bool result = WfcApi.Run(ref s, ref output, ref rng);
        Assert.IsTrue(result);
        for (int i = 0; i < 9; i++) Assert.LessOrEqual(output[i], 1);
        WfcApi.Dispose(ref s); output.Dispose();
    }

    [Test] public void Dispose_Double() { var s = WfcApi.Create(3, 3, 2, Allocator.Temp); WfcApi.Dispose(ref s); WfcApi.Dispose(ref s); }
}
