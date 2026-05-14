using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid.Tests
{
    public class NativeGrid2DTests
    {
        [Test]
        public unsafe void Create_WidthHeightSet()
        {
            using var grid = new NativeGrid2D(10, 20, Allocator.Temp);
            Assert.AreEqual(10, grid.Width);
            Assert.AreEqual(20, grid.Height);
            Assert.AreEqual(200, grid.Cells.Length);
        }

        [Test]
        public unsafe void InBounds_InsideTrue()
        {
            using var grid = new NativeGrid2D(5, 5, Allocator.Temp);
            Assert.IsTrue(grid.InBounds(new int2(0, 0)));
            Assert.IsTrue(grid.InBounds(new int2(4, 4)));
        }

        [Test]
        public unsafe void InBounds_OutsideFalse()
        {
            using var grid = new NativeGrid2D(5, 5, Allocator.Temp);
            Assert.IsFalse(grid.InBounds(new int2(-1, 0)));
            Assert.IsFalse(grid.InBounds(new int2(5, 0)));
            Assert.IsFalse(grid.InBounds(new int2(0, 5)));
        }

        [Test]
        public unsafe void SetAndIsFree_Works()
        {
            using var grid = new NativeGrid2D(5, 5, Allocator.Temp);
            Assert.IsTrue(grid.IsFree(new int2(2, 2)));
            grid.Set(2, 2, CellState.Blocked);
            Assert.IsFalse(grid.IsFree(new int2(2, 2)));
        }
    }

    public class GridHeuristicsTests
    {
        [Test]
        public unsafe void Euclidean_SamePoint_Zero()
        {
            Assert.AreEqual(0f, GridHeuristics.Euclidean(new int2(5, 5), new int2(5, 5)), 0.001f);
        }

        [Test]
        public unsafe void Euclidean_UnitDiagonal()
        {
            Assert.AreEqual(1.4142f, GridHeuristics.Euclidean(new int2(0, 0), new int2(1, 1)), 0.001f);
        }

        [Test]
        public unsafe void Octile_Cardinal()
        {
            Assert.AreEqual(3f, GridHeuristics.Octile(new int2(0, 0), new int2(3, 0)), 0.001f);
        }

        [Test]
        public unsafe void Octile_Diagonal()
        {
            Assert.AreEqual(1.4142f, GridHeuristics.Octile(new int2(0, 0), new int2(1, 1)), 0.001f);
        }

        [Test]
        public unsafe void Manhattan_Correct()
        {
            Assert.AreEqual(5f, GridHeuristics.Manhattan(new int2(0, 0), new int2(3, 2)), 0.001f);
        }
    }

    public class NativeMinHeapTests
    {
        [Test]
        public unsafe void PushPop_SortedOrder()
        {
            using var heap = new NativeMinHeap(16, Allocator.Temp);
            heap.Push(2, 5f);
            heap.Push(0, 1f);
            heap.Push(1, 3f);

            Assert.AreEqual(0, heap.Pop());
            Assert.AreEqual(1, heap.Pop());
            Assert.AreEqual(2, heap.Pop());
        }

        [Test]
        public unsafe void TryUpdate_LowersCost()
        {
            using var heap = new NativeMinHeap(16, Allocator.Temp);
            heap.Push(0, 10f);
            heap.Push(1, 5f);

            heap.TryUpdate(0, 2f);

            Assert.AreEqual(0, heap.Pop());
            Assert.AreEqual(1, heap.Pop());
        }

        [Test]
        public unsafe void Contains_Works()
        {
            using var heap = new NativeMinHeap(16, Allocator.Temp);
            heap.Push(0, 1f);
            heap.Push(5, 2f);
            Assert.IsTrue(heap.Contains(0));
            Assert.IsTrue(heap.Contains(5));
            Assert.IsFalse(heap.Contains(15));
        }
    }
}