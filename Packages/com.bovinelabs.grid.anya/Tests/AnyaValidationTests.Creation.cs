using BovineLabs.Grid.Anya;
using NUnit.Framework;
using Unity.Collections;

public unsafe partial class AnyaValidationTests
{
    [Test]
    [Category("Creation")]
    public void Create_ValidDimensions_CreatesState()
    {
        Assert.IsTrue(AnyaApi.TryCreate(10, 10, 100, Allocator.Temp, out var s));
        try
        {
            Assert.AreEqual(10, s.Grid.Width);
            Assert.AreEqual(10, s.Grid.Height);
            Assert.AreEqual(100, s.Grid.Length);
            Assert.IsTrue(s.Heap.IsCreated);
            Assert.IsTrue(s.Pool.IsCreated);
            Assert.IsTrue(s.NodeLookup.IsCreated);
            Assert.IsTrue(s.RootGCost != null);
        }
        finally
        {
            AnyaApi.Dispose(ref s);
        }
    }

    [Test]
    [Category("Creation")]
    public void Dispose_Double_DoesNotThrow()
    {
        Assert.IsTrue(AnyaApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        AnyaApi.Dispose(ref s);
        AnyaApi.Dispose(ref s);
    }
}
