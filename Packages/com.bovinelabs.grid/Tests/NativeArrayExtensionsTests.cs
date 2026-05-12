using NUnit.Framework;
using Unity.Collections;
using BovineLabs.Grid;

public class NativeArrayExtensionsTests
{
    [Test] public void Fill_Int() { var a = new NativeArray<int>(10, Allocator.Temp); a.Fill(42); for (int i = 0; i < 10; i++) Assert.AreEqual(42, a[i]); a.Dispose(); }
    [Test] public void Fill_Float() { var a = new NativeArray<float>(5, Allocator.Temp); a.Fill(3.14f); for (int i = 0; i < 5; i++) Assert.AreEqual(3.14f, a[i], 0.001f); a.Dispose(); }
    [Test] public void Fill_Byte() { var a = new NativeArray<byte>(7, Allocator.Temp); a.Fill((byte)255); for (int i = 0; i < 7; i++) Assert.AreEqual(255, a[i]); a.Dispose(); }
    [Test] public void Fill_Empty() { var a = new NativeArray<int>(0, Allocator.Temp); Assert.DoesNotThrow(() => a.Fill(0)); a.Dispose(); }
}
