using BovineLabs.Grid;
using NUnit.Framework;
using Unity.Collections;

public class MinHeapTests
{
    private MinHeap heap;

    [SetUp]
    public unsafe void SetUp()
    {
        Assert.IsTrue(MinHeap.TryCreate(100, Allocator.Temp, out heap));
    }

    [TearDown]
    public unsafe void TearDown()
    {
        heap.Dispose();
    }

    [Test]
    public unsafe void Empty_OnCreate()
    {
        Assert.IsTrue(heap.IsEmpty);
        Assert.AreEqual(0, heap.Length);
    }

    [Test]
    public unsafe void Insert_Single()
    {
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(0, 5f)));
        Assert.AreEqual(1, heap.Length);
    }

    [Test]
    public unsafe void Pop_Single()
    {
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(3, 7f)));
        Assert.IsTrue(heap.TryPop(out var n));
        Assert.AreEqual(3, n.Id);
        Assert.IsTrue(heap.IsEmpty);
    }

    [Test]
    public unsafe void Pop_Ordered()
    {
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(0, 5f)));
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(1, 2f)));
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(2, 8f)));
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(3, 1f)));
        Assert.IsTrue(heap.TryPop(out var n3));
        Assert.AreEqual(3, n3.Id);
        Assert.IsTrue(heap.TryPop(out var n1));
        Assert.AreEqual(1, n1.Id);
        Assert.IsTrue(heap.TryPop(out var n0));
        Assert.AreEqual(0, n0.Id);
        Assert.IsTrue(heap.TryPop(out var n2));
        Assert.AreEqual(2, n2.Id);
    }

    [Test]
    public unsafe void Pop_TieBreak()
    {
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(0, 1f, 5f)));
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(1, 1f, 2f)));
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(2, 1f, 8f)));
        Assert.IsTrue(heap.TryPop(out var n1));
        Assert.AreEqual(1, n1.Id);
        Assert.IsTrue(heap.TryPop(out var n0));
        Assert.AreEqual(0, n0.Id);
        Assert.IsTrue(heap.TryPop(out var n2));
        Assert.AreEqual(2, n2.Id);
    }

    [Test]
    public unsafe void DecreaseKey()
    {
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(0, 10f)));
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(1, 5f)));
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(0, 2f)));
        Assert.IsTrue(heap.TryPop(out var n0));
        Assert.AreEqual(0, n0.Id);
        Assert.IsTrue(heap.TryPop(out var n1));
        Assert.AreEqual(1, n1.Id);
    }

    [Test]
    public unsafe void DecreaseKey_NoIncrease()
    {
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(0, 1f)));
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(0, 10f)));
        Assert.IsTrue(heap.TryPeek(out var top));
        Assert.AreEqual(1f, top.Key0, 0.001f);
    }

    [Test]
    public unsafe void Peek_NoRemove()
    {
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(0, 3f)));
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(1, 1f)));
        Assert.AreEqual(2, heap.Length);
    }

    [Test]
    public unsafe void Contains_Inserted()
    {
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(5, 3f)));
        Assert.IsTrue(heap.Contains(5));
    }

    [Test]
    public unsafe void Contains_NotInserted()
    {
        Assert.IsFalse(heap.Contains(0));
    }

    [Test]
    public unsafe void Contains_AfterPop()
    {
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(5, 3f)));
        Assert.IsTrue(heap.TryPop(out _));
        Assert.IsFalse(heap.Contains(5));
    }

    [Test]
    public unsafe void Clear()
    {
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(0, 5f)));
        heap.Clear();
        Assert.IsTrue(heap.IsEmpty);
        Assert.IsFalse(heap.Contains(0));
    }

    [Test]
    public unsafe void Many_MaintainsOrder()
    {
        for (var i = 0; i < 50; i++) Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(i, (50 - i) * 1.5f)));
        var prev = float.NegativeInfinity;
        while (!heap.IsEmpty)
        {
            Assert.IsTrue(heap.TryPop(out var n));
            Assert.LessOrEqual(prev, n.Key0);
            prev = n.Key0;
        }
    }

    [Test]
    public unsafe void DecreaseKey_Interleaved()
    {
        for (var i = 0; i < 10; i++) Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(i, 100f - i)));
        for (var i = 0; i < 10; i++) Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(i, i * 0.5f)));
        var count = 0;
        var prev = float.NegativeInfinity;
        while (!heap.IsEmpty)
        {
            Assert.IsTrue(heap.TryPop(out var n));
            Assert.LessOrEqual(prev, n.Key0);
            prev = n.Key0;
            count++;
        }

        Assert.AreEqual(10, count);
    }

    [Test]
    public unsafe void DuplicateId_OneEntry()
    {
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(0, 5f)));
        Assert.IsTrue(heap.TryInsertOrDecrease(new HeapNode(0, 3f)));
        Assert.AreEqual(1, heap.Length);
    }
}