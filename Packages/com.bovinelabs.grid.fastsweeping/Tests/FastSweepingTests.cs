using NUnit.Framework;
using Unity.Collections;
using BovineLabs.Grid.FastSweeping;

public class FastSweepingTests
{
    [Test] public void Create_Dimensions()
    { var s = FastSweepingApi.Create(5, 3, Allocator.Temp); Assert.AreEqual(15, s.Grid.Length); FastSweepingApi.Dispose(ref s); }

    [Test] public void InitSources_SetsZero()
    {
        var s = FastSweepingApi.Create(5, 1, Allocator.Temp);
        var src = new NativeArray<int>(new int[] { 0 }, Allocator.Temp);
        FastSweepingApi.Initialize(ref s, src);
        Assert.AreEqual(0f, s.T[0], 0.001f);
        Assert.IsTrue(float.IsPositiveInfinity(s.T[1]));
        FastSweepingApi.Dispose(ref s); src.Dispose();
    }

    static NativeArray<float> MakeSpeed(int len, float val, Allocator a)
    {
        var arr = new NativeArray<float>(len, a);
        for (int i = 0; i < len; i++) arr[i] = val;
        return arr;
    }

    [Test] public void Sweep_1D()
    {
        var s = FastSweepingApi.Create(5, 1, Allocator.Temp);
        var speed = MakeSpeed(5, 1f, Allocator.Temp);
        var src = new NativeArray<int>(new int[] { 0 }, Allocator.Temp);
        FastSweepingApi.Initialize(ref s, src);
        FastSweepingApi.SweepAllDirections(ref s, speed, 3);
        Assert.AreEqual(1f, s.T[1], 0.01f);
        Assert.AreEqual(2f, s.T[2], 0.01f);
        Assert.AreEqual(4f, s.T[4], 0.01f);
        FastSweepingApi.Dispose(ref s); speed.Dispose(); src.Dispose();
    }

    [Test] public void Sweep_2D_Center()
    {
        var s = FastSweepingApi.Create(3, 3, Allocator.Temp);
        var speed = MakeSpeed(9, 1f, Allocator.Temp);
        var src = new NativeArray<int>(new int[] { 4 }, Allocator.Temp);
        FastSweepingApi.Initialize(ref s, src);
        FastSweepingApi.SweepAllDirections(ref s, speed, 5);
        Assert.AreEqual(0f, s.T[4], 0.001f);
        Assert.AreEqual(1f, s.T[1], 0.1f);
        Assert.AreEqual(1.707f, s.T[0], 0.05f);
        FastSweepingApi.Dispose(ref s); speed.Dispose(); src.Dispose();
    }

    [Test] public void RelaxCell_Updates()
    {
        var s = FastSweepingApi.Create(5, 1, Allocator.Temp);
        var speed = MakeSpeed(5, 1f, Allocator.Temp);
        for (int i = 0; i < s.T.Length; i++) s.T[i] = float.PositiveInfinity;
        s.T[0] = 0f;
        FastSweepingApi.RelaxCell(s, speed, 1);
        Assert.AreEqual(1f, s.T[1], 0.01f);
        FastSweepingApi.Dispose(ref s); speed.Dispose();
    }

    [Test] public void Dispose_Double() { var s = FastSweepingApi.Create(3, 3, Allocator.Temp); FastSweepingApi.Dispose(ref s); FastSweepingApi.Dispose(ref s); }
}
