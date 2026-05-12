using NUnit.Framework;
using Unity.Collections;
using BovineLabs.Grid.Belief;

public class BeliefTests
{
    [Test] public void Create_Dimensions()
    { var s = BeliefApi.Create(5, 5, 2, Allocator.Temp); Assert.AreEqual(25, s.Grid.Length); Assert.AreEqual(2, s.LabelCount); BeliefApi.Dispose(ref s); }

    [Test] public void ClearMessages()
    {
        var s = BeliefApi.Create(3, 3, 2, Allocator.Temp);
        for (int i = 0; i < s.Messages.Length; i++) s.Messages[i] = 5f;
        BeliefApi.ClearMessages(ref s);
        for (int i = 0; i < s.Messages.Length; i++) Assert.AreEqual(0f, s.Messages[i], 0.001f);
        BeliefApi.Dispose(ref s);
    }

    [Test] public void Iterate_ThenDecode()
    {
        var s = BeliefApi.Create(3, 3, 2, Allocator.Temp);
        var unary = new NativeArray<float>(s.Grid.Length * 2, Allocator.Temp);
        var pairwise = new NativeArray<float>(4, Allocator.Temp); // 2x2
        var labels = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
        // Strong unary: cell 0 prefers label 0
        for (int i = 0; i < unary.Length; i++) unary[i] = 10f;
        unary[0] = 0f;  unary[1] = 10f; // cell 0: label 0 is best
        for (int i = 0; i < pairwise.Length; i++) pairwise[i] = 1f; // smoothness

        BeliefApi.SetUnary(ref s, unary);
        BeliefApi.ClearMessages(ref s);
        BeliefApi.Iterate(ref s, pairwise, 5);
        BeliefApi.DecodeMap(ref s, ref labels);
        // With strong unary, cell 0 should be label 0
        Assert.AreEqual(0, labels[0]);
        BeliefApi.Dispose(ref s); unary.Dispose(); pairwise.Dispose(); labels.Dispose();
    }

    [Test] public void Dispose_Double() { var s = BeliefApi.Create(3, 3, 2, Allocator.Temp); BeliefApi.Dispose(ref s); BeliefApi.Dispose(ref s); }
}
