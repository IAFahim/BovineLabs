using BovineLabs.Grid;
using BovineLabs.Grid.Belief;
using NUnit.Framework;
using Unity.Collections;

public class BeliefTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(BeliefApi.TryCreate(5, 5, 2, Allocator.Temp, out var s));
        Assert.AreEqual(25, s.Grid.Length);
        Assert.AreEqual(2, s.LabelCount);
        BeliefApi.Dispose(ref s);
    }

    [Test]
    public unsafe void ClearMessages()
    {
        Assert.IsTrue(BeliefApi.TryCreate(3, 3, 2, Allocator.Temp, out var s));
        for (var i = 0; i < 400; i++) s.Messages[i] = 5f;
        BeliefApi.ClearMessages(ref s);
        for (var i = 0; i < 400; i++) Assert.AreEqual(0f, s.Messages[i], 0.001f);
        BeliefApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Iterate_ThenDecode()
    {
        Assert.IsTrue(BeliefApi.TryCreate(3, 3, 2, Allocator.Temp, out var s));
        var unary = new NativeArray<float>(s.Grid.Length * 2, Allocator.Temp);
        var pairwise = new NativeArray<float>(4, Allocator.Temp);
        var labels = new NativeArray<int>(s.Grid.Length, Allocator.Temp);

        for (var i = 0; i < unary.Length; i++) unary[i] = 10f;
        unary[0] = 0f;
        unary[1] = 10f;
        for (var i = 0; i < pairwise.Length; i++) pairwise[i] = 1f;

        BeliefApi.SetUnary(ref s, unary);
        BeliefApi.ClearMessages(ref s);
        Assert.IsTrue(BeliefApi.TryIterate(ref s, pairwise, 5));
        Assert.IsTrue(BeliefApi.TryDecodeMap(ref s, ref labels));

        Assert.AreEqual(0, labels[0]);
        BeliefApi.Dispose(ref s);
        unary.Dispose();
        pairwise.Dispose();
        labels.Dispose();
    }

    [Test]
    public unsafe void ConsensusChain()
    {
        Assert.IsTrue(BeliefApi.TryCreate(5, 1, 2, Allocator.Temp, out var s));
        var unary = new NativeArray<float>(s.Grid.Length * 2, Allocator.Temp);
        var pairwise = new NativeArray<float>(4, Allocator.Temp);
        var labels = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
        unary.Fill(0f);
        unary[0 * 2 + 0] = 0f;
        unary[0 * 2 + 1] = 100f;
        unary[4 * 2 + 0] = 100f;
        unary[4 * 2 + 1] = 0f;
        pairwise[0] = 0f;
        pairwise[1] = 10f;
        pairwise[2] = 10f;
        pairwise[3] = 0f;
        BeliefApi.SetUnary(ref s, unary);
        BeliefApi.ClearMessages(ref s);
        Assert.IsTrue(BeliefApi.TryIterate(ref s, pairwise, 10));
        Assert.IsTrue(BeliefApi.TryDecodeMap(ref s, ref labels));
        Assert.AreEqual(0, labels[0], "Cell 0 should be label A");
        Assert.AreEqual(1, labels[4], "Cell 4 should be label B");
        BeliefApi.Dispose(ref s);
        unary.Dispose();
        pairwise.Dispose();
        labels.Dispose();
    }

    [Test]
    public unsafe void MessageClear_NoGhostBeliefs()
    {
        Assert.IsTrue(BeliefApi.TryCreate(3, 1, 2, Allocator.Temp, out var s));
        var unary1 = new NativeArray<float>(s.Grid.Length * 2, Allocator.Temp);
        var pairwise = new NativeArray<float>(4, Allocator.Temp);
        var labels1 = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
        var labels2 = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
        unary1.Fill(0f);
        unary1[0 * 2 + 0] = 0f;
        unary1[0 * 2 + 1] = 100f;
        pairwise[0] = 0f;
        pairwise[1] = 1f;
        pairwise[2] = 1f;
        pairwise[3] = 0f;
        BeliefApi.SetUnary(ref s, unary1);
        BeliefApi.ClearMessages(ref s);
        Assert.IsTrue(BeliefApi.TryIterate(ref s, pairwise, 5));
        Assert.IsTrue(BeliefApi.TryDecodeMap(ref s, ref labels1));
        var unary2 = new NativeArray<float>(s.Grid.Length * 2, Allocator.Temp);
        unary2.Fill(0f);
        BeliefApi.SetUnary(ref s, unary2);
        BeliefApi.ClearMessages(ref s);
        Assert.IsTrue(BeliefApi.TryIterate(ref s, pairwise, 5));
        Assert.IsTrue(BeliefApi.TryDecodeMap(ref s, ref labels2));
        for (var i = 0; i < labels2.Length; i++)
            Assert.AreEqual(labels1[i], labels2[i], $"Cell {i} should match fresh run when unary is neutral");
        BeliefApi.Dispose(ref s);
        unary1.Dispose();
        unary2.Dispose();
        pairwise.Dispose();
        labels1.Dispose();
        labels2.Dispose();
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(BeliefApi.TryCreate(3, 3, 2, Allocator.Temp, out var s));
        BeliefApi.Dispose(ref s);
        BeliefApi.Dispose(ref s);
    }
}