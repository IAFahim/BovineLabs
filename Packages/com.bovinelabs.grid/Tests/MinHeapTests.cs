using NUnit.Framework;
using Unity.Collections;
using BovineLabs.Grid;

public class MinHeapTests
{
    private MinHeap heap;

    [SetUp] public void SetUp() { heap = MinHeap.Create(100, Allocator.Temp); }
    [TearDown] public void TearDown() { heap.Dispose(); }

    [Test] public void Empty_OnCreate() { Assert.IsTrue(heap.IsEmpty); Assert.AreEqual(0, heap.Length); }
    [Test] public void Insert_Single() { heap.InsertOrDecrease(new HeapNode(0, 5f)); Assert.AreEqual(1, heap.Length); }

    [Test] public void Pop_Single()
    { heap.InsertOrDecrease(new HeapNode(3, 7f)); var n = heap.Pop(); Assert.AreEqual(3, n.Id); Assert.IsTrue(heap.IsEmpty); }

    [Test] public void Pop_Ordered()
    {
        heap.InsertOrDecrease(new HeapNode(0, 5f));
        heap.InsertOrDecrease(new HeapNode(1, 2f));
        heap.InsertOrDecrease(new HeapNode(2, 8f));
        heap.InsertOrDecrease(new HeapNode(3, 1f));
        Assert.AreEqual(3, heap.Pop().Id);
        Assert.AreEqual(1, heap.Pop().Id);
        Assert.AreEqual(0, heap.Pop().Id);
        Assert.AreEqual(2, heap.Pop().Id);
    }

    [Test] public void Pop_TieBreak()
    {
        heap.InsertOrDecrease(new HeapNode(0, 1f, 5f));
        heap.InsertOrDecrease(new HeapNode(1, 1f, 2f));
        heap.InsertOrDecrease(new HeapNode(2, 1f, 8f));
        Assert.AreEqual(1, heap.Pop().Id);
        Assert.AreEqual(0, heap.Pop().Id);
        Assert.AreEqual(2, heap.Pop().Id);
    }

    [Test] public void DecreaseKey()
    {
        heap.InsertOrDecrease(new HeapNode(0, 10f));
        heap.InsertOrDecrease(new HeapNode(1, 5f));
        heap.InsertOrDecrease(new HeapNode(0, 2f));
        Assert.AreEqual(0, heap.Pop().Id);
        Assert.AreEqual(1, heap.Pop().Id);
    }

    [Test] public void DecreaseKey_NoIncrease()
    { heap.InsertOrDecrease(new HeapNode(0, 1f)); heap.InsertOrDecrease(new HeapNode(0, 10f)); Assert.AreEqual(1f, heap.Peek().Key0, 0.001f); }

    [Test] public void Peek_NoRemove() { heap.InsertOrDecrease(new HeapNode(0, 3f)); heap.InsertOrDecrease(new HeapNode(1, 1f)); Assert.AreEqual(2, heap.Length); }

    [Test] public void Contains_Inserted() { heap.InsertOrDecrease(new HeapNode(5, 3f)); Assert.IsTrue(heap.Contains(5)); }
    [Test] public void Contains_NotInserted() { Assert.IsFalse(heap.Contains(0)); }
    [Test] public void Contains_AfterPop() { heap.InsertOrDecrease(new HeapNode(5, 3f)); heap.Pop(); Assert.IsFalse(heap.Contains(5)); }

    [Test] public void Clear()
    { heap.InsertOrDecrease(new HeapNode(0, 5f)); heap.Clear(); Assert.IsTrue(heap.IsEmpty); Assert.IsFalse(heap.Contains(0)); }

    [Test] public void Many_MaintainsOrder()
    {
        for (int i = 0; i < 50; i++) heap.InsertOrDecrease(new HeapNode(i, (50 - i) * 1.5f));
        float prev = float.NegativeInfinity;
        while (!heap.IsEmpty) { var n = heap.Pop(); Assert.LessOrEqual(prev, n.Key0); prev = n.Key0; }
    }

    [Test] public void DecreaseKey_Interleaved()
    {
        for (int i = 0; i < 10; i++) heap.InsertOrDecrease(new HeapNode(i, 100f - i));
        for (int i = 0; i < 10; i++) heap.InsertOrDecrease(new HeapNode(i, i * 0.5f));
        int count = 0; float prev = float.NegativeInfinity;
        while (!heap.IsEmpty) { var n = heap.Pop(); Assert.LessOrEqual(prev, n.Key0); prev = n.Key0; count++; }
        Assert.AreEqual(10, count);
    }

    [Test] public void DuplicateId_OneEntry() { heap.InsertOrDecrease(new HeapNode(0, 5f)); heap.InsertOrDecrease(new HeapNode(0, 3f)); Assert.AreEqual(1, heap.Length); }
}
