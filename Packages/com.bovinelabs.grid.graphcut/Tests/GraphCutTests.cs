using BovineLabs.Grid;
using BovineLabs.Grid.GraphCut;
using NUnit.Framework;
using Unity.Collections;

public class GraphCutTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(GraphCutApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        Assert.AreEqual(25, s.Grid.Length);
        GraphCutApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Solve_Small()
    {
        Assert.IsTrue(GraphCutApi.TryCreate(2, 1, 10, Allocator.Temp, out var s));
        Assert.IsTrue(GraphCutApi.TrySolve(ref s, 0, 1));
        GraphCutApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Bottleneck_FlowIsMinCut()
    {
        Assert.IsTrue(GraphCutApi.TryCreate(3, 1, 50, Allocator.Temp, out var s));
        var u0 = new NativeArray<int>(3, Allocator.Temp);
        var u1 = new NativeArray<int>(3, Allocator.Temp);
        var pw = new NativeArray<int>(3, Allocator.Temp);
        u0.Fill(0);
        u1.Fill(0);
        pw.Fill(0);
        u0[0] = 100;
        u1[2] = 100;
        pw[0] = 5;
        pw[1] = 5;
        GraphCutApi.BuildBinaryEnergy(ref s, u0, u1, pw);
        Assert.IsTrue(GraphCutApi.TryMinCut(ref s));
        var labels = new NativeArray<int>(3, Allocator.Temp);
        Assert.IsTrue(GraphCutApi.TryExtractCutLabels(ref s, ref labels, 0, 1));
        Assert.AreEqual(0, labels[0], "Node 0 should be source side");
        Assert.AreEqual(1, labels[2], "Node 2 should be sink side");
        GraphCutApi.Dispose(ref s);
        u0.Dispose();
        u1.Dispose();
        pw.Dispose();
        labels.Dispose();
    }

    [Test]
    public unsafe void GridPartition_SymmetricCut()
    {
        Assert.IsTrue(GraphCutApi.TryCreate(10, 10, 2000, Allocator.Temp, out var s));
        var u0 = new NativeArray<int>(100, Allocator.Temp);
        var u1 = new NativeArray<int>(100, Allocator.Temp);
        var pw = new NativeArray<int>(100, Allocator.Temp);
        u0.Fill(0);
        u1.Fill(0);
        pw.Fill(1);
        for (var y = 0; y < 10; y++)
        {
            u0[s.Grid.ToIndex(0, y)] = 1000;
            u1[s.Grid.ToIndex(9, y)] = 1000;
        }

        GraphCutApi.BuildBinaryEnergy(ref s, u0, u1, pw);
        Assert.IsTrue(GraphCutApi.TryMinCut(ref s));
        var labels = new NativeArray<int>(100, Allocator.Temp);
        Assert.IsTrue(GraphCutApi.TryExtractCutLabels(ref s, ref labels, 0, 1));
        Assert.AreEqual(0, labels[s.Grid.ToIndex(0, 0)], "Left col should be source");
        Assert.AreEqual(1, labels[s.Grid.ToIndex(9, 0)], "Right col should be sink");
        GraphCutApi.Dispose(ref s);
        u0.Dispose();
        u1.Dispose();
        pw.Dispose();
        labels.Dispose();
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(GraphCutApi.TryCreate(3, 3, 20, Allocator.Temp, out var s));
        GraphCutApi.Dispose(ref s);
        GraphCutApi.Dispose(ref s);
    }
}