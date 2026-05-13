using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Morse;

public class MorseTests
{
    [Test] public void Create_Dimensions()
    { var s = MorseApi.Create(5, 5, 100, Allocator.Temp); Assert.AreEqual(25, s.Grid.Length); MorseApi.Dispose(ref s); }

    [Test] public void BuildGradient_Simple()
    {
        var s = MorseApi.Create(5, 5, 100, Allocator.Temp);
        var scalar = new NativeArray<float>(25, Allocator.Temp);
        // Simple slope
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                scalar[s.Grid.ToIndex(x, y)] = y;
        MorseApi.BuildGradient(ref s, in scalar);
        Assert.Greater(s.Critical.Length, 0);
        MorseApi.Dispose(ref s); scalar.Dispose();
    }

    [Test] public void TraceManifolds()
    {
        var s = MorseApi.Create(5, 5, 100, Allocator.Temp);
        var scalar = new NativeArray<float>(25, Allocator.Temp);
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                scalar[s.Grid.ToIndex(x, y)] = y;
        MorseApi.BuildGradient(ref s, in scalar);
        MorseApi.TraceManifolds(ref s);
        // All cells should have a component
        for (int i = 0; i < s.Grid.Length; i++)
            Assert.GreaterOrEqual(s.Component[i], 0);
        MorseApi.Dispose(ref s); scalar.Dispose();
    }

    [Test] public void Dispose_Double() { var s = MorseApi.Create(3, 3, 10, Allocator.Temp); MorseApi.Dispose(ref s); MorseApi.Dispose(ref s); }
}
